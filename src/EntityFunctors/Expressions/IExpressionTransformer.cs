namespace EntityFunctors.Expressions
{
    using System;
    using System.Linq.Expressions;

    public interface IExpressionTransformer<TFrom, TTo>
    {
        Expression<Func<TTo, TToResult>> Transform<TFromResult, TToResult>(Expression<Func<TFrom, TFromResult>> source);

        Expression<Func<TTo, TResult>> Transform<TResult>(Expression<Func<TFrom, TResult>> source);

        LambdaExpression Transform(LambdaExpression source);

        Expression Transform(Expression source);
    }
}