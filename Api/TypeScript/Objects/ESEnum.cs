using System;
using System.Text;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Generates a TypeScript <c>enum</c> declaration from a referenced C# enum type.
    /// </summary>
    /// <remarks>
    /// Used by the TypeScript generation system to convert backend C# enums into corresponding TypeScript definitions,
    /// preserving all named enum values. This helps maintain strict type safety across the API boundary.
    /// </remarks>
    public class ESEnum : AbstractTypeScriptObject
    {
        /// <summary>
        /// The backing C# enum type from which this TypeScript enum is generated.
        /// </summary>
        private readonly Type _referenceEnum;

        /// <summary>
        /// Gets the referenced enum type that this instance wraps.
        /// </summary>
        /// <returns>The <see cref="Type"/> of the C# enum.</returns>
        public Type GetReferenceType() => _referenceEnum;

        /// <summary>
        /// Constructs an <see cref="ESEnum"/> to generate a TypeScript enum from a given C# enum type.
        /// </summary>
        /// <param name="referenceEnum">The C# enum type to wrap.</param>
        /// <param name="container">The module that this enum is part of (not currently used here).</param>
        public ESEnum(Type referenceEnum, ESModule container)
        {
            _referenceEnum = referenceEnum;
            // Note: container is accepted for consistency/future use but is unused in this implementation.
        }

        /// <summary>
        /// Appends the TypeScript source code for this enum to the provided string builder.
        /// </summary>
        /// <param name="builder">The output <see cref="StringBuilder"/> to append TypeScript code to.</param>
        /// <param name="svc">The TypeScript service used during code generation (not used here).</param>
        public override void ToSource(StringBuilder builder, TypeScriptService svc)
        {
            builder.AppendLine();
            builder.AppendLine($"export enum {_referenceEnum.Name} {{");

			// Write each enum member
			var fields = _referenceEnum.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

			foreach (var field in fields)
			{
				string name = field.Name;
				object value = field.GetRawConstantValue();

                if (value == null)
                {
                    // ?!
                    builder.AppendLine($"    {name}=null,");
                }
                else if (value is string)
                {
					builder.AppendLine($"    {name}=\"{value}\",");
				}
                else
                {
                    // assume numeric
					builder.AppendLine($"    {name}={value},");
				}

			}

            builder.AppendLine("}");
        }
    }
}
