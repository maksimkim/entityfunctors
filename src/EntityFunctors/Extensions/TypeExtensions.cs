namespace EntityFunctors.Extensions
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public static class TypeExtensions
    {
        //a thread-safe way to hold default instances created at run-time
        private static readonly ConcurrentDictionary<Type, object> TypeDefaults = new ConcurrentDictionary<Type, object>();

        public static object GetDefaultValue(this Type type)
        {
            return type.IsValueType ? TypeDefaults.GetOrAdd(type, Activator.CreateInstance) : null;
        }

        public static ConstantExpression GetDefaultExpression(this Type type)
        {
            return Expression.Constant(type.GetDefaultValue(), type);
        }

        public static Type GetItemType(this Type enumerableType)
        {
            var target = 
                enumerableType
                .GetInterfaces()
                .Union(new[] {enumerableType})
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (target == null)
                throw new ArgumentException(string.Format("Type {0} doesn't implement IEnumerable<T>", enumerableType.Name));

            return target.GetGenericArguments()[0];
        }

        public static bool TryGetItemType(this Type candidate, out Type itemType)
        {
            itemType = null;
            
            var target =
                candidate
                .GetInterfaces()
                .Union(new[] { candidate })
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (target != null)
                itemType = target.GetGenericArguments()[0];

            return itemType != null;
        }
    }
}