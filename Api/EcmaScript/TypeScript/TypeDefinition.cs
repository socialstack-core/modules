using System;
using System.Collections.Generic;
using System.Linq;
using Api.Startup;

namespace Api.EcmaScript.TypeScript
{
    /// <summary>
    /// Represents a TypeScript type definition that can be generated dynamically.
    /// </summary>
    public class TypeDefinition : IGeneratable
    {
        /// <summary>
        /// The name of the TypeScript type.
        /// </summary>
        public string Name;

        /// <summary>
        /// The generic type template if the type has generics.
        /// </summary>
        public string GenericTemplate;

        /// <summary>
        /// A list of inherited types or interfaces.
        /// </summary>
        public List<string> Inheritence = [];

        /// <summary>
        /// A dictionary containing the properties of the TypeScript type, where the key is the property name and the value is the type.
        /// </summary>
        public Dictionary<string, string> Properties = [];

        /// <summary>
        /// Sets the name of the type.
        /// </summary>
        /// <param name="name">The name to set.</param>
        public void SetName(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Sets the generic type template.
        /// </summary>
        /// <param name="template">The generic type template.</param>
        public void SetGenericTemplate(string template)
        {
            GenericTemplate = template;
        }

        /// <summary>
        /// Adds an inherited type or interface.
        /// </summary>
        /// <param name="inherit">The name of the inherited type.</param>
        public void AddInheritence(string inherit)
        {
            Inheritence.Add(inherit);
        }

        /// <summary>
        /// Adds a property to the type definition.
        /// </summary>
        /// <param name="k">The property name.</param>
        /// <param name="v">The property type.</param>
        public void AddProperty(string k, string v)
        {
            Properties[k] = v.Contains('`') ? v[..v.IndexOf('`')] : v;
        }

        /// <summary>
        /// Generates the TypeScript type definition as a source code string.
        /// </summary>
        /// <returns>The TypeScript type definition as a string.</returns>
        public string CreateSource()
        {
            var source = "export type " + Name;

            if (!string.IsNullOrEmpty(GenericTemplate))
            {
                source += "<" + GenericTemplate + ">";
            }

            source += " = ";

            if (Inheritence.Any())
            {
                source += string.Join(" & ", Inheritence) + " & ";
            }

            source += "{" + Environment.NewLine;

            foreach (var record in Properties)
            {
                source += "".PadLeft(4) + $"{EcmaService.LcFirst(record.Key)}?: {record.Value}," + Environment.NewLine;
            }

            source += "}" + Environment.NewLine;
            return source;
        }
    }
}
