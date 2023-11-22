using System;
using System.Collections;
using System.Collections.Generic;
using Api.AutoForms;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Newtonsoft.Json.Linq;

namespace Api.Eventing
{
	/// <summary>
	/// A grouping of common events, such as before/ after create, update, delete etc.
	/// These are typically added to the Events class, named directly after the type that is being used.
	/// Like this:
	/// public static EventGroup{Page} Page;
	/// You can extend it with custom events as well - just do so on the base EventGroup{T, ID} type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class EventGroup<T> : EventGroup<T, uint>
		where T : Content<uint>, new()
	{
	}

	/// <summary>
	/// A grouping of common events, such as before/ after create, update, delete etc.
	/// These are typically added to the Events class, named directly after the type that is being used.
	/// Like this:
	/// public static EventGroup{Page} Page;
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	public partial class EventGroup<T, ID> : EventGroupCore<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Just before a service loads an entity list.
		/// </summary>
		public EventHandler<QueryPair<T, ID>> BeforeList;

		/// <summary>
		/// Called to actually obtain the list of results from the data engine.
		/// </summary>
		public EventHandler<QueryPair<T, ID>> List;

		/// <summary>
		/// Just after an entity list was loaded.
		/// </summary>
		public EventHandler<Filter<T, ID>> AfterList;
		/// <summary>
		/// Called after an entity has been listed, just before it is written to the output
		/// </summary>
		public EventHandler<T> ListEntry;

		/// <summary>
		/// List entities.
		/// </summary>
		public EndpointEventHandler<Filter<T, ID>> EndpointStartList;
		/// <summary>
		/// List entities.
		/// </summary>
		public EndpointEventHandler<Filter<T, ID>> EndpointEndList;
		/// <summary>
		/// Called after an entity has been listed, just before it is written to the output
		/// </summary>
		public EndpointEventHandler<T> EndpointListEntry;

		/// <summary>
		/// Just before a field is added (and made settable).
		/// </summary>
		public EventHandler<JsonField<T, ID>> BeforeSettable;

		/// <summary>
		/// Just before a field is added (and made gettable).
		/// </summary>
		public EventHandler<JsonField<T, ID>> BeforeGettable;
	}

	/// <summary>
	/// Core event group which can be used by any general type and ID pairing.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	public partial class EventGroupCore<T, ID> : EventGroup
	{
		#region Service events

		/// <summary>
		/// Just before a new entity is created. The given entity won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public EventHandler<T> BeforeCreate;

		/// <summary>
		/// Called to actually create the result in the data engine.
		/// </summary>
		public EventHandler<T> Create;
		
		/// <summary>
		/// Called to actually create the result in the data engine during partial complete.
		/// </summary>
		public EventHandler<T> CreatePartial;

		/// <summary>
		/// Just after an entity has been created. The given object will now have an ID.
		/// </summary>
		public EventHandler<T> AfterCreate;

		/// <summary>
		/// Just before an entity is being deleted. Return null to cancel the deletion.
		/// </summary>
		public EventHandler<T> BeforeDelete;

		/// <summary>
		/// Called to actually delete the result from the data engine.
		/// </summary>
		public EventHandler<T> Delete;

		/// <summary>
		/// Just after an entity has been deleted.
		/// </summary>
		public EventHandler<T> AfterDelete;

		/// <summary>
		/// Just before updating an entity. Optionally make additional changes, or return null to cancel the update. You MAY apply changes to the first argument.
		/// The second argument is the original object which MUST be unchanged but may be used for comparisons.
		/// </summary>
		[Permissions(IsManual = true)]
		public EventHandler<T, T> BeforeUpdate;

		/// <summary>
		/// Called to actually update the result in the data engine. Given the updated object and the set of fields that were changed in it.
		/// </summary>
		public EventHandler<T, ChangedFields, DataOptions> Update;

		/// <summary>
		/// Just after updating an entity.
		/// </summary>
		public EventHandler<T> AfterUpdate;

		/// <summary>
		/// Just before an entity is loaded.
		/// </summary>
		public EventHandler<ID> BeforeLoad;

		/// <summary>
		/// Called to actually obtain the result from the data engine.
		/// </summary>
		public EventHandler<T, ID> Load;

		/// <summary>
		/// Just after an entity was loaded.
		/// </summary>
		public EventHandler<T> AfterLoad;

		#endregion

		/// <summary>
		/// Called just after the host service instance type has been changed. Use this to clear out any caches built on the instance type.
		/// </summary>
		public EventHandler<AutoService> AfterInstanceTypeUpdate;

		#region Controller events

		/// <summary>
		/// Load entity metadata.
		/// </summary>
		public EndpointEventHandler<ID> EndpointStartLoad;
		/// <summary>
		/// Load entity metadata.
		/// </summary>
		public EndpointEventHandler<T> EndpointEndLoad;
		/// <summary>
		/// Create a new entity.
		/// </summary>
		public EndpointEventHandler<T> EndpointStartCreate;
		/// <summary>
		/// Create a new entity.
		/// </summary>
		public EndpointEventHandler<T> EndpointEndCreate;
		/// <summary>
		/// Delete an entity.
		/// </summary>
		public EndpointEventHandler<T> EndpointStartDelete;
		/// <summary>
		/// Delete an entity.
		/// </summary>
		public EndpointEventHandler<T> EndpointEndDelete;
		/// <summary>
		/// Update entity metadata.
		/// </summary>
		public EndpointEventHandler<T> EndpointStartUpdate;
		/// <summary>
		/// Update entity metadata.
		/// </summary>
		public EndpointEventHandler<T> EndpointEndUpdate;

		#endregion

		/// <summary>
		/// Called when a remote entity was received via remote sync.
		/// The object will be of the correct content type and will be populated 
		/// by passing it through all the AfterLoad handlers.
		/// </summary>
		public EventHandler<T, int> Received;


		/// <summary>
		/// Gets the capability for loading something of this type.
		/// </summary>
		/// <returns></returns>
		public override Capability GetLoadCapability()
		{
			return AfterLoad.Capability;
		}

	}

	/// <summary>
	/// The base class of all EventGroup instances.
	/// </summary>
	public class EventGroup
	{
		/// <summary>
		/// All event handlers in this group that were assigned a capability. Can be null.
		/// </summary>
		public List<EventHandler> AllWithCapabilities;

		/// <summary>
		/// All event handlers in this group.
		/// </summary>
		public List<EventHandler> All;

		/// <summary>
		/// Creates a new instance of this event group. Automatically populates all EventHandler fields.
		/// </summary>
		public EventGroup() {
			All = new List<EventHandler>();

			// Setup all of the fields on it too:
			Events.SetupEventsOnObject(this, GetType(), null, All);
		}

		/// <summary>
		/// Gets the capability for loading something of this type.
		/// </summary>
		/// <returns></returns>
		public virtual Capability GetLoadCapability()
		{
			return null;
		}
		
	}
}