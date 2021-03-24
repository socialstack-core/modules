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
	public partial class EventGroup<T, ID>
	{
		#region Service events

		/// <summary>
		/// NOTE: Very frequently bypassed. This event will be used more regularly in future versions. Just before a new entity revision is created.
		/// The given entity won't have an ID yet. Return null to cancel the creation of the revision.
		/// </summary>
		public EventHandler<T> BeforeRevisionCreate;

		/// <summary>
		/// NOTE: Very frequently bypassed. This event will be used more regularly in future versions. Called just after an entity revision has been created.
		/// </summary>
		public EventHandler<T> AfterRevisionCreate;

		/// <summary>
		/// Called just before an entity draft has been created.
		/// </summary>
		public EventHandler<T> BeforeDraftCreate;

		/// <summary>
		/// Called just after an entity draft has been created.
		/// </summary>
		public EventHandler<T> AfterDraftCreate;

		/// <summary>
		/// Just before an entity revision is being deleted. Return null to cancel the deletion.
		/// </summary>
		public EventHandler<T> BeforeRevisionDelete;

		/// <summary>
		/// Just after an entity revision has been deleted.
		/// </summary>
		public EventHandler<T> AfterRevisionDelete;

		/// <summary>
		/// Just before updating an entity revision. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public EventHandler<T> BeforeRevisionUpdate;

		/// <summary>
		/// Just after updating an entity revision.
		/// </summary>
		public EventHandler<T> AfterRevisionUpdate;

		/// <summary>
		/// Just before updating an entity revision. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public EventHandler<T> BeforeRevisionPublish;

		/// <summary>
		/// Just after updating an entity revision.
		/// </summary>
		public EventHandler<T> AfterRevisionPublish;

		/// <summary>
		/// Just before an entity revision was loaded.
		/// </summary>
		public EventHandler<ID> BeforeRevisionLoad;

		/// <summary>
		/// Just after an entity revision was loaded.
		/// </summary>
		public EventHandler<T> AfterRevisionLoad;

		/// <summary>
		/// Just before a service loads an entity revision list.
		/// </summary>
		public EventHandler<Filter<T>> BeforeRevisionList;

		/// <summary>
		/// Just after an entity revision list was loaded.
		/// </summary>
		public EventHandler<List<T>> AfterRevisionList;

		#endregion
		
		#region Controller events
		
		/// <summary>
		/// Draft is being created.
		/// </summary>
		public EndpointEventHandler<T> EndpointStartDraftCreate;

		/// <summary>
		/// After a draft is being created.
		/// </summary>
		public EndpointEventHandler<T> EndpointEndDraftCreate;

		/// <summary>
		/// A revision is being published.
		/// </summary>
		public EndpointEventHandler<T> EndpointStartRevisionPublish;

		/// <summary>
		/// After a revision was published.
		/// </summary>
		public EndpointEventHandler<T> EndpointEndRevisionPublish;
		/// <summary>
		/// Delete a revision.
		/// </summary>
		public EndpointEventHandler<ID> EndpointStartRevisionDelete;
		/// <summary>
		/// Delete a revision.
		/// </summary>
		public EndpointEventHandler<T> EndpointEndRevisionDelete;
		/// <summary>
		/// Update revision metadata.
		/// </summary>
		public EndpointEventHandler<T> EndpointStartRevisionUpdate;
		/// <summary>
		/// After a revision was updated.
		/// </summary>
		public EndpointEventHandler<T> EndpointEndRevisionUpdate;
		/// <summary>
		/// Load revision metadata.
		/// </summary>
		public  EndpointEventHandler<ID> EndpointStartRevisionLoad;
		/// <summary>
		/// Load revision metadata.
		/// </summary>
		public EndpointEventHandler<T> EndpointEndRevisionLoad;
		/// <summary>
		/// List revisions.
		/// </summary>
		public EndpointEventHandler<Filter<T>> EndpointStartRevisionList;

		#endregion

	}
}