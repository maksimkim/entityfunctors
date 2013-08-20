namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;

    public class PropertyToPropertyAssociation<TSource, TTarget> : IAccessable, IMappingAssociation
    {
        public PropertyPart Source { get; private set; }

        public PropertyPart Target { get; private set; }

        //private readonly IDictionary<Type, PropertyPart> _parts;

        private readonly bool _convertionRequired;

        public MappingDirection Direction { get; private set; }

        public PropertyToPropertyAssociation(PropertyPart source, PropertyPart target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);

            var pSource = source.Property;
            var pTarget = target.Property;

            _convertionRequired = pSource.PropertyType != pTarget.PropertyType;

            if (_convertionRequired && !(source.Converter != null && target.Converter != null && source.Converter.Method.ReturnType == pTarget.PropertyType && target.Converter.Method.ReturnType == pSource.PropertyType))
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

            //_parts = new Dictionary<Type, PropertyPart>
            //{
            //    { typeof(TSource), source },
            //    { typeof(TTarget), target }
            //};

            Direction = MappingDirection.All;
        }

        public Expression BuildMapper(ParameterExpression @from, ParameterExpression to, IMappingRegistry registry, ParameterExpression expands)
        {
            Contract.Assert(@from.Type == typeof(TSource) || @from.Type == typeof(TTarget));
            Contract.Assert(to.Type == typeof(TSource) || to.Type == typeof(TTarget));

            var direction = @from.Type == typeof(TTarget) ? MappingDirection.Write : MappingDirection.Read;

            var donor = direction == MappingDirection.Write ? Target : Source;
            var aceptor = direction == MappingDirection.Write ? Source : Target;

            if ((Direction & direction) != direction)
                return Expression.Empty();

            var donorAccessor = Expression.Property(@from, donor.Property);

            return Expression.Assign(
                Expression.Property(to, aceptor.Property),
                !_convertionRequired
                ? donorAccessor
                : BuildConverter(donor.Property, donorAccessor, donor.Converter.Method)
            );
        }

        public PropertyInfo RewritableProperty
        {
            get { return Target.Property; }
        }

        public Expression Rewrite(Expression original, ParameterExpression parameter)
        {
            return Expression.Property(parameter, Source.Property);
        }

        private KeyValuePair<PropertyInfo, Delegate> _converter;
        
        public IEnumerable<KeyValuePair<PropertyInfo, Delegate>> ValueConverters
        {
            get
            {
                if (!_convertionRequired)
                    yield break;

                //todo: Check for default value
                yield return
                    _converter.Key == null
                    ? (_converter = new KeyValuePair<PropertyInfo, Delegate>(Target.Property, Target.Converter))
                    : _converter;
            }
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