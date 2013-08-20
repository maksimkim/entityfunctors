namespace EntityFunctors
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Diagnostics.Contracts;
    using Associations;
    using Extensions;

    public class TypeMap<TSource, TTarget> : IAssociationProvider
    {
        private readonly IList<IMappingAssociation> _associations = new List<IMappingAssociation>();

        protected TypeMap()
        {
            Contract.Assert(typeof(TSource) != typeof(TTarget));

            Key = new TypeMapKey(typeof(TSource), typeof(TTarget));
        }

        protected IAccessable MapProperties<TProperty>(
            Expression<Func<TSource, TProperty>> source,
            Expression<Func<TTarget, TProperty>> target
        )
        {
            var association = new PropertyToPropertyAssociation<TSource, TTarget>(new PropertyPart(source.GetProperty()), new PropertyPart(target.GetProperty()));
            _associations.Add(association);
            return association;
        }

        protected IAccessable MapProperties<TSourceProperty, TTargetProperty>(
            Expression<Func<TSource, TSourceProperty>> source,
            Func<TSourceProperty, TTargetProperty> converter,
            Expression<Func<TTarget, TTargetProperty>> target,
            Func<TTargetProperty, TSourceProperty> inverseConverter
        )
        {
            Contract.Assert(converter != null);
            Contract.Assert(inverseConverter != null);

            var association = new PropertyToPropertyAssociation<TSource, TTarget>(new PropertyPart(source.GetProperty(), converter), new PropertyPart(target.GetProperty(), inverseConverter));
            _associations.Add(association);
            return association;
        }

        protected void MapExpressionToProperty<TProperty>(
            Expression<Func<TSource, TProperty>> source,
            Expression<Func<TTarget, TProperty>> target
        )
        {
            Contract.Assert(source != null);

            _associations.Add(new ExpressionToPropertyAssociation<TSource, TTarget>(
                source,
                new PropertyPart(target.GetProperty())
             ));
        }

        protected IExpandable MapComponents<TSourceComponent, TTargetComponent>(
            Expression<Func<TSource, TSourceComponent>> source,
            Expression<Func<TTarget, TTargetComponent>> target
        )
        {
            var association = new ComponentToComponentAssociation<TSource, TTarget>(new PropertyPart(source.GetProperty()), new PropertyPart(target.GetProperty()));
            _associations.Add(association);
            return association;
        }

        protected IExpandable MapCollections<TSourceItem, TTargetItem>(
            Expression<Func<TSource, IEnumerable<TSourceItem>>> source,
            Expression<Func<TTarget, IEnumerable<TTargetItem>>> target,
            Expression<Func<TSourceItem, TTargetItem>> converter
        )
        {
            Contract.Assert(converter != null);

            var association = new CollectionAssociation<TSource, TTarget>(new PropertyPart(source.GetProperty()), new PropertyPart(target.GetProperty()), converter);
            _associations.Add(association);
            return association;
        }

        protected IExpandable MapComponentCollections<TSourceItem, TTargetItem>(
            Expression<Func<TSource, IEnumerable<TSourceItem>>> source,
            Expression<Func<TTarget, IEnumerable<TTargetItem>>> target
        )
        {
            var association = new ComponentCollectionAssociation<TSource, TTarget>(new PropertyPart(source.GetProperty()), new PropertyPart(target.GetProperty()));
            _associations.Add(association);
            return association;
        }

        public virtual TypeMapKey Key { get; protected set; }
        

        public IEnumerable<IMappingAssociation> Associations
        {
            get { return _associations; }
        }
    }
}