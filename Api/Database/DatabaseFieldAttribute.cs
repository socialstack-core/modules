using System;

namespace Api.Database
{
	/// <summary>
	/// Use this to declare a field's varchar/ varbinary character length.
	/// If you want your field to be ignored, make it private and use a public property to optionally expose it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class DatabaseFieldAttribute : Attribute
	{
		/// <summary>
		/// Length of the field value
		/// </summary>
		public int Length;
		/// <summary>
		/// Secondary length of the field value, used for decimal fields and similar.
		/// </summary>
		public int Length2;

		private bool _autoIncSet;
		private bool _autoInc;

		/// <summary>
		/// Previous field names, if any.
		/// </summary>
		public string[] PreviousNames;

		/// <summary>
		/// True if the attribute explicitly set the AutoIncrement field.
		/// </summary>
		public bool AutoIncWasSet
		{
			get {
				return _autoIncSet;
			}
		}

		/// <summary>
		/// Use this to declare a column as auto increment.
		/// If you set it to false on your class, it will block the Id field from being auto increment too.
		/// </summary>
		public bool AutoIncrement
		{
			get {
				return _autoInc;
			}
			set {
				_autoIncSet = true;
				_autoInc = value;
			}
		}
		/// <summary>
		/// Indicates that this field should just be ignored entirely.
		/// </summary>
		public bool Ignore = false;

		/// <summary>
		/// Class only: Table group name if the underlying storage supports or uses them.
		/// </summary>
		public string Group;

		internal DatabaseFieldAttribute() { }

		/// <summary>
		/// Indicate that this content type should be stored in the given storage group.
		/// </summary>
		/// <param name="group"></param>
		public DatabaseFieldAttribute(string group)
		{
			Group = group;
		}

		internal DatabaseFieldAttribute(int length)
		{
			Length = length;
		}

		/// <summary>
		/// Typically used for decimals. 
		/// </summary>
		/// <param name="afterDecimal"></param>
		/// <param name="totalLength"></param>
		internal DatabaseFieldAttribute(int afterDecimal, int totalLength)
		{
			// nums after the decimal pt:
			Length = afterDecimal;

			// Total length (optional and 10 is default):
			Length2 = totalLength;
		}

	}
}
