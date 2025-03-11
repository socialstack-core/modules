using System;
using System.Collections.Generic;

namespace Api.EcmaScript.TypeScript
{
    /// <summary>
    /// Represents a method within a TypeScript class.
    /// </summary>
    public partial class ClassMethod : IGeneratable
    {
        /// <summary>
        /// Gets or sets the method's visibility modifier (e.g., public, private).
        /// </summary>
        public string Modifier { get; set; } = "public";

        /// <summary>
        /// Gets or sets the name of the method.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the generic template definition for the method, if applicable.
        /// </summary>
        public string GenericTemplate { get; set; }

        /// <summary>
        /// Gets or sets the return type of the method.
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// Gets or sets the list of arguments for the method.
        /// </summary>
        public List<ClassMethodArgument> Arguments { get; set; } = [];

        /// <summary>
        /// Code injected into the function
        /// </summary>
        public List<string> Injected = [];

        /// <summary>
        /// Generates the TypeScript method definition as a source code string.
        /// </summary>
        /// <returns>The TypeScript method definition as a formatted string.</returns>
        public string CreateSource()
        {
            var src = "".PadLeft(4) + $"{Modifier} {Name}(";

            for (int i = 0; i < Arguments.Count; i++)
            {
                if (i > 0) 
                {
                    src += ", ";
                }
                src += Arguments[i].CreateSource();
            }
            if (string.IsNullOrEmpty(ReturnType))
            {
                // Usually constructor.
                src += "){" + Environment.NewLine;
            }
            else
            {
                src += $"): {ReturnType} {{" + Environment.NewLine;
            }
            foreach(var sloc in Injected)
            {
                src += "".PadLeft(8) + sloc + Environment.NewLine;
            }
            src += "".PadLeft(4) + "}" + Environment.NewLine;

            return src;
        }
    }
}
