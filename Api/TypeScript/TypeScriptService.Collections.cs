using System;
using System.Collections;
using System.Collections.Generic;

namespace Api.TypeScript
{
    /// <summary>
    /// Provides utility functions for resolving .NET types into TypeScript-compatible constructs.
    /// </summary>
    public partial class TypeScriptService : AutoService
    {
        /// <summary>
        /// Determines whether the specified type is considered enumerable (e.g., arrays, lists, collections).
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns><c>true</c> if the type implements <see cref="IEnumerable"/> and is not a string; otherwise, <c>false</c>.</returns>
        public static bool IsEnumerable(Type type)
        {
            return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
        }

        /// <summary>
        /// Determines whether the specified type is considered dictionary-like 
        /// (e.g., <see cref="Dictionary{TKey, TValue}"/>, <see cref="IDictionary"/>).
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns><c>true</c> if the type represents a dictionary or key-value store; otherwise, <c>false</c>.</returns>
        public bool IsDictionaryLike(Type type)
        {
            if (type is null)
            {
                return false;
            }

            // Non-generic IDictionary
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return true;
            }

            // Generic IDictionary<,> or Dictionary<,>
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(IDictionary<,>) || genericDef == typeof(Dictionary<,>))
                {
                    return true;
                }

                // Check interfaces
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType &&
                        iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to get the element type of an enumerable type (e.g., <c>List&lt;T&gt;</c>, <c>T[]</c>).
        /// </summary>
        /// <param name="type">The enumerable type.</param>
        /// <returns>The element type if found; otherwise, <c>null</c>.</returns>
        public Type GetEnumerableElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (typeof(IEnumerable<>).IsAssignableFrom(genericDef))
                {
                    return type.GetGenericArguments()[0];
                }

                // Check implemented interfaces
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType &&
                        iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        return iface.GetGenericArguments()[0];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to extract the key and value types of a dictionary-like type.
        /// </summary>
        /// <param name="type">The type to analyze.</param>
        /// <param name="keyType">Outputs the key type if found.</param>
        /// <param name="valueType">Outputs the value type if found.</param>
        /// <returns><c>true</c> if the type is a dictionary and types were extracted; otherwise, <c>false</c>.</returns>
        public bool GetDictionaryKeyValueTypes(Type type, out Type keyType, out Type valueType)
        {
            keyType = null;
            valueType = null;

            if (type == null)
            {
                return false;
            }

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(IDictionary<,>) || genericDef == typeof(Dictionary<,>))
                {
                    var args = type.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                    return true;
                }

                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType &&
                        iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    {
                        var args = iface.GetGenericArguments();
                        keyType = args[0];
                        valueType = args[1];
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="returnType"></param>
        /// <returns></returns>
        public bool IsEntityCollection(Type returnType)
        {
            if (!IsEnumerable(returnType))
            {
                return false;
            }
            var target = returnType.GetGenericArguments()[0];

            return IsEntityType(target);
        }
    }
}
