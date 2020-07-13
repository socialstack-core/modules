using Api.AutoForms;
using System;


namespace Api.Database
{
	
	/// <summary>
	/// Used to represent an entity to 
	/// store in the database automatically.
	/// A database table will always have the columns defined here as fields.
	/// </summary>
	public partial class DatabaseRow
	{
		/// <summary>
		/// The row ID.
		/// </summary>
		[DatabaseIndex]
		[DatabaseField(AutoIncrement = true)]
		[Module(Hide = true)]
		public int Id;

		/// <summary>
		/// The name of the type. Can be used to obtain the content ID.
		/// </summary>
		[Module(Hide = true)]
		public string Type {
			get {
				return GetType().Name;
			}
		}
	}
	
}