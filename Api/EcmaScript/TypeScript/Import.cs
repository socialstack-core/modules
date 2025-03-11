

using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.EcmaScript.TypeScript
{
    /// <summary>
    /// Represents an import statement.
    /// </summary>
    public partial class Import : IGeneratable
    {
        /// <summary>
        /// This is the default import
        /// </summary>
        public string DefaultImport;

        /// <summary>
        /// A list of imported objects
        /// </summary>
        public List<string> Symbols = [];

        /// <summary>
        /// Where we importin' from?
        /// </summary>
        public string From;

        /// <summary>
        /// Outputs the data as an import in TS
        /// </summary>
        /// <returns></returns>
        public string CreateSource()
        {
            var src = "import ";

            if (!string.IsNullOrEmpty(DefaultImport))
            {
                src += DefaultImport;
            }

            if (Symbols.Any())
            {
                if (!string.IsNullOrEmpty(DefaultImport))
                {
                    src += ", ";
                }
                src += "{" + string.Join(", ", Symbols) + "}";
            }

            src += $" from '{From}'" + Environment.NewLine;
            return src;
        }
    }
}