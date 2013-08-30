namespace EntityFunctors.Associations
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using Expressions;
    using Extensions;

    public class PropertyToPropertyWithConversionAssociation<TSource, TSourceProperty, TTarget, TTargetProperty>
        : PropertyToPropertyAssociationBase<TSource, TTarget>, IConvertionAssociation 
        where TSource : class 
        where TTarget : class, new()
    {
        public ConverterInfo SourceConverter { get; private set; }

        public ConverterInfo TargetConverter { get; private set; }
        
        public PropertyToPropertyWithConversionAssociation(
            Expression<Func<TSource, TSourceProperty>> source,
            Expression<Func<TSourceProperty, TTargetProperty>> sourceConverter,
            Expression<Func<TTarget, TTargetProperty>> target,
            Expression<Func<TTargetProperty, TSourceProperty>> targetConverter
        ) : base(source, target)
        {
            Contract.Assert(sourceConverter != null);
            Contract.Assert(targetConverter != null);

            SourceConverter = new ConverterInfo(sourceConverter);
            TargetConverter = new ConverterInfo(targetConverter);
        }

        protected override Expression BuildDonor(MemberExpression donorAccessor, MappingDirection direction)
        {
            var converter = (direction == MappingDirection.Read ? SourceConverter : TargetConverter).Expression;
            
            var conversion = converter.Apply(donorAccessor);

            if (!donorAccessor.Type.IsValueType)
                conversion = Expression.Condition(
                    donorAccessor.CreateCheckForDefault(),
                    conversion.Type.GetDefaultExpression(),
                    conversion
                );

            return conversion;
        }
    }
}