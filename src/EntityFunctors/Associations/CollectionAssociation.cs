namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using Extensions;

    public class CollectionAssociation<TSource, TTarget> : CollectionAssociationBase<TSource, TTarget>
    {
        public LambdaExpression Converter { get; private set; }

        public CollectionAssociation(PropertyPart source, PropertyPart target, LambdaExpression converter) 
            : base(source, target)
        {
            Contract.Assert(converter != null);
            Contract.Assert(converter.Parameters.Count == 1);
            Contract.Assert(converter.Parameters[0].Type == source.Property.PropertyType.GetItemType());
            Contract.Assert(converter.ReturnType == target.Property.PropertyType.GetItemType());
            
            Converter = converter;
        }

        public override IEnumerable<TypeMapKey> ChildMapKeys
        {
            get { yield break;}
        }

        protected override LambdaExpression CreateSelector(Type @from, Type to, ParameterExpression expands, IMappingRegistry registry)
        {
            return Converter;
        }
    }
}