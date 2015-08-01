using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace ConfigurationExtensions
{
    public class ValidationEnabledAttribute : Attribute
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty;

        private static readonly ConcurrentDictionary<Type, PropertyInfo> TypeToPropertyMap
            = new ConcurrentDictionary<Type, PropertyInfo>();

        public static bool IsEnabled(object o)
        {
            var type = o.GetType();

            var property = TypeToPropertyMap.GetOrAdd(type, t =>
            {
                var properties = type.GetProperties(Flags);

                return properties.SingleOrDefault(p =>
                    p.PropertyType == typeof(bool)
                    && p.GetCustomAttribute<ValidationEnabledAttribute>() != null);
            });

            return property == null || (bool)property.GetValue(o);
        }
    }
}
