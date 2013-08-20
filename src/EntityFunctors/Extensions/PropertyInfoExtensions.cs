namespace EntityFunctors.Extensions
{
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Runtime.Serialization;

    public static class PropertyInfoExtensions
    {
        private static readonly ConcurrentDictionary<PropertyInfo, string> PropertyCache = new ConcurrentDictionary<PropertyInfo, string>();

        public static string GetContractName(this PropertyInfo property)
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