using System;

namespace Api.Database
{
	/// <summary>
	/// Use this to declare an index. If applied to a class, it can have one or more fields.
	/// [DatabaseField] will default to a unique index of the field it's on.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class DatabaseIndexAttribute : Attribute
	{
		/// <summary>
		///  Can be either ASC or DESC.
		/// </summary>
		public string Direction;
		public bool Unique = true;
		public string[] Fields;

		internal DatabaseIndexAttribute() { }

		/// <summary>
		/// Typically used to declare 
		internal DatabaseIndexAttribute(params string[] fields)
		{
			Fields = fields;
		}
		
	}
}
