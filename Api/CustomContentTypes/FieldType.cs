using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;


namespace Api.CustomContentTypes
{
	
	/// <summary>
	/// A particular field type.
	/// </summary>
	public class FieldType
	{
		/// <summary>
		/// Type name to use.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// System type.
		/// </summary>
		public Type Type;
		
		/// <summary>
		/// Parses and emits the default value. Note that if it is not set (field value is null or empty), this will not be called.
		/// </summary>
		public Action<ILGenerator, string> OnDefault;
		
		/// <summary>
		/// Defines info about an available field type.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="onDefault"></param>
		public FieldType(string name, Type type, Action<ILGenerator, string> onDefault)
		{
			Name = name;
			Type = type;
			OnDefault = onDefault;
		}
		
		/// <summary>
		/// Parse the given string as the default value.
		/// </summary>
		public void EmitDefault(ILGenerator body, string value)
		{
			OnDefault(body, value);
		}
	}
	
}