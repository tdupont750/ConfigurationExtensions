using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace ConfigurationExtensions
{
    public static class NameValueCollectionExtensions
    {
        private static readonly Type[] SupportedByChangeType =
        {
            typeof (string),
            typeof (DateTime)
        };

        public static NameValueCollection Combine(
            this NameValueCollection collection, 
            params NameValueCollection[] collections)
        {
            var result = new NameValueCollection { collection };

            foreach (var subCollection in collections)
                foreach (var key in subCollection.AllKeys)
                {
                    if (result.AllKeys.Contains(key))
                        continue;

                    var value = subCollection[key];
                    result.Add(key, value);
                }

            return result;
        }

        public static T CreateObject<T>(
            this NameValueCollection collection,
            string prefix = null,
            bool isRequired = false,
            bool validate = true)
            where T : new()
        {
            var type = typeof(T);

            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = type.Name;
            }

            object result;
            if (TryCreateObject(collection, prefix, type, validate, out result))
            {
                return (T)result;
            }

            if (isRequired)
            {
                throw new KeyNotFoundException(prefix + " not found");
            }

            return Activator.CreateInstance<T>();
        }

        private static bool TryCreateObject(
            this NameValueCollection collection,
            string prefix,
            Type type,
            bool validate,
            out object result)
        {
            var exactMatchCount = collection.AllKeys.Count(k => k == prefix);
            if (exactMatchCount == 1)
            {
                return TryGetValue(prefix, type, collection[prefix], out result);
            }

            if (exactMatchCount > 1)
            {
                throw new InvalidOperationException(prefix + " must be unique");
            }

            var keyInfos = collection.AllKeys
                .Where(k => k.StartsWith(prefix))
                .Select(
                    k =>
                    {
                        var collectionIndex = k.IndexOf('[', prefix.Length);
                        var isCollection = collectionIndex != -1;
                        var collectionKey = string.Empty;

                        if (isCollection)
                        {
                            var closeCollectionIndex = k.IndexOf(']', collectionIndex);
                            collectionKey = k.Substring(collectionIndex + 1, closeCollectionIndex - collectionIndex - 1);
                        }

                        return new
                        {
                            IsCollection = isCollection,
                            CollectionKey = collectionKey
                        };
                    })
                .ToList();

            if (keyInfos.Count == 0)
            {
                result = null;
                return false;
            }

            if (type.IsGenericType && keyInfos.All(k => k.IsCollection))
            {
                var genericType = type.GetGenericTypeDefinition();
                var genericArguments = type.GetGenericArguments();
                var distinctCollectionKeys = keyInfos
                    .Select(k => k.CollectionKey)
                    .Distinct()
                    .ToList();

                if (genericType == typeof(List<>))
                {
                    var list = (IList)Activator.CreateInstance(type);
                    var valueType = genericArguments.Single();

                    foreach (var collectionKey in distinctCollectionKeys.OrderBy(int.Parse))
                    {
                        var valuePrefix = string.Format("{0}[{1}]", prefix, collectionKey);

                        object valueResult;
                        if (!TryCreateObject(collection, valuePrefix, valueType, validate, out valueResult))
                        {
                            valueResult = GetDefault(valueType);
                        }

                        list.Add(valueResult);
                    }

                    result = list;
                    return true;
                }

                if (genericType == typeof(Dictionary<,>))
                {
                    var keyType = genericArguments.First();
                    if (keyType != typeof(string))
                    {
                        throw new InvalidOperationException(prefix + " must be Dictionary with string key");
                    }

                    var dictionary = (IDictionary)Activator.CreateInstance(type);
                    var valueType = type.GetGenericArguments().Skip(1).Single();

                    foreach (var collectionKey in distinctCollectionKeys)
                    {
                        var valuePrefix = string.Format("{0}[{1}]", prefix, collectionKey);

                        object valueResult;
                        if (!TryCreateObject(collection, valuePrefix, valueType, validate, out valueResult))
                        {
                            valueResult = GetDefault(valueType);
                        }

                        dictionary[collectionKey] = valueResult;
                    }

                    result = dictionary;
                    return true;
                }

                throw new NotSupportedException(prefix + " must be of type List<> or Dictionary<,>");
            }

            result = Activator.CreateInstance(type);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);

            foreach (var property in properties)
            {
                var propertyPrefix = prefix + "." + property.Name;

                object value;
                if (TryCreateObject(collection, propertyPrefix, property.PropertyType, validate, out value))
                {
                    property.SetValue(result, value);
                }
            }

            if (validate && ValidationEnabledAttribute.IsEnabled(result))
            {
                var context = new ValidationContext(result);
                Validator.ValidateObject(result, context, true);
            }

            return true;
        }

        private static bool TryGetValue(
            string prefix,
            Type type,
            string value,
            out object result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = null;
                return false;
            }

            if (type == typeof(TimeSpan))
            {
                result = TimeSpan.Parse(value);
                return true;
            }

            if (type.IsEnum)
            {
                result = Enum.Parse(type, value);
                return true;
            }

            if (type.IsPrimitive || SupportedByChangeType.Contains(type))
            {
                result = Convert.ChangeType(value, type);
                return true;
            }

            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType == null)
            {
                throw new NotSupportedException(prefix + " is not a convertable type");
            }

            return TryGetValue(prefix, underlyingType, value, out result);
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType
                ? Activator.CreateInstance(type)
                : null;
        }
    }
}
