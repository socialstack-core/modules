using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Eventing;
using Api.Contexts;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Database;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Api.Users;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Api.Permissions
{

	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class Init
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Init()
		{

			var setupForTypeMethod = GetType().GetMethod(nameof(SetupForType));
			
			Events.Service.AfterCreate.AddEventListener((Context context, AutoService service) => {

				if (service == null)
				{
					return new ValueTask<AutoService>(service);
				}

				// Get the content type for this service and event group:
				var servicedType = service.ServicedType;

				if (servicedType == null)
				{
					return new ValueTask<AutoService>(service);
				}
				
				// Add List event:
				var setupType = setupForTypeMethod.MakeGenericMethod(new Type[] {
					servicedType,
					service.IdType
				});

				setupType.Invoke(this, new object[] {
					service
				});

				return new ValueTask<AutoService>(service);
			}, 1);

			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) => {

				// Public - the role used by anonymous users.
				Roles.Public.GrantFeature("load").GrantFeature("list")
					.Grant("user_create");
				
				// Super admin - can do everything.
				// Note that you can grant everything and then revoke certain things if you want.
				Roles.Developer.GrantEverything();

				// Admin - can do almost everything. Usually everything super admin can do, minus system config options/ site level config.
				Roles.Admin.GrantTheSameAs(Roles.Developer); // <-- In this case, we grant the same as SA.

				// Guest - account created, not activated. Basically the same as a public account by default.
				Roles.Guest.GrantTheSameAs(Roles.Public); // <-- In this case, we grant the same as public.

				// Users can update or delete any content they've created themselves:
				Roles.Guest.If("IsSelf()").ThenGrantFeature("update", "delete");

				// Member - created and (optionally) activated.
				Roles.Member.GrantTheSameAs(Roles.Guest);

				return new ValueTask<object>(source);
			}, 9);
		}

		/// <summary>
		/// Sets up for the given type with its event group.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="service"></param>
		public void SetupForType<T, ID>(AutoService<T, ID> service)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// If it's a mapping type, no-op.
			if (ContentTypes.IsAssignableToGenericType(typeof(T), typeof(Mapping<,>)))
			{
				// Mapping types don't get mounted by the permission system.
				return;
			}

			var group = service.EventGroup;

			var fields = group.GetType().GetFields();

			group.AllWithCapabilities = null;

			foreach (var field in fields)
			{
				if (field.FieldType == typeof(Eventing.EventHandler<T>) && field.Name.StartsWith("Before"))
				{
					// Standard event handle.
					var eventHandler = field.GetValue(group) as Eventing.EventHandler<T>;

					// Create a capability for this event type - e.g. User.BeforeCreate becomes a capability called "User_Create".
					if (eventHandler.Capability == null)
					{
						var capability = new Capability(service, field.Name[6..]);
						eventHandler.Capability = capability;
					}

					if (group.AllWithCapabilities == null)
					{
						group.AllWithCapabilities = new List<Api.Eventing.EventHandler>();
					}

					group.AllWithCapabilities.Add(eventHandler);

					SetupForStandardEvent<T>(eventHandler, eventHandler.Capability, field);
				}
				else if (field.FieldType == typeof(Eventing.EventHandler<T, T>) && field.Name.StartsWith("Before"))
				{
					// Update event handle.
					var eventHandler = field.GetValue(group) as Eventing.EventHandler<T, T>;

					// Create a capability for this event type - e.g. User.BeforeUpdate becomes a capability called "User_Update".
					if (eventHandler.Capability == null)
					{
						var capability = new Capability(service, field.Name[6..]);
						eventHandler.Capability = capability;
					}

					if (group.AllWithCapabilities == null)
					{
						group.AllWithCapabilities = new List<Api.Eventing.EventHandler>();
					}

					group.AllWithCapabilities.Add(eventHandler);

					SetupForStandardDoubleEvent(eventHandler, eventHandler.Capability, field);
				}
				else if (field.FieldType == typeof(Eventing.EventHandler<T>) && field.Name.StartsWith("After") && field.Name.EndsWith("Load"))
				{
					// Special case for Load events because BeforeLoad is an EventHandler with just an ID and we can't use that to figure out if the load is ok or not.
					var eventHandler = field.GetValue(group) as Eventing.EventHandler<T>;

					// Create a capability for this event type - e.g. User.AfterLoad becomes a capability called "User_Load".

					if (eventHandler.Capability == null)
					{
						var capability = new Capability(service, field.Name[5..]);
						eventHandler.Capability = capability;
					}

					if (group.AllWithCapabilities == null)
					{
						group.AllWithCapabilities = new List<Api.Eventing.EventHandler>();
					}

					group.AllWithCapabilities.Add(eventHandler);

					SetupForStandardEvent(eventHandler, eventHandler.Capability, field);
				}
				else if (field.FieldType == typeof(Eventing.EventHandler<QueryPair<T, ID>>) && field.Name.StartsWith("Before"))
				{
					// List handle
					var eventHandler = field.GetValue(group) as Eventing.EventHandler<QueryPair<T, ID>>;

					// Create a capability for this event type - e.g. User.BeforeCreate becomes a capability called "User_Create".

					if (eventHandler.Capability == null)
					{
						var capability = new Capability(service, field.Name[6..]);
						eventHandler.Capability = capability;
					}

					if (group.AllWithCapabilities == null)
					{
						group.AllWithCapabilities = new List<Api.Eventing.EventHandler>();
					}

					group.AllWithCapabilities.Add(eventHandler);

					SetupForListEvent(eventHandler, eventHandler.Capability);
				}

			}

		}

		/// <summary>
		/// Sets up a particular Before*List event handler with permissions
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="handler"></param>
		/// <param name="capability"></param>
		public void SetupForListEvent<T, ID>(Api.Eventing.EventHandler<QueryPair<T, ID>> handler, Capability capability)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// Add an event handler at priority 1 (runs before others).
			handler.AddEventListener((Context context, QueryPair<T, ID> pair) =>
			{
				if (context.IgnorePermissions)
				{
					// Unchanged
					return new ValueTask<QueryPair<T, ID>>(pair);
				}
				
				// Check if the capability is granted.
				// If it is, return the first arg.
				// Otherwise, return null.
				var role = context.Role;

				if (role == null)
				{
					// No user role - can't grant this capability.
					// This is likely to indicate a deeper issue, so we'll warn about it:
					Log.Warn("user", "User ID " + context.UserId + " has no role (or the role with that ID hasn't been instanced).");
					throw PermissionException.Create(capability.Name, context);
				}

				// Get the grant rule (a filter) for this role + capability:
				var rawGrantRule = role.GetGrantRule(capability) as Filter<T, ID>;

				// If it's outright rejected..
				if (rawGrantRule == null)
				{
					throw PermissionException.Create(capability.Name, context);
				}
				
				pair.QueryB = rawGrantRule;

				// Both are set. Must combine them safely:
				return new ValueTask<QueryPair<T, ID>>(pair);
			}, 1);
		}

		/// <summary>
		/// Sets up a particular non-list event handler with permissions
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="handler"></param>
		/// <param name="capability"></param>
		/// <param name="field"></param>
		public void SetupForStandardEvent<T>(Api.Eventing.EventHandler<T> handler, Capability capability, FieldInfo field)
		{
			var permsAttrib = field.GetCustomAttribute<PermissionsAttribute>();

			if (permsAttrib != null && permsAttrib.IsManual) {
				// Be careful out there! You *MUST* test your capability when you dispatch your event. 
				// Use handler.TestCapability instead e.g. Events.Thing.BeforeUpdate.TestCapability(..)
				return;
			}

			// Add an event handler at priority 1 (runs before others).
			handler.AddEventListener(async (Context context, T content) =>
			{
				// Note: The following code is very similar to handler.TestCapability(context, content) which is used for manual mode.

				if (context.IgnorePermissions || content == null)
				{
					return content;
				}

				// Check if the capability is granted.
				// If it is, return the first arg.
				// Otherwise, return null.
				var role = context == null ? Roles.Public : context.Role;

				if (role == null)
				{
					// No user role - can't grant this capability.
					// This is likely to indicate a deeper issue, so we'll warn about it:
					throw PermissionException.Create(capability.Name, context, "No role");
				}

				if (await role.IsGranted(capability, context, content, false))
				{
					// It's granted - return the first arg:
					return content;
				}

				throw PermissionException.Create(capability.Name, context);
			}, 1);
		}
		
		/// <summary>
		/// Sets up a particular non-list event handler with permissions, for handlers of the 2 type variety. This (currently) means only Update handlers.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="handler"></param>
		/// <param name="capability"></param>
		/// <param name="field"></param>
		public void SetupForStandardDoubleEvent<T>(Api.Eventing.EventHandler<T, T> handler, Capability capability, FieldInfo field)
		{
			var permsAttrib = field.GetCustomAttribute<PermissionsAttribute>();

			if (permsAttrib != null && permsAttrib.IsManual) {
				// Be careful out there! You *MUST* test your capability when you dispatch your event. 
				// Use handler.TestCapability instead e.g. Events.Thing.BeforeUpdate.TestCapability(..)
				return;
			}

			// Add an event handler at priority 1 (runs before others).
			handler.AddEventListener(async (Context context, T content, T orig) =>
			{
				// Note: The following code is very similar to handler.TestCapability(context, content) which is used for manual mode.

				if (context.IgnorePermissions || content == null)
				{
					return content;
				}

				// Check if the capability is granted.
				// If it is, return the first arg.
				// Otherwise, return null.
				var role = context == null ? Roles.Public : context.Role;

				if (role == null)
				{
					// No user role - can't grant this capability.
					// This is likely to indicate a deeper issue, so we'll warn about it:
					throw PermissionException.Create(capability.Name, context, "No role");
				}

				if (await role.IsGranted(capability, context, content, false))
				{
					// It's granted - return the first arg:
					return content;
				}

				throw PermissionException.Create(capability.Name, context);
			}, 1);
		}

	}
}
