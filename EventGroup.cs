using System;
using System.Collections;
using System.Collections.Generic;
using Api.Database;
using Api.Permissions;


namespace Api.Eventing
{
	/// <summary>
	/// A grouping of common events, such as before/ after create, update, delete etc.
	/// These are typically added to the Events class, named directly after the type that is being used.
	/// Like this:
	/// public static EventGroup{Page} Page;
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public partial class EventGroup<T> where T : DatabaseRow, new()
	{
		/// <summary>
		/// Just before a new entity is created. The given entity won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public EventHandler<T> BeforeCreate;

		/// <summary>
		/// Just after an entity has been created. The given object will now have an ID.
		/// </summary>
		public EventHandler<T> AfterCreate;

		/// <summary>
		/// Just before an entity is being deleted. Return null to cancel the deletion.
		/// </summary>
		public EventHandler<T> BeforeDelete;

		/// <summary>
		/// Just after an entity has been deleted.
		/// </summary>
		public EventHandler<T> AfterDelete;

		/// <summary>
		/// Just before updating an entity. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public EventHandler<T> BeforeUpdate;

		/// <summary>
		/// Just after updating an entity.
		/// </summary>
		public EventHandler<T> AfterUpdate;

		/// <summary>
		/// Just after an entity was loaded.
		/// </summary>
		public EventHandler<T> AfterLoad;

		/// <summary>
		/// Just before a service loads an entity list.
		/// </summary>
		public EventHandler<Filter<T>> BeforeList;

		/// <summary>
		/// Just after an entity list was loaded.
		/// </summary>
		public EventHandler<List<T>> AfterList;

	}
}