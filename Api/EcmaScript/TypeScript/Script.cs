

using System;
using System.Collections.Generic;

namespace Api.EcmaScript.TypeScript
{
    /// <summary>
    /// Represents a script or module within typescript.
    /// </summary>
    public partial class Script : IGeneratable
    {
        
        /// <summary>
        /// Where will this be saved to.
        /// </summary>
        public string FileName; 

        /// <summary>
        /// A list of imports for the current script/module
        /// </summary>
        public List<Import> Imports = [];

        /// <summary>
        /// A list of all nodes inside the script.
        /// </summary>
        public List<IGeneratable> Children = [];

        /// <summary>
        /// Inject custom lines of code into the script
        /// </summary>
        public List<string> Injected = [];

        /// <summary>
        /// Adds an import to the script/module
        /// </summary>
        /// <param name="import"></param>
        public void AddImport(Import import)
        {
            Imports.Add(import);
        }

        /// <summary>
        /// Add a single source line of code.
        /// </summary>
        /// <param name="sloc"></param>
        public void AddSLOC(string sloc)
        {
            Injected.Add(sloc);
        }

        /// <summary>
        /// Adds a child source generator inside the script.
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(IGeneratable child)
        {
            Children.Add(child);
        }

        /// <summary>
        /// Generate the TypeScript
        /// </summary>
        /// <returns></returns>
        public string CreateSource()
        {
            var source = "/* * * * * * * | Auto Generated Script, do not edit | * * * * * * * */" + Environment.NewLine;

            source += "// Imports" + Environment.NewLine;
            foreach(var import in Imports)
            {
                source += import.CreateSource();
            }
            source += Environment.NewLine;
            source += "// Module" + Environment.NewLine;

            foreach(var child in Children)
            {
                source += child.CreateSource() + Environment.NewLine;
            }

            source += string.Join(Environment.NewLine, Injected) + Environment.NewLine;

            return source;
        }
    }
}