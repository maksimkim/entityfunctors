namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;

    public class ComponentToComponentAssociation<TSource, TTarget> : IAccessable, IExpandable, IMappingAssociation
    {
        public PropertyPart Source { get; private set; }

        public PropertyPart Target { get; private set; }

        public MappingDirection Direction { get; private set; }

        protected string Expand { get; set; }

        public ComponentToComponentAssociation(PropertyPart source, PropertyPart target)
        {
            Contract.Assert(source != null);
            Contract.Assert(target != null);

            Source = source;
            Target = target;

            Direction = MappingDirection.All;
        }

        public Expression BuildMapper(ParameterExpression @from, ParameterExpression to, IMappingRegistry registry, ParameterExpression expands)
        {
            Contract.Assert(@from.Type == typeof(TSource) || @from.Type == typeof(TTarget));
            Contract.Assert(to.Type == typeof(TSource) || to.Type == typeof(TTarget));

            var direction = @from.Type == typeof(TTarget) ? MappingDirection.Write : MappingDirection.Read;

            if ((Direction & direction) != direction)
                return Expression.Empty();

            var partFrom = direction == MappingDirection.Write ? Target : Source;
            var partTo = direction == MappingDirection.Write ? Source : Target;

            var propFrom = Expression.Property(@from, partFrom.Property);
            var propTo = Expression.Property(to, partTo.Property);

            var typeFrom = partFrom.Property.PropertyType;
            var typeTo = partTo.Property.PropertyType;

            var varFrom = Expression.Variable(typeFrom);
            var varTo = Expression.Variable(typeTo);

            //todo: filter out expands
            var mapper = registry.GetMapper(varFrom, varTo, expands);

            if (mapper == null)
                throw new InvalidOperationException(string.Format(
                    "Component registration mapping {0} <--> {1} requires mapping for types {2} <--> {3} that wasn't found",
                    typeof(TSource).Name + "." + Source.Property.Name,
                    typeof(TTarget).Name + "." + Target.Property.Name,
                    Source.Property.PropertyType,
                    Target.Property.PropertyType
                ));

            var body = new List<Expression>();

            if (direction == MappingDirection.Read)
            {
                //create target object (only direct mapping: entity -> dto)
                var ctor = typeTo.GetConstructor(Type.EmptyTypes);

                if (ctor == null)
                    throw new InvalidOperationException(string.Format(
                        "Type {0} must declare public parameterless constructor to be mapped from {1}",
                        typeTo,
                        typeFrom
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
                    Expression.Assign(propTo, typeTo.GetDefaultExpression()), 
                    Expression.Block(new[] {varFrom, varTo}, body)
                );

            if (direction == MappingDirection.Read && expands != null && !string.IsNullOrWhiteSpace(Expand))
                result = Expression.IfThen(
                    expands.CreateContains(Expression.Constant(Expand, typeof(string))), 
                    result
                );

            return result;

        }

        public PropertyInfo TargetProperty
        {
            get { return Target.Property; }
        }

        public Expression Rewrite(Expression original, ParameterExpression parameter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<PropertyInfo, Delegate>> ValueConverters
        {
            get { yield break; }
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
            Expand = Target.Property.GetContractName();
        }
    }
}