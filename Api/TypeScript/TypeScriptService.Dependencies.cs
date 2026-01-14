using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Api.TypeScript
{
    /// <summary>
    /// A service responsible for analyzing .NET types and their dependencies
    /// to generate metadata suitable for TypeScript transformation.
    /// </summary>
    public partial class TypeScriptService : AutoService
    {
        private readonly List<Type> _allTypes = [];
        private readonly List<Type> _ignoreDeps = [];
        private static readonly List<string> _namespaceIgnores = [];

        // Tracks types currently being processed to prevent stack overflows from cycles.
        private readonly HashSet<Type> _processingTypes = [];

        /// <summary>
        /// Add a type to ignore.
        /// </summary>
        public void IgnoreType(Type t) => _ignoreDeps.Add(t);
        
        /// <summary>
        /// Ignores a whole namespace.
        /// </summary>
        /// <param name="ns"></param>
        public void IgnoreNamespace(string ns) => _namespaceIgnores.Add(ns);

        /// <summary>
        /// Gets all collected types.
        /// </summary>
        public List<Type> GetAllTypes() => _allTypes;

        /// <summary>
        /// Adds a type and all of its transformable dependencies to the internal type list.
        /// </summary>
        public void AddType(Type type)
        {
            if (
                type is null || 
                _processingTypes.Contains(type) || 
                !CanTransform(type) || 
                _allTypes.Contains(type) || 
                _namespaceIgnores.Any(ns => type.FullName is not null && type.FullName.StartsWith(ns)))
            {
                return;
            }

            _processingTypes.Add(type);

            if (type.BaseType is not null && CanTransform(type.BaseType))
            {
                AddType(type.BaseType);
            }

            foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var valueType = member switch
                {
                    FieldInfo f => f.FieldType,
                    PropertyInfo p => p.PropertyType,
                    _ => null
                };

                if (valueType is null)
                {
                    continue;
                }

                valueType = ResolveType(valueType);

                if (IsEnumerable(valueType))
                {
                    valueType = GetEnumerableElementType(valueType);
                }

                if (IsDictionaryLike(valueType))
                {
                    if (GetDictionaryKeyValueTypes(valueType, out var keyType, out var valType))
                    {
                        if (CanTransform(keyType)) 
                        {
                            AddType(keyType);
                        }
                        if (CanTransform(valType)) 
                        {
                            AddType(valType);
                        }
                    }
                    continue;
                }

                if (CanTransform(valueType))
                {
                    AddType(valueType);
                }
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var returnType = ResolveType(method.ReturnType);

                if (CanTransform(returnType))
                {
                    AddType(returnType);
                }

                foreach (var param in method.GetParameters())
                {
                    var paramType = ResolveType(param.ParameterType);

                    if (CanTransform(paramType))
                    {
                        if (IsEnumerable(paramType))
                        {
                            paramType = GetEnumerableElementType(paramType);
                        }

                        if (IsDictionaryLike(paramType))
                        {
                            if (GetDictionaryKeyValueTypes(paramType, out var keyType, out var valType))
                            {
                                if (CanTransform(keyType)) 
                                {
                                    AddType(keyType);
                                }
                                if (CanTransform(valType)) 
                                {
                                    AddType(valType);
                                }
                            }
                            continue;
                        }
                        AddType(paramType);
                    }
                }
            }

            _allTypes.Add(type);
            _processingTypes.Remove(type);
            AddType(type);
            
        }

        /// <summary>
        /// Determines whether a given type is eligible for transformation.
        /// </summary>
        private bool CanTransform(Type type)
        {
            if (type is null)
            {
                return false;
            }

            if (type == typeof(AutoController))
            {
                return false;
            }

            if (IsDictionaryLike(type))
            {
                return false;
            }

            if (_ignoreDeps.Contains(type))
            {
                return false;
            }

            if (_allTypes.Contains(type))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                if(_namespaceIgnores.Any(n => type.Namespace.StartsWith(n)))
                {
                    return false;
                }
            }
            if (type.IsGenericType)
            {
                return false;
            }
            return true;
        }
    }
}
