using System;
using System.Collections;
using System.Collections.Generic;
using Api.AutoForms;
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
	public partial class EventGroup<T> where T : DatabaseRow, new()
	{
		#region Service events

		/// <summary>
		/// NOTE: Very frequently bypassed. This event will be used more regularly in future versions. Just before a new entity revision is created.
		/// The given entity won't have an ID yet. Return null to cancel the creation of the revision.
		/// </summary>
		public EventHandler<T> RevisionBeforeCreate;

		/// <summary>
		/// NOTE: Very frequently bypassed. This event will be used more regularly in future versions. Called just after an entity revision has been created.
		/// </summary>
		public EventHandler<T> RevisionAfterCreate;

		/// <summary>
		/// Just before an entity revision is being deleted. Return null to cancel the deletion.
		/// </summary>
		public EventHandler<T> RevisionBeforeDelete;

		/// <summary>
		/// Just after an entity revision has been deleted.
		/// </summary>
		public EventHandler<T> RevisionAfterDelete;

		/// <summary>
		/// Just before updating an entity revision. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public EventHandler<T> RevisionBeforeUpdate;

		/// <summary>
		/// Just after updating an entity revision.
		/// </summary>
		public EventHandler<T> RevisionAfterUpdate;

		/// <summary>
		/// Just after an entity revision was loaded.
		/// </summary>
		public EventHandler<T> RevisionAfterLoad;

		/// <summary>
		/// Just before a service loads an entity revision list.
		/// </summary>
		public EventHandler<Filter<T>> RevisionBeforeList;

		/// <summary>
		/// Just after an entity revision list was loaded.
		/// </summary>
		public EventHandler<List<T>> RevisionAfterList;

		#endregion
		
		#region Controller events
		
		/// <summary>
		/// Delete a revision.
		/// </summary>
		public EndpointEventHandler<T> RevisionDelete;
		/// <summary>
		/// Update revision metadata.
		/// </summary>
		public EndpointEventHandler<AutoForm<T>> RevisionUpdate;
		/// <summary>
		/// Load revision metadata.
		/// </summary>
		public  EndpointEventHandler<T> RevisionLoad;
		/// <summary>
		/// List revisions.
		/// </summary>
		public EndpointEventHandler<Filter<T>> RevisionList;

		#endregion

	}
}