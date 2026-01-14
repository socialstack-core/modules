using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api.Startup;
using Api.Translate;

namespace Api.TypeScript
{
    public partial class TypeScriptService : AutoService
    {
        /// <summary>
        /// Resolves the actual underlying type of a given <see cref="System.Type"/> by:
        /// - Unwrapping <see cref="Nullable{T}"/> to <c>T</c>
        /// - Extracting the inner type from <see cref="Task{TResult}"/> and <see cref="ValueTask{TResult}"/>
        /// - Mapping non-generic <see cref="Task"/> and <see cref="ValueTask"/> to <see cref="void"/>
        /// </summary>
        /// <param name="t">The type to resolve.</param>
        /// <returns>The resolved underlying type, if applicable; otherwise, the original type.</returns>
        /// <remarks>
        /// This method is useful for determining the "real" return type of asynchronous or nullable constructs,
        /// which are commonly used in API controllers and service contracts.
        /// </remarks>
        public Type ResolveType(Type t)
        {
            // Unwrap Nullable<T> to T
            t = Nullable.GetUnderlyingType(t) ?? t;

            // Handle Task<T> or ValueTask<T>
            if (t.IsGenericType)
            {
                var genericDef = t.GetGenericTypeDefinition();
                if (genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>))
                {
                    // Extract and return the inner result type
                    return t.GetGenericArguments()[0];
                }
            }

            // Map Task or ValueTask (non-generic) to void
            if (t == typeof(Task) || t == typeof(ValueTask))
            {
                return typeof(void);
            }

            // Return type as-is if no transformation is required
            return t;
        }

        /// <summary>
        /// Generates a readable generic type signature from a given .NET type.
        /// For example, Dictionary&lt;string, List&lt;int&gt;&gt; will return "Dictionary&lt;string, List&lt;int&gt;&gt;".
        /// Handles both open and closed generic types.
        /// </summary>
        /// <param name="t">The type to generate a signature for.</param>
        /// <returns>A string representation of the generic type.</returns>
        public string GetGenericSignature(Type t)
        {
            if (t == null)
            {
                return "null";
            }
            var baseNullable = Nullable.GetUnderlyingType(t);
            
            if(baseNullable != null)
            {
                return "(" + GetGenericSignature(baseNullable) + " | undefined)";
            }

            if(t.IsArray)
            {
                var result = GetGenericSignature(t.GetElementType());
                var arrayDepth = t.GetArrayRank();

                if(arrayDepth > 1)
                {
                    if (Nullable.GetUnderlyingType(t) != null)
                    {
                        t = Nullable.GetUnderlyingType(t) ?? t;
                    }
                    throw new NotSupportedException("You can't return multidimensional arrays through the public API as they are not supported in Typescript. The type attempted was an Enumerable which holds a " + UnwrapTypeNesting(t.GetElementType()).Name);
                }
                return result + "[]";
            }

            if(t.IsGenericType)
			{
				var args = t.GetGenericArguments();

                if (t.IsGenericTypeDefinition)
                {
                    var name = TidyGenericName(t.Name) + "<";

                    for (var i = 0; i < args.Length; i++)
                    {
                        if (i > 0)
                        {
                            name += ", ";
                        }
                        name += GetGenericSignature(args[i]);
                    }

                    return name + ">";
                }

				var baseType = t.GetGenericTypeDefinition();

                if(baseType == typeof(List<>))
                {
                    return GetGenericSignature(args[0]) + "[]";
                }
                if(baseType == typeof(IEnumerable<>))
                {
                    return GetGenericSignature(args[0]) + "[]";
                }
                if(baseType == typeof(Dictionary<,>))
                {
                    return "Record<" + GetGenericSignature(args[0]) + "," + GetGenericSignature(args[1]) + ">";
                }
                if(baseType == typeof(Task<>) || baseType == typeof(ValueTask<>))
                {
                    return "Promise<" + GetGenericSignature(args[0]) + ">";
                }
                if (baseType == typeof(ContentStream<,>))
                {
                    return "ApiList<" + GetGenericSignature(args[0]) + ">";
                }
                if (t.FullName is not null && TidyGenericName(t.FullName) == "Api.Translate.Localized")
                {
                    return GetGenericSignature(t.GetGenericArguments()[0]);
                }
                if (TryGetClass("Api.Revisions.Revision", out var type))
                {
                    if (t == type)
                    {
                        return GetGenericSignature(args[0]);
                    }    
                }

                var overwrite = GetTypeOverwrite(t);
                var sb = new StringBuilder();

                if (overwrite != null)
                {
                    sb.Append(overwrite + "<");
                }
                else
                {
                    sb.Append(TidyGenericName(t.Name) + "<");
                }

                for (var i = 0; i < args.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(GetGenericSignature(args[i]));
                }

                sb.Append('>');

                return sb.ToString();

            }

            var overwriteNonGeneric = GetTypeOverwrite(t);

            return overwriteNonGeneric ?? (
                string.IsNullOrEmpty(t.Name) ? "never" : t.Name
            );
        }

        
        /// <summary>
        /// Strips the backtick segment of generic type names.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static string TidyGenericName(string typeName)
        {
            var backtick = typeName.IndexOf('`');

			if (backtick != -1)
			{
				return typeName[0..backtick];
			}

            return typeName;
		}

        private static readonly Dictionary<string, Type> _tryGetClassCache = [];
        
        private static bool TryGetClass(string typeName, out Type type)
        {
            if (_tryGetClassCache.TryGetValue(typeName, out type))
            {
                return true;
            }
            var assembly = typeof(TypeScriptService).Assembly;

            type = assembly.GetTypes().FirstOrDefault(searchType => searchType.FullName is not null && TidyGenericName(searchType.FullName).Equals(typeName));

            if (type is null)
            {
                return false;
            }
            _tryGetClassCache[typeName] = type;
            return true;

        }
    }
}