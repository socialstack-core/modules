using System.Text;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Represents an abstract base class for objects that can generate
    /// their TypeScript source code representation.
    /// </summary>
    /// <remarks>
    /// Inherit from this class to define custom TypeScript object representations
    /// that can be serialized to TypeScript source using a provided
    /// <see cref="TypeScriptService"/>.
    /// </remarks>
    public abstract class AbstractTypeScriptObject
    {
        /// <summary>
        /// Appends the TypeScript source code representation of this object
        /// to the specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder"/> to which the TypeScript source will be appended.
        /// This allows for efficient concatenation and composition of source output.
        /// </param>
        /// <param name="svc">
        /// An instance of <see cref="TypeScriptService"/> providing services and context
        /// required for generating valid TypeScript code, such as formatting rules,
        /// naming conventions, and type resolution.
        /// </param>
        /// <remarks>
        /// Derived classes must implement this method to define how the object is
        /// translated into TypeScript syntax. This method is typically used in code
        /// generation pipelines where .NET backend models are translated into
        /// client-side TypeScript definitions.
        /// </remarks>
        public abstract void ToSource(StringBuilder builder, TypeScriptService svc);
    }
}