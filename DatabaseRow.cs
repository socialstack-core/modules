using Api.AutoForms;
using System;


namespace Api.Database
{
	/// <summary>
	/// Used to represent an entity to 
	/// store in the database automatically.
	/// A database table will always have the columns defined here as fields.
	/// </summary>
	public abstract partial class DatabaseRow : DatabaseRow<int>
	{
	}

	/// <summary>
	/// Used to represent an entity to 
	/// store in the database automatically.
	/// A database table will always have the columns defined here as fields.
	/// </summary>
	public abstract partial class DatabaseRow<T> : IHaveId<T> where T : struct
	{
		/// <summary>
		/// The row ID.
		/// </summary>
		[DatabaseIndex]
		[DatabaseField(AutoIncrement = true)]
		[Module(Hide = true)]
		public T Id;

		/// <summary>
		/// The name of the type. Can be used to obtain the content ID.
		/// </summary>
		[Module(Hide = true)]
		public string Type {
			get {
				return GetType().Name;
			}
		}

		/// <summary>
		/// Gets the ID of this row.
		/// </summary>
		/// <returns></returns>
		public T GetId()
		{
			return Id;
		}
	}
	
}