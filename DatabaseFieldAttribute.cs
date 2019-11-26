using System;

namespace Api.Database
{
	/// <summary>
	/// Use this to declare a field's varchar/ varbinary character length.
	/// If you want your field to be ignored, make it private and use a public property to optionally expose it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class DatabaseFieldAttribute : Attribute
	{
		public int Length;
		/// <summary>
		/// Use this to declare a column as auto increment.
		/// If you set it to false on your class, it will block the Id field from being auto increment too.
		/// </summary>
		public bool AutoIncrement = false;


		internal DatabaseFieldAttribute() { }

		internal DatabaseFieldAttribute(int length)
		{
			Length = length;
		}
		
	}
}
