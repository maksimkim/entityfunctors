﻿namespace EntityFunctors.Associations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq.Expressions;
    using Extensions;

    public class CollectionAssociation<TSource, TSourceItem, TTarget, TTargetItem> 
        : CollectionAssociationBase<TSource, TSourceItem, TTarget, TTargetItem>, IConvertionAssociation
        where TSource : class
        where TTarget : class, new()
    {
        public ConverterInfo SourceConverter 
        { 
            get
            {
                throw new InvalidOperationException();
            } 
        }

        public ConverterInfo TargetConverter { get; private set; }

        public CollectionAssociation(Expression<Func<TSource, IEnumerable<TSourceItem>>> source, Expression<Func<TTarget, IEnumerable<TTargetItem>>> target, Expression<Func<TSourceItem, TTargetItem>> converter)
            : base(source, target)
        {
            Contract.Assert(converter != null);

            TargetConverter = new ConverterInfo(converter);
        }

        protected override LambdaExpression CreateSelector(Type @from, Type to, ParameterExpression expands, IMappingRegistry registry)
        {
            return TargetConverter.Expression;
        }
    }
}