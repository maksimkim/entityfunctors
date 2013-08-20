namespace EntityFunctors.Associations
{
    using System.Linq.Expressions;

    public interface IMappingRegistry
    {
        Expression GetMapper(ParameterExpression @from, ParameterExpression to, ParameterExpression expands);
    }
}