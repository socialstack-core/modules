using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Api.Database;
using Api.Startup;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Represents a list of generic types to be emitted as TypeScript type aliases.
    /// </summary>
    public class GenericTypeList : AbstractTypeScriptObject
    {
        private readonly List<Type> _contentTypes = [];

        private readonly List<Type> _requiredImports = [];

        private ESModule _container;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="container"></param>
        public GenericTypeList(ESModule container)
        {
            _container = container;
        }

        /// <summary>
        /// Adds a .NET type to the list of types to be emitted as TypeScript types.
        /// </summary>
        /// <param name="type">The .NET type to include in the output.</param>
        public void AddContentType(Type type)
        {
            _contentTypes.Add(type);
            var svc = Services.Get<TypeScriptService>();
    
            if (type == typeof(Content<>))
            {
                // load all required imports.
                List<Type> structsToGenerate = [];
                
                foreach (var globalField in ContentFields.GlobalVirtualFields.Values)
                {
                    var virtualInfo = globalField.VirtualInfo;

                    Type virtualType = null;

                    if(virtualInfo.DynamicTypeField != null){
                        virtualType = typeof(object);
                    } else if(virtualInfo.ValueGeneratorType != null) {
                        virtualType = virtualInfo.GetValueGeneratorOutputType();
					} else {
                        virtualType = virtualInfo.Type;
                    }

                    if(virtualInfo.IsList){
                        virtualType = virtualType.MakeArrayType();
                    }
                    virtualType = virtualType.IsArray ? virtualType.GetElementType() : virtualType;

                    if (
                        TypeScriptService.IsEntityType(virtualType) ||
                        (
                            typeof(IEnumerable<>).IsAssignableFrom(virtualType) &&
                            TypeScriptService.IsEntityType(virtualType.GetGenericArguments().First())
                        )
                    )
                    {
                        var existingModule = svc.modules.FirstOrDefault(m => m.IsEntity(virtualType));

                        if (existingModule is not null)
                        {
                            _container.AddRawImport(virtualType.Name, "Api/" + virtualType.Name);
                        }
                        else
                        {
                            // lets generate it .
                            _container.AddType(virtualType);
                        }
                    }
                    else
                    {
                        _container.AddType(virtualType);
                    }
                }
                
                foreach (var structure in structsToGenerate)
                {
                    _container.AddType(structure);
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<Type> GetRequiredImports()
        {
            return _requiredImports;
        }

        /// <summary>
        /// Emits TypeScript code for each generic type in the list.
        /// </summary>
        /// <param name="builder">The output string builder.</param>
        /// <param name="svc">The TypeScript service context.</param>
        public override void ToSource(StringBuilder builder, TypeScriptService svc)
        {
            foreach (var type in _contentTypes)
            {
                builder.AppendLine();
                builder.Append($"export type {svc.GetGenericSignature(type)} = ");

                // Extend base type if applicable
                if (type.BaseType is not null && type.BaseType != typeof(object) && type.BaseType != typeof(Content))
                {
                    builder.Append($"{svc.GetGenericSignature(type.BaseType)} & ");
                }

                builder.AppendLine("{");

                // Emit fields and properties
                foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    switch (member)
                    {
                        case FieldInfo field:
                            EmitField(builder, svc, field.Name, field.FieldType);
                            break;

                        case PropertyInfo prop:
                            EmitField(builder, svc, prop.Name, prop.PropertyType);
                            break;
                    }
                }


                // Emit global virtual fields if the type is Content<>
                if (type == typeof(Content<>))
                {
                    builder.AppendLine($"    // adding ({ContentFields.GlobalVirtualFields.Count}) global virtual fields.");
                    try
                    {
                        foreach (var globalField in ContentFields.GlobalVirtualFields.Values)
                        {
                            var virtualInfo = globalField.VirtualInfo;

                            Type virtualType = null;

                            if(virtualInfo.DynamicTypeField != null){
                                virtualType = typeof(object);
                            } else if(virtualInfo.ValueGeneratorType != null) {
                                virtualType = virtualInfo.GetValueGeneratorOutputType();
							} else {
                                virtualType = virtualInfo.Type;
                            }

                            if(virtualInfo.IsList){
                                virtualType = virtualType.MakeArrayType();
                            }
                            var fieldName = TypeScriptService.LcFirst(globalField.VirtualInfo?.FieldName);

                            if (
                                TypeScriptService.IsEntityType(virtualType) ||
                                (
                                    typeof(IEnumerable<>).IsAssignableFrom(virtualType) &&
                                    TypeScriptService.IsEntityType(virtualType.GetGenericArguments().First())
                                ) || (
                                    virtualType.IsArray && TypeScriptService.IsEntityType(virtualType.GetElementType())
                                )
                            )
                            {
                                // import, it's an entity and needs importing
                                
                                EmitField(builder, svc, fieldName, virtualType);
                                _requiredImports.Add(virtualType);
                            }
                            else
                            {
                                EmitField(builder, svc, fieldName, virtualType);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        builder.AppendLine($"    // Error writing global virtual fields: {ex.Message}");
                    }
                }
                builder.AppendLine("}");
                
                builder.AppendLine();
                
                
                
                builder.AppendLine();
            }
        }

        /// <summary>
        /// Emits a single field or property as a TypeScript type line.
        /// </summary>
        private static void EmitField(StringBuilder builder, TypeScriptService svc, string name, Type type)
        {
           var tsField = new TypeScriptField
           {
               FieldName = name,
               FieldType = type
           };
           tsField.ToSource(builder, svc);
        }
    }
}
