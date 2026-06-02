using System;
using System.Threading.Tasks;
using Api.Translate;

namespace Api.TypeScript
{
    public partial class TypeScriptService : AutoService
    {
        /// <summary>
        /// Checks whether the provided type is a nullable type (e.g., int?, DateTime?).
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is nullable; otherwise, false.</returns>
        public static bool IsNullable(Type type)
        {
            // Reference types are considered nullable
            if (!type.IsValueType)
            {
                return true;
            }

            // Nullable value types are generic types of Nullable<T>
            return Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// True if the given type is a localized one. Optionally can be inside a ValueTask too.
        /// Returns the type inside Localized if so - note that the returned type can still be 
        /// nullable and not necessarily an entity type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static bool IsContentfulValueTask(Type type, out Type contentType)
        {
            while (true)
            {
                if (type is null)
                {
                    contentType = null;
                    return false;
                }
                // Check if the type is generic and is a ValueTask<>
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    type = type.GetGenericArguments()[0];
                    continue;
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Localized<>))
                {
                    contentType = type.GetGenericArguments()[0];
                    return true;
                }

                contentType = null;
                return false;
            }
        }

        /// <summary>
        /// Unwraps the first type from enumerable, nullable, valueTask and Localized.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type UnwrapTypeNesting(Type type)
        {
            if (type.IsArray)
            {
                type = type.GetElementType();
            }

            if (IsContentfulValueTask(type, out var resolved))
            {
                type = resolved;
            }

            if (IsNullable(type))
            {
                type = Nullable.GetUnderlyingType(type) ?? type;
            }

            if (IsEnumerable(type) && type.GetGenericArguments().Length != 0)
            {
                type = type.GetGenericArguments()[0];
            }

            return type;
        }
        
        /// <summary>
        /// True if the given type is enumerable. Can be nested in valuetask/ nullable.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNestedCollection(Type type)
        {
            if (IsContentfulValueTask(type, out var resolved))
            {
                type = resolved;
            }

            if (IsNullable(type))
            {
                type = Nullable.GetUnderlyingType(type) ?? type;
            }

            if (IsEnumerable(type))
            {
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Converts the first character of the string to lowercase.
        /// If the string is null, empty, or the first character is already lowercase, the original string is returned.
        /// </summary>
        /// <param name="input">The string to transform.</param>
        /// <returns>The string with its first character lowercased.</returns>
        public static string LcFirst(string input)
        {
            if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
            {
                return input;
            }

            return char.ToLower(input[0]) + input[1..];
        }

    }
}