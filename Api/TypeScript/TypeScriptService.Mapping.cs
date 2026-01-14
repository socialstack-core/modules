using System;
using System.Collections.Generic;

namespace Api.TypeScript
{
    public partial class TypeScriptService : AutoService
    {
        /// <summary>
        /// Adds the mechanic to allow overrides.
        /// </summary>
        private readonly Dictionary<Type, string> _overwriteTypes = [];

        private readonly List<Type> _ignoreTypes = [];

        /// <summary>
        /// Sets or replaces the TypeScript overwrite mapping for a given .NET type.
        /// </summary>
        public void SetTypeOverwrite(Type type, string overwrite)
        {
            if (type is null || string.IsNullOrWhiteSpace(overwrite))
            {
                return;
            }

            _overwriteTypes[type] = overwrite; // replaces or adds
        }

        /// <summary>
        /// Gets the TypeScript overwrite string for a given .NET type.
        /// </summary>
        /// <param name="type">The type to lookup.</param>
        /// <returns>The overwrite string if one exists; otherwise, null.</returns>
        public string GetTypeOverwrite(Type type)
        {
            if (type is null)
            {
                return null;
            }

            if (!type.IsArray)
            {
                return _overwriteTypes.GetValueOrDefault(type);
            }

            var elementType = type.GetElementType();

            var typeStr = _overwriteTypes.GetValueOrDefault(elementType);
            if (typeStr is null)
            {
                return null;
            }

            // Append array brackets based on the array rank of the actual array type
            for (var i = 0; i < type.GetArrayRank(); i++)
            {
                typeStr += "[]";
            }

            return typeStr;
        }

        /// <summary>
        /// Ignores the given type
        /// </summary>
        /// <param name="type"></param>
        public void AddIgnoreType(Type type)
        {
            _ignoreTypes.Add(type);
        }

        /// <summary>
        /// True if the given type is ignored
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsIgnoreType(Type type)
        {
            return _ignoreTypes.Contains(type);
        }
    }
}