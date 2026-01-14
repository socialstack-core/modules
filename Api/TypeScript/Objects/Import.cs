using System;
using System.Collections.Generic;
using System.Text;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Represents a TypeScript import statement.
    /// Contains the symbols to be imported from another module.
    /// </summary>
    public class Import : AbstractTypeScriptObject
    {
        /// <summary>
        /// The module to import from.
        /// </summary>
        public ESModule from;

        /// <summary>
        /// List of symbols (classes, enums, etc.) to import.
        /// </summary>
        public List<string> Symbols { get; set; } = [];

        /// <summary>
        /// Emits the TypeScript import statement to the output builder.
        /// </summary>
        /// <param name="builder">The string builder used to construct the module's source.</param>
        /// <param name="svc">The TypeScript service context.</param>
        public override void ToSource(StringBuilder builder, TypeScriptService svc)
        {
            if (Symbols.Count == 0 || from == null)
            {
                return;
            }

            builder.AppendLine();
            builder.AppendLine($"import {{ {string.Join(", ", Symbols)} }} from '{from.GetImportPath()}';");
        }
    }
}