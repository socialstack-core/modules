using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Api.Startup;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Represents a type definition used for TypeScript code generation or type analysis.
    /// This class collects the declared public instance members (fields and properties)
    /// and tracks their types as dependencies.
    /// </summary>
    public class TypeDefinition : AbstractTypeScriptObject
    {
        private readonly List<Type> _resolvedTypes = new();
        private readonly Type _definedType;
        private readonly ESModule _container;
        private List<TypeScriptField> _fields = [];
        

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDefinition"/> class by analyzing the given reference type.
        /// </summary>
        /// <param name="referenceType">The reference type to analyze and define.</param>
        /// <param name="container">The module in which it sits.</param>
        public TypeDefinition(Type referenceType, ESModule container)
        {
            referenceType = TypeScriptService.UnwrapTypeNesting(referenceType);
            _definedType = referenceType;
            _container = container;

            // Collect types from the referenceType's fields and properties
            foreach (var member in referenceType.GetMembers(BindingFlags.Instance | BindingFlags.Public |
                                                            BindingFlags.DeclaredOnly))
            {
                switch (member)
                {
                    case FieldInfo field:
                        
                        // due to there being 2 variants
                        // of JsonIgnore attribute, just check
                        // for either simply.
                        if (field.GetCustomAttributes().Any(a => a.GetType().Name == "JsonIgnoreAttribute"))
                        {
                            continue;
                        }
                        AddResolvedType(field.FieldType);
                        AddField(field.Name, field.FieldType);
                        break;
                    case PropertyInfo property:
                        // same here.
                        if (property.GetCustomAttributes().Any(a => a.GetType().Name == "JsonIgnoreAttribute"))
                        {
                            continue;
                        }
                        AddResolvedType(property.PropertyType);
                        AddField(property.Name, property.PropertyType);
                        break;
                }
            }

            // Add resolved types as dependencies
            foreach (var type in _resolvedTypes)
            {
                ProcessResolvedType(type, referenceType);
            }
        }

        /// <summary>
        /// Defines a new field on this type
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldType"></param>
        public void AddField(string fieldName, Type fieldType)
        {
            _fields.Add(new()
            {
                FieldName = fieldName,
                FieldType = fieldType
            });
        }

        private void AddResolvedType(Type type)
        {
            var unwrappedType = TypeScriptService.UnwrapTypeNesting(type);
            if (unwrappedType == GetReferenceType()) return;
            if (!_resolvedTypes.Contains(unwrappedType))
            {
                _resolvedTypes.Add(unwrappedType);
            }
        }

        private void ProcessResolvedType(Type type, Type referenceType)
        {
            // Skip primitive types or System namespace types unless they are POCOs
            if (type.IsPrimitive || type.Namespace?.StartsWith("System") == true)
            {
                if (!(type.BaseType == typeof(object) && !type.IsPrimitive)) return;
            }

            if (type.IsEnum)
            {
                _container.AddEnum(type);
                return;
            }

            if (type == typeof(void) || type == typeof(string) || type == referenceType)
            {
                return;
            }

            // Add valid types to the container
            _container.AddType(type);
        }

        /// <summary>
        /// Imports virtual fields from the reference type and resolves missing types.
        /// </summary>
        public void ImportVirtualFields(List<ESModule> modules)
        {
            var virtualFields = GetReferenceType().GetCustomAttributes<HasVirtualFieldAttribute>().ToList();

            foreach (var virtualField in virtualFields)
            {
                if (TypeScriptService.IsEntityType(virtualField.Type))
                {
                    var existingInModule = modules.FirstOrDefault(m => m.IsEntity(virtualField.Type));
                    if (existingInModule != null)
                    {
                        _container.Import(virtualField.Type, existingInModule);
                    }
                }
                else
                {;
                    _container.AddType(virtualField.Type);
                }
            }
        }

        /// <summary>
        /// Returns the name of the type definition.
        /// </summary>
        /// <returns>The type name.</returns>
        public string GetName()
        {
            return GetReferenceType().Name;
        }

        /// <summary>
        /// Gets the original reference type from which this definition was created.
        /// </summary>
        /// <returns>The unwrapped reference <see cref="Type"/>.</returns>
        public Type GetReferenceType() => _definedType;

        /// <summary>
        /// Gets the list of all types that this type depends on (fields, properties, and added custom fields).
        /// </summary>
        /// <returns>A list of dependent <see cref="Type"/> objects.</returns>
        public List<Type> GetDependencies()
        {
            return _resolvedTypes;
        }

        /// <summary>
        /// Generates the TypeScript source code for the type definition.
        /// </summary>
        public override void ToSource(StringBuilder builder, TypeScriptService svc)
        {
            if (GetReferenceType().IsPrimitive || GetReferenceType().Namespace?.StartsWith("System") == true)
            {
                return;
            }

            builder.AppendLine();
            builder.AppendLine("/**");
            builder.AppendLine($"* This type was generated to reflect {{{GetName()}}} ({GetReferenceType().FullName})");
            builder.AppendLine("**/");

            builder.Append($"export type {svc.GetGenericSignature(GetReferenceType())} = ");

            // Handle inheritance
            if (GetReferenceType().BaseType is not null && GetReferenceType().BaseType != typeof(object) && GetReferenceType().BaseType != typeof(ValueType))
            {
                builder.Append(svc.GetGenericSignature(GetReferenceType().BaseType) + " & ");
            }

            builder.AppendLine("{");

            // Process virtual fields and regular fields/properties
            foreach (var field in _fields)
            {
                field.ToSource(builder, svc);    
            }
            
            // Process virtual fields
            ProcessVirtualFields(builder, svc);

            builder.AppendLine("}");
        }

        private void ProcessVirtualFields(StringBuilder builder, TypeScriptService svc)
        {
            var virtualFields = GetReferenceType().GetCustomAttributes<HasVirtualFieldAttribute>().ToList();
            if (virtualFields.Count != 0)
            {
                builder.AppendLine($"    // HasVirtualField() fields ({virtualFields.Count} in total)");
            }

            foreach (var virtualField in virtualFields)
            {
                if (virtualField.Type is null)
                {
                    builder.AppendLine($"    {TypeScriptService.LcFirst(virtualField.FieldName)}?: Content<uint>");
                }
                else
                {
                    builder.AppendLine($"    {TypeScriptService.LcFirst(virtualField.FieldName)}{(TypeScriptService.IsNullable(virtualField.Type) ? "?:" : ":")} {svc.GetGenericSignature(virtualField.Type)};");
                }
            }
        }
    }
}
