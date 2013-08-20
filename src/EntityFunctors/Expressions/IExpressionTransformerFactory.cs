namespace EntityFunctors.Expressions
{
    public interface IExpressionTransformerFactory
    {
        IExpressionTransformer<TFrom, TTo> Create<TFrom, TTo>();
    }
}

