namespace EntityFunctors.Cfg
{
    using System.Reflection;

    public interface IReflectionOptimizer
    {
        string GetName(PropertyInfo property);
    }
}