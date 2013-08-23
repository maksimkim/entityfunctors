namespace EntityFunctors.Expressions
{
    using System;
    using System.Linq.Expressions;

    public interface IExpressionMapper
    {
        Expression<Func<TTarget, TTargetResult>> Map<TSource, TSourceResult, TTarget, TTargetResult>(Expression<Func<TSource, TSourceResult>> expression);

        Expression<Func<TTarget, TResult>> Map<TSource, TTarget, TResult>(Expression<Func<TSource, TResult>> expression);

        LambdaExpression Map(LambdaExpression expression, Type sourceType, Type targetType);

        Expression Map(Expression expression, Type sourceType, Type targetType);
    }
}