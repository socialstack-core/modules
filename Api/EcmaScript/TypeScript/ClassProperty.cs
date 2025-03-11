using System;
using Api.ContentSync;

namespace Api.EcmaScript.TypeScript
{
    /// <summary>
    /// Represents a property within a TypeScript class.
    /// </summary>
    public partial class ClassProperty : IGeneratable
    {
        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the TypeScript type of the property.
        /// </summary>
        public string PropertyType { get; set; }

        /// <summary>
        /// Gets or sets the default value of the property.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the visibility of the property (e.g., public, private, protected).
        /// Defaults to "public".
        /// </summary>
        public string Visibility { get; set; } = "public";

        /// <summary>
        /// Generates the TypeScript property definition as a source code string.
        /// </summary>
        /// <returns>The TypeScript property definition as a formatted string.</returns>
        public string CreateSource()
        {
            var src = "".PadLeft(4) + $"{Visibility} {PropertyName}: {PropertyType}";

            if (!string.IsNullOrEmpty(DefaultValue))
            {
                src += " = ";
                if (PropertyType == "string")
                {
                    src += $"'{DefaultValue.Replace("'", "\\'")}'";
                }
                else
                {
                    src += DefaultValue;
                }
            }

            src += ";" + Environment.NewLine;

            return src;
        }
    }
}
