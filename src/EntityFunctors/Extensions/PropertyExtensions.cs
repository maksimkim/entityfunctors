namespace EntityFunctors.Extensions
{
    using System.Reflection;
    using Cfg;

    public static class PropertyExtensions
    {
        public static string GetName(this PropertyInfo property)
        {
            return Config.ReflectionOptimizer.GetName(property);
        }
    }
}