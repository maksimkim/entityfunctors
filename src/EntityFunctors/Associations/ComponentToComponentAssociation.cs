namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;
    using Cfg;
    using Extensions;
    using Fluent;

    public class ComponentToComponentAssociation<TSource, TSourceComponent, TTarget, TTargetComponent> 
        : IMappingAssociation, IComponentAssociation, IAccessable, IExpandable
        where TSource : class 
        where TSourceComponent : class
        where TTarget : class, new()
        where TTargetComponent : class, new()
    {
        public string Key { get; private set; }

        public MappingDirection Direction { get; private set; }

        public LambdaExpression Source { get; private set; }

        public LambdaExpression Target { get; private set; }

        public bool Expand { get; private set; }

        public TypeMapKey ComponentKey { get; private set; }

        public ComponentToComponentAssociation(Expression<Func<TSource, TSourceComponent>> source, Expression<Func<TTarget, TTargetComponent>> target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);
            
            PropertyInfo prop;
            Contract.Assert(source.Body.TryGetProperty(out prop));
            Contract.Assert(target.Body.TryGetProperty(out prop));

            Source = source;
            Target = target;

            target.GetProperty();

            Key = Target.GetProperty().GetName();

            Direction = MappingDirection.All;

            ComponentKey = new TypeMapKey(Source.ReturnType, Target.ReturnType);
        }

        public Expression BuildMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression propertyKeys, IMappingRegistry registry)
        {
            Contract.Assert(@from.Type == typeof(TSource) || @from.Type == typeof(TTarget));
            Contract.Assert(to.Type == typeof(TSource) || to.Type == typeof(TTarget));

            var direction = @from.Type == typeof(TTarget) ? MappingDirection.Write : MappingDirection.Read;

            if ((Direction & direction) != direction)
                return Expression.Empty();

            var donorAccessor = direction == MappingDirection.Write ? Target : Source;
            var acceptorAccessor = direction == MappingDirection.Write ? Source : Target;

            var propFrom = donorAccessor.Apply(@from);
            var propTo = acceptorAccessor.Apply(to);

            var varFrom = Expression.Variable(donorAccessor.ReturnType);
            var varTo = Expression.Variable(acceptorAccessor.ReturnType);

            //todo: filter out expands
            var mapper = registry.GetMapper(varFrom, varTo, null);

            if (mapper == null)
                throw new InvalidOperationException(string.Format(
                    "Component registration mapping {0} <--> {1} requires mapping for types {2} <--> {3} that wasn't found",
                    typeof(TSource).Name + "." + Source.GetProperty().Name,
                    typeof(TTarget).Name + "." + Target.GetProperty().Name,
                    Source.ReturnType,
                    Target.ReturnType
                ));

            var body = new List<Expression>();

            if (direction == MappingDirection.Read)
            {
                //create target object (only direct mapping: entity -> dto)
                var ctor = acceptorAccessor.ReturnType.GetConstructor(Type.EmptyTypes);

                if (ctor == null)
                    throw new InvalidOperationException(string.Format(
                        "Type {0} must declare public parameterless constructor to be mapped from {1}",
                        acceptorAccessor.ReturnType,
                        donorAccessor.ReturnType
                    ));

                body.Add(Expression.Assign(propTo, Expression.New(ctor)));
            }

            //variable assignments
            body.Add(Expression.Assign(varFrom, propFrom));
            body.Add(Expression.Assign(varTo, propTo));

            body.Add(mapper);

            var result = 
                Expression.IfThenElse(
                    propFrom.CreateCheckForDefault(), 
                    Expression.Assign(propTo, acceptorAccessor.ReturnType.GetDefaultExpression()), 
                    Expression.Block(new[] {varFrom, varTo}, body)
                );

            if (propertyKeys == null)
                return result;

            if (direction == MappingDirection.Read && Expand)
                result = Expression.IfThen(
                    propertyKeys.CreateContains(Expression.Constant(Key, typeof(string))), 
                    result
                );

            if (direction == MappingDirection.Write)
                result = Expression.IfThen(
                    Expression.OrElse(
                        propertyKeys.CreateCheckForDefault(),
                        propertyKeys.CreateContains(Expression.Constant(Key, typeof(string)))
                    ),
                    result
                );

            return result;

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

        public void Expandable()
        {
            Expand = true;
        }
    }
}