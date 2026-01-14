using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Api.Startup;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Represents a TypeScript code generator for "includes" structures,
    /// supporting dynamic property chaining for virtual fields and entity-specific includes.
    /// </summary>
    /// <remarks>
    /// This class is used to generate TypeScript source that enables inclusion chains, commonly
    /// used in query systems (e.g., GraphQL-style field selection or API expansions).
    /// 
    /// It builds a base <c>ApiIncludes</c> class and, for each added entity, a corresponding
    /// <c>{Entity}Includes</c> class with accessors for virtual fields defined via
    /// <see cref="HasVirtualFieldAttribute"/>.
    /// </remarks>
    public class ApiIncludes : AbstractTypeScriptObject
    {
        private List<Type> _entities = [];

        private List<Type> _secondaryIncludes = [];

        /// <summary>
        /// Registers an entity type to be included in the TypeScript generation output.
        /// </summary>
        /// <param name="entityType">
        /// The CLR <see cref="Type"/> of the entity for which a corresponding <c>{Entity}Includes</c>
        /// TypeScript class will be generated.
        /// </param>
        public void AddEntity(Type entityType)
        {
            _entities.Add(entityType);
        }

        /// <summary>
        /// Appends the TypeScript source code that defines inclusion helpers for all registered entities.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder"/> to append the TypeScript source output to.
        /// </param>
        /// <param name="svc">
        /// A <see cref="TypeScriptService"/> that provides utilities like naming and formatting helpers.
        /// </param>
        /// <remarks>
        /// Generates:
        /// <list type="bullet">
        /// <item><description>The base <c>ApiIncludes</c> class with virtual field accessors from global definitions.</description></item>
        /// <item><description>One <c>{Entity}Includes</c> class per entity, reflecting its own virtual fields.</description></item>
        /// </list>
        /// These classes allow client-side TypeScript code to fluently chain field access expressions for API consumption.
        /// </remarks>
        public override void ToSource(StringBuilder builder, TypeScriptService svc)
        {
            builder.AppendLine("export class ApiIncludes {");
            
            
            builder.AppendLine("    private text: string = '';");
            builder.AppendLine();
            builder.AppendLine("    constructor(existing: string = '', addition: string = ''){");
            builder.AppendLine("        this.text = (existing.length != 0) ? existing : '';");
            builder.AppendLine("        if (addition.length != 0) {");
            builder.AppendLine("             if (this.text != ''){");
            builder.AppendLine("                this.text += '.'");
            builder.AppendLine("             }");
            builder.AppendLine("             this.text += addition;");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            
            builder.AppendLine();
            builder.AppendLine("    toString(){ return this.text }");
            
            builder.AppendLine("}");
            builder.AppendLine("export class TerminalIncludes extends ApiIncludes {}");
            
            builder.AppendLine("export class ApiGlobalIncludes extends ApiIncludes {");

            builder.AppendLine();

            builder.AppendLine("    get all(){ ");
            builder.AppendLine("          return new TerminalIncludes(this.toString(), '*'); ");
            builder.AppendLine("    }");

            // Generate virtual field accessors from global virtual fields.
            foreach (var kvp in ContentFields.GlobalVirtualFields)
            {
                var virtualInfo = kvp.Value.VirtualInfo;

                Type virtualType = null;
                
                if(virtualInfo.DynamicTypeField != null){
                    virtualType = typeof(object);
                } else if(virtualInfo.ValueGeneratorType != null) {
                    virtualType = virtualInfo.GetValueGeneratorOutputType();
                } else {
                    virtualType = virtualInfo.Type;
                }

                if (virtualInfo.ValueGeneratorType != null || !TypeScriptService.IsEntityType(virtualType))
                {
                    // Functional includes and anything that is not an entity type with its own includes set.
                    builder.AppendLine($"    get {TypeScriptService.LcFirst(kvp.Key)}() {{");
                    builder.AppendLine($"        return new TerminalIncludes(this.toString(), '{TypeScriptService.LcFirst(kvp.Key)}');");
                    builder.AppendLine("    }");
                }
                else
                {
                    builder.AppendLine($"    get {TypeScriptService.LcFirst(kvp.Key)}() {{");
                    builder.AppendLine($"        return new {virtualType.Name}Includes(this.toString(), '{TypeScriptService.LcFirst(kvp.Key)}');");
                    builder.AppendLine("    }");
                }
            }

            builder.AppendLine("}");

            // Generate entity-specific includes classes
            foreach (var entity in _entities)
            {
                builder.AppendLine();
                builder.AppendLine($"export class {entity.Name}Includes extends ApiGlobalIncludes {{");

                var virtuals = entity.GetCustomAttributes<HasVirtualFieldAttribute>();

                foreach (var virtualField in virtuals)
                {

                    if (virtualField.Type is null)
                    {
                        builder.AppendLine($"    get {TypeScriptService.LcFirst(virtualField.FieldName)}() {{");
                        builder.AppendLine($"        return new ApiIncludes(this.toString(), '{virtualField.FieldName.ToLower()}');");
                        builder.AppendLine("    }");
                        continue;
                    }
                    
                    builder.AppendLine($"    get {TypeScriptService.LcFirst(virtualField.FieldName)}() {{");

                    var targetType = virtualField.Type.Name;
                    
                    
                    
                    builder.AppendLine($"        return new {targetType}Includes(this.toString(), '{virtualField.FieldName.ToLower()}');");
                    builder.AppendLine("    }");
                }
                
                AddSecondaryResultIncludes(entity, builder);

                builder.AppendLine("}");
            }

            builder.AppendLine("export class SecondaryIncludes extends ApiGlobalIncludes {");
            builder.AppendLine("    private secondaryIncludeString: string;");
            builder.AppendLine("    constructor(existing: string = '', addition: string = ''){");
            builder.AppendLine("        super('','');");
            builder.AppendLine("        let src = existing.startsWith('secondary') ? existing : 'secondary';");
            builder.AppendLine("        this.secondaryIncludeString = src + '.' + (addition.length != 0 ? addition : '');");    
            builder.AppendLine("    }");
            builder.AppendLine("    toString(){ return this.secondaryIncludeString }");
            builder.AppendLine("}");

            foreach (var entity in _secondaryIncludes)
            {
                CreateSecondaryIncludeClass(entity, builder);
            }
        }

        private void AddSecondaryResultIncludes(Type type, StringBuilder builder)
        {
            var secondaryIncludes = type.GetCustomAttributes<HasSecondaryResultAttribute>();

            var hasSecondaryResultAttributes = secondaryIncludes as HasSecondaryResultAttribute[] ?? secondaryIncludes.ToArray();
            
            if (hasSecondaryResultAttributes.Length == 0)
            {
                return;
            }

            foreach (var include in hasSecondaryResultAttributes)
            {
                _secondaryIncludes.Add(include.Type);
                
                builder.AppendLine($"    get {include.FieldName}() {{");
                builder.AppendLine($"          return new {include.Type.Name}Includes(this.toString(), '{include.FieldName.ToLower()}');");
                builder.AppendLine("    }");
            }
        }

        private void CreateSecondaryIncludeClass(Type type, StringBuilder builder)
        {
            builder.AppendLine($"export class {type.Name}Includes extends SecondaryIncludes {{");
            
            var hasVirtField = type.GetCustomAttributes<HasVirtualFieldAttribute>();

            foreach (var virtualField in hasVirtField)
            {
                builder.AppendLine($"    get {TypeScriptService.LcFirst(virtualField.FieldName)}() {{");
                builder.AppendLine($"        return new {virtualField.Type.Name}Includes(this.toString(), '{virtualField.FieldName.ToLower()}');");
                builder.AppendLine("    }");
            }
            
            builder.AppendLine("}");
        }
    }
}
