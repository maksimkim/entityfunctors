namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;
    using Cfg;
    using Extensions;

    public class PropertyToPropertyAssociation<TSource, TTarget> : IAccessable, IMappingAssociation
    {
        public PropertyPart Source { get; private set; }

        public PropertyPart Target { get; private set; }

        private readonly bool _convertionRequired;

        public MappingDirection Direction { get; private set; }

        public PropertyToPropertyAssociation(PropertyPart source, PropertyPart target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);

            var pSource = source.Property;
            var pTarget = target.Property;

            _convertionRequired = pSource.PropertyType != pTarget.PropertyType;

            if (_convertionRequired && !(source.ConverterMethod != null && target.ConverterMethod != null && source.ConverterMethod.ReturnType == pTarget.PropertyType && target.ConverterMethod.ReturnType == pSource.PropertyType))
                throw new InvalidOperationException(
                   string.Format(
                       "Mapping property {0} to {1} requires providing value converter from {2} to {3}",
                       pSource.Name,
                       pTarget.Name,
                       pSource.PropertyType,
                       pTarget.PropertyType
                   )
               );
           
            Source = source;

            Target = target;

            Direction = MappingDirection.All;
        }

        public Expression BuildMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression propertyKeys, IMappingRegistry registry)
        {
            Contract.Assert(@from.Type == typeof(TSource) || @from.Type == typeof(TTarget));
            Contract.Assert(to.Type == typeof(TSource) || to.Type == typeof(TTarget));

            var direction = @from.Type == typeof(TTarget) ? MappingDirection.Write : MappingDirection.Read;

            var donor = direction == MappingDirection.Write ? Target : Source;
            var aceptor = direction == MappingDirection.Write ? Source : Target;

            if ((Direction & direction) != direction)
                return Expression.Empty();

            var donorAccessor = Expression.Property(@from, donor.Property);

            Expression result = Expression.Assign(
                Expression.Property(to, aceptor.Property), 
                !_convertionRequired 
                ? donorAccessor : 
                BuildConverter(donor.Property, donorAccessor, donor.ConverterMethod)
            );

            if (direction == MappingDirection.Write && propertyKeys != null)
                result = Expression.IfThen(
                    Expression.OrElse(
                        propertyKeys.CreateCheckForDefault(), 
                        propertyKeys.CreateContains(Expression.Constant(Config.ReflectionOptimizer.GetName(Target.Property), typeof(string)))
                    ),
                    result
                );

            return result;
        }

        public Expression Build(Expression arg)
        {
            var constant = arg as ConstantExpression;
            if (constant != null)
            {
                if (Target.Converter == null)
                    throw new InvalidOperationException();

                return Expression.Constant(
                    Target.Converter(constant.Value), 
                    Target.ConverterMethod.ReturnType
                );
            }
                
            
            return Expression.Property(arg, Source.Property);
        }

        public PropertyInfo TargetProperty
        {
            get { return Target.Property; }
        }

        public Expression Rewrite(Expression original, Expression replacement)
        {
            var cnst = original as ConstantExpression;

            if (cnst != null)
                return _convertionRequired ? Expression.Constant(Target.Converter(cnst.Value), Source.Property.PropertyType) : cnst;
           
            return Expression.Property(replacement, Source.Property);
        }

        public IEnumerable<TypeMapKey> ChildMapKeys
        {
            get { yield break; }
        }

        private static Expression BuildConverter(PropertyInfo property, MemberExpression accessor, MethodInfo method)
        {
            //todo: conditional defaut check

            Expression converter = Expression.Call(method, accessor);

            if (!property.PropertyType.IsValueType)
            {
                converter =
                    Expression.Condition(
                        accessor.CreateCheckForDefault(),
                        method.ReturnType.GetDefaultExpression(),
                        converter
                    );
            }

            return converter;
        }

        public void ReadOnly()
        {
            Direction = MappingDirection.Read;
        }

        public void WriteOnly()
        {
            Direction = MappingDirection.Write;
        }

        public void Read()
        {
            AddDirection(MappingDirection.Read);
        }

        public void Write()
        {
            AddDirection(MappingDirection.Write);
        }

        private void AddDirection(MappingDirection val)
        {
            if ((Direction & val) != val)
                Direction |= val;
        }
    }
}