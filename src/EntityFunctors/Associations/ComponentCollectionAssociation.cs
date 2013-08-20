namespace EntityFunctors.Associations
{
    using System;
    using System.Linq.Expressions;

    public class ComponentCollectionAssociation<TSource, TTarget> : CollectionAssociationBase<TSource, TTarget>
    {
        public ComponentCollectionAssociation(PropertyPart source, PropertyPart target)
            :base(source, target)
        {

        }

        protected override LambdaExpression CreateSelector(Type @from, Type to, ParameterExpression expands, IMappingRegistry registry)
        {
            var paramFrom = Expression.Variable(@from);
            var varTo = Expression.Variable(to);

            var ctor = to.GetConstructor(Type.EmptyTypes);

            if (ctor == null)
                throw new InvalidOperationException(string.Format(
                    "Type {0} must declare public parameterless constructor to be mapped from {1}",
                    to,
                    @from
                ));

            //todo: filter out expands
            var mapper = registry.GetMapper(paramFrom, varTo, expands);

            if (mapper == null)
                throw new InvalidOperationException(string.Format(
                    "Component collection registration mapping {0} <--> {1} requires mapping for types {2} <--> {3} that wasn't found",
                    typeof(TSource).Name + "." + Source.Property.Name,
                    typeof(TTarget).Name + "." + Target.Property.Name,
                    @from,
                    to
                ));

            return Expression.Lambda(
                Expression.Block(
                    new[] { varTo },
                    new[] { Expression.Assign(varTo, Expression.New(ctor)), mapper, varTo }
                ),
                paramFrom
            );
        }
    }
}