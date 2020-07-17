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
		/// <summary>
		/// True if this is a unique index.
		/// </summary>
		public bool Unique = true;
		/// <summary>
		/// The fields in this index (case sensitive).
		/// </summary>
		public string[] Fields;

		internal DatabaseIndexAttribute() { }

		/// <summary>
		/// Typically used to declare an index in the database consisting of one or more fields.
		/// </summary>
		internal DatabaseIndexAttribute(params string[] fields)
		{
			Fields = fields;
		}
		
	}
}
