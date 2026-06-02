using System;

namespace Api.Startup
{
    /// <summary>
    /// An attribute to specify the return type of a method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ReturnsAttribute : Attribute
    {
        /// <summary>
        /// Gets the return type specified by the attribute.
        /// </summary>
        public readonly Type ReturnType;


        /// <summary>
        /// Initializes a new instance of the <see cref="ReturnsAttribute"/> class.
        /// </summary>
        /// <param name="type">The return type of the method.</param>
        public ReturnsAttribute(Type type)
        {
            ReturnType = type;
        }
    }

	/// <summary>
	/// An attribute to specify the body type of a method. This is used when it receives a complex object.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public class ReceivesAttribute : Attribute
	{
		/// <summary>
		/// Gets the body type specified by the attribute.
		/// </summary>
		public readonly Type RecievesType;


		/// <summary>
		/// Initializes a new instance of the <see cref="ReceivesAttribute"/> class.
		/// </summary>
		/// <param name="type">The body type for the method.</param>
		public ReceivesAttribute(Type type)
		{
			RecievesType = type;
		}
	}

	/// <summary>
	/// An attribute to specify the name of a type. This is used when type names collide (e.g. Content collides with its generic variation).
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class JsonTypeNameAttribute : Attribute
	{
		/// <summary>
		/// Gets the body type specified by the attribute.
		/// </summary>
		public readonly string Name;


		/// <summary>
		/// Initializes a new instance of the <see cref="JsonTypeNameAttribute"/> class.
		/// </summary>
		/// <param name="name">The name for the type.</param>
		public JsonTypeNameAttribute(string name)
		{
			Name = name;
		}
	}

	/// <summary>
	/// An attribute to specify additional options to guide the typescript binder.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public class JsonOptionsAttribute : Attribute
	{
		/// <summary>
		/// True if the field is optional as seen by the typescript bindings.
		/// </summary>
		public bool Optional;

		/// <summary>
		/// Ensure the given type is present in the bindings (for representing the structure of the json).
		/// </summary>
		public Type PublicType;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonOptionsAttribute"/> class.
		/// </summary>
		public JsonOptionsAttribute()
		{
		}
	}
}