namespace EntityFunctors.Associations.Impl
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;

    public class PropertyToPropertyWithConversionAssociation<TSource, TSourceProperty, TTarget, TTargetProperty>
        : PropertyToPropertyAssociationBase, IConvertionAssociation 
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
    }
}