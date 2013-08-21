namespace EntityFunctors.Cfg
{
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Runtime.Serialization;

    public class DefaultReflectionOptimizer : IReflectionOptimizer
    {
        private static readonly ConcurrentDictionary<PropertyInfo, string> PropertyCache = new ConcurrentDictionary<PropertyInfo, string>();

        public string GetName(PropertyInfo property)
        {
            DataMemberAttribute attr;
            return PropertyCache.GetOrAdd(
                property,
                key =>
                    (attr = key.GetCustomAttribute<DataMemberAttribute>()) != null && !string.IsNullOrWhiteSpace(attr.Name)
                        ? attr.Name
                        : key.Name
                );
        }
    }
}