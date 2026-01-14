using Api.Permissions;
using System;
using System.Collections.Generic;


namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		/// <summary>
		/// Called when capabilities are being granted to roles.
		/// </summary>
		public static EventHandler<object> CapabilityOnSetup;

		/// <summary>
		/// Events on the Role type.
		/// </summary>
		public static RoleEventGroup Role;

		/// <summary>
		/// Set of events for a contentFieldAccessRule.
		/// </summary>
		public static EventGroup<ContentFieldAccessRule> ContentFieldAccessRule;
		
	}

	/// <summary>
	/// Custom events for Role.
	/// </summary>
	public partial class RoleEventGroup : EventGroup<Role>
	{

		/// <summary>
		/// Event used to register role objects in to Roles.X fields. 
		/// It runs specifically after an event is either created or loaded in the cache but before its grants are loaded.
		/// Note that it is possible for a role to pass through here more than once but it would be the same object.
		/// </summary>
		public EventHandler<Role> Register;

		/// <summary>
		/// Called just before the role cache starts. This is the best place to install custom roles.
		/// </summary>
		public EventHandler<RoleService> CollectCustomRoles;
	}

}