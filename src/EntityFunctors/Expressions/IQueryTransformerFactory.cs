namespace EntityFunctors.Expressions
{
    public interface IQueryTransformerFactory
    {
        IQueryTransformer<TFrom, TTo> Create<TFrom, TTo>();
    }
}

