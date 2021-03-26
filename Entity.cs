using Api.AutoForms;
using System;


namespace Api.Database
{
	/// <summary>
	/// Used to represent an entity which can either be stored in the cache only or in the database.
	/// By default, unless you specify [CacheOnly] on your type, the entity will be stored in the database.
	/// A database table will always have the columns defined here as fields.
	/// Will often be Entity{int}
	/// </summary>
	/// <typeparam name="T">The type of ID of your entity. Usually int.</typeparam>
	public abstract partial class Entity<T> : IHaveId<T> where T : struct
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

		/// <summary>
		/// Sets the ID of this row.
		/// </summary>
		/// <returns></returns>
		public void SetId(T id)
		{
			Id = id;
		}
	}
	
}