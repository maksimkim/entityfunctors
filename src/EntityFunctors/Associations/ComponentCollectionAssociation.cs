namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Extensions;

    public class ComponentCollectionAssociation<TSource, TSourceItem, TTarget, TTargetItem> 
        : CollectionAssociationBase<TSource, TSourceItem, TTarget, TTargetItem>, IComponentAssociation
        where TSource : class
        where TSourceItem : class
        where TTarget : class, new()
        where TTargetItem: class, new()
    {
        public TypeMapKey ComponentKey { get; private set; }

        public ComponentCollectionAssociation(
            Expression<Func<TSource, IEnumerable<TSourceItem>>> source, 
            Expression<Func<TTarget, IEnumerable<TTargetItem>>> target)
            : base(source, target)
        {
            ComponentKey = new TypeMapKey(SourceItemType, TargetItemType);
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
                    typeof(TSource).Name + "." + Source.GetProperty().Name,
                    typeof(TTarget).Name + "." + Target.GetProperty().Name,
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