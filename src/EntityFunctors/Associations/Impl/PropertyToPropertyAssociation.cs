namespace EntityFunctors.Associations.Impl
{
    using System;
    using System.Linq.Expressions;

    public class PropertyToPropertyAssociation<TSource, TTarget, TProperty> : PropertyToPropertyAssociationBase
        where TSource : class
        where TTarget : class, new() 
    {
        public PropertyToPropertyAssociation(Expression<Func<TSource, TProperty>> source, Expression<Func<TTarget, TProperty>> target)
            :base(source, target)
        {
        }

        protected override Expression BuildDonor(MemberExpression donorAccessor, MappingDirection direction)
        {
            return donorAccessor;
        }
    }
}