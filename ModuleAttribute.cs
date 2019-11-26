using System;
using System.Collections.Generic;

namespace Api.AutoForms
{
	/// <summary>
	/// Use this to define a custom UI module to use when rendering this field. E.g. [Module("UI/TagInput")]
	/// It's the same structure as canvas JSON.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	internal class ModuleAttribute : Attribute
	{
		/// <summary>
		/// The name of the module. E.g. "UI/TagInput".
		/// </summary>
		public string Name;
		
		public ModuleAttribute(string name)
		{
			Name = name;
		}

	}
}
