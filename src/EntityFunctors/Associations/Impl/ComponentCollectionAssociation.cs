namespace EntityFunctors.Associations.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

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
    }
}