using System;
using System.Collections.Generic;

namespace Api.AutoForms
{
	/// <summary>
	/// Use this to define a prop value for your UI module. [Data("placeholder", "Enter a name here")]
	/// It's the same structure as canvas JSON.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	internal class DataAttribute : Attribute
	{
		/// <summary>
		/// The prop name.
		/// </summary>
		public string Name;

		/// <summary>
		/// The value to use. Typically strings and numbers.
		/// </summary>
		public object Value;

		public DataAttribute(string name, object value)
		{
			Name = name;
			Value = value;
		}

	}
}
