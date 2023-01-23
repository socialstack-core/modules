using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Eventing
{
	/// <summary>
	/// Event handlers are instanced automatically and form a major part of the pluggable architecture.
	/// Modules can define events via simply extending the Events class.
	/// Handlers are also heaviest on add - they're designed to maximise repeat run performance - so avoid rapidly adding and removing them.
	/// Instead add one handler at startup and then do a check inside it to see if it should run or not.
	/// </summary>
	public abstract partial class EventHandler
	{
		/// <summary>
		/// The field name that this handler is from, if it was known.
		/// </summary>
		public string Name;

		/// <summary>
		/// The capability that was created for this handler, if there is one. Only available on Before* handlers.
		/// </summary>
		public Capability Capability;

		/// <summary>
		/// Attributes on the various event handler fields. Can be null.
		/// </summary>
		public List<Attribute> Attributes;

		/// <summary>
		/// The primary type of this event handler. If set, it's the type of the first arg (and also its return type).
		/// If an Api.Results.Set/ Filter type is applied here, it will be resolved into the contained type.
		/// If you want the full original type - i.e you're after Api.Results.Set/ Filter types too - use SourcePrimaryType.
		/// </summary>
		public Type PrimaryType;

		/// <summary>
		/// The primary type of this event handler. If set, it's the type of the first arg (and also its return type).
		/// Unlike primary type, this is exactly as-is in the source.
		/// </summary>
		public Type SourcePrimaryType;

		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		public EventHandler()
		{
		}

		/// <summary>
		/// Tests the capability of this handler for the given content/ context. Used when you're running manual permissions via [Permissions(IsManual=true)].
		/// </summary>
		/// <param name="context"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public async ValueTask<T1> TestCapability<T1>(Context context, T1 content)
		{
			if (context.IgnorePermissions)
			{
				return content;
			}

			if (Capability == null)
			{
				// This handler doesn't have a capability. They're typically on Before* event handlers, but can also be in After handlers for e.g. AfterLoad.
				throw PermissionException.Create(
					"none",
					context,
					"Failed to manually test permissions on a handler because it doesn't have a capability. " +
					"This usually means the wrong event handler was used."
				);
			}

			// Check if the capability is granted.
			// If it is, return the first arg.
			// Otherwise, return null.
			var role = context == null ? Roles.Public : context.Role;

			if (role == null)
			{
				// No user role - can't grant this capability.
				// This is likely to indicate a deeper issue, so we'll warn about it:
				throw PermissionException.Create(Capability.Name, context, "No role");
			}

			if (await role.IsGranted(Capability, context, content, false))
			{
				// It's granted - return the first arg:
				return content;
			}

			throw PermissionException.Create(Capability.Name, context);
		}
		
		/// <summary>
		/// Gets an attribute of the given type if its on this event handler's field. Null if not set.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public Attribute GetCustomAttribute<T>()
		{
			if (Attributes == null)
			{
				return null;
			}

			foreach (var attrib in Attributes)
			{
				if (attrib is T)
				{
					return attrib;
				}
			}

			return null;
		}

		/// <summary>
		/// Adds a custom attribute declared on the field.
		/// </summary>
		/// <param name="set"></param>
		public void AddAttributes(IEnumerable<Attribute> set)
		{
			if (set == null)
			{
				return;
			}

			foreach (var attrib in set)
			{
				if (Attributes == null)
				{
					Attributes = new List<Attribute>();
				}

				Attributes.Add(attrib);
			}
		}

		/// <summary>
		/// Sets the PrimaryType - essentially the type of the first arg.
		/// Note: If it is a generic Api.Results.Set or Filter type then this will obtain the type of the raw value.
		/// </summary>
		/// <param name="type"></param>
		protected void SetPrimaryType(Type type)
		{
			SourcePrimaryType = type;
			PrimaryType = type;
			
			// Resolve Set, List and Filter types (primarily) next. This helps auto grab the most important type from e.g. List events.
			if (type.IsConstructedGenericType)
			{
				Type[] typeArguments = type.GetGenericArguments();
				if(typeArguments != null && typeArguments.Length > 0)
				{
					PrimaryType = typeArguments[0];
				}
			}
			
		}
		
	}

	/// <summary>
	/// Used to define the func type used in an event handler.
	/// </summary>
	/// <typeparam name="T">
	/// Specify the func type for the method set.
	/// </typeparam>
	public class EventHandlerMethodSet<T> : EventHandler where T:class
	{
		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		public EventHandlerMethodSet() : base() {}

		/// <summary>
		/// The raw ordered set of methods.
		/// </summary>
		protected EventMethodSet<T> MethodSet = new EventMethodSet<T>();
		
		/// <summary>
		/// Adds a new event listener with the given priority.
		/// Lower priorities mean it executes sooner. The default is 10.
		/// </summary>
		/// <param name="evt">The event handler to run.</param>
		/// <param name="priority">Lower priorities mean it executes sooner. The default is 10.</param>
		public void AddEventListener(T evt, int priority = 10)
		{
			if (evt == null)
			{
				throw new NullReferenceException("Listener is required.");
			}

			MethodSet.Add(evt, priority);
		}
		
	}

	/// <summary>
	/// Event handlers are instanced automatically and form a major part of the pluggable architecture.
	/// Modules can define events via simply extending the Events class.
	/// </summary>
	/// <typeparam name="T1">
	/// Type of 1st arg.
	/// </typeparam>
	public partial class EventHandler<T1> : EventHandlerMethodSet<Func<Context, T1, ValueTask<T1>>>
	{
		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		public EventHandler() : base() {
			SetPrimaryType(typeof(T1));
		}

		/// <summary>
		/// Triggers this event handler to run.
		/// Fires events in ascending order of the priority number.
		/// Events with the same priority number will occur in the order they were added.
		/// </summary>
		/// <param name="context">
		/// The context which can be used to identify the original user making a particular request.
		/// Important for e.g. returning correctly localised database results automatically.</param>
		/// <param name="v1">1st arg value to pass to the methods. This one is also the default return value.</param>
		/// <returns></returns>
		public async ValueTask<T1> Dispatch(Context context, T1 v1)
		{
			if(context == null){
				throw new ArgumentNullException(nameof(context));
			}
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1);
			}
			return v1;
		}

	}

	/// <summary>
	/// Event handlers are instanced automatically and form a major part of the pluggable architecture.
	/// Modules can define events via simply extending the Events class.
	/// </summary>
	/// <typeparam name="T1">
	/// Type of 1st arg.
	/// </typeparam>
	/// <typeparam name="T2">
	/// Type of 2nd arg.
	/// </typeparam>
	public partial class EventHandler<T1, T2> : EventHandlerMethodSet<Func<Context, T1, T2, ValueTask<T1>>>
	{
		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		public EventHandler() : base()
		{
			SetPrimaryType(typeof(T1));
		}

		/// <summary>
		/// Triggers this event handler to run.
		/// Fires events in ascending order of the priority number.
		/// Events with the same priority number will occur in the order they were added.
		/// </summary>
		/// <param name="context">
		/// The context which can be used to identify the original user making a particular request.
		/// Important for e.g. returning correctly localised database results automatically.</param>
		/// <param name="v1">1st arg value to pass to the methods. This one is also the default return value.</param>
		/// <param name="v2">2nd arg value to pass to the methods.</param>
		/// <returns></returns>
		public async ValueTask<T1> Dispatch(Context context, T1 v1, T2 v2)
		{
			if(context == null){
				throw new ArgumentNullException(nameof(context));
			}
			
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1, v2);
			}
			return v1;
		}
		
	}

	/// <summary>
	/// Event handlers are instanced automatically and form a major part of the pluggable architecture.
	/// Modules can define events via simply extending the Events class.
	/// </summary>
	/// <typeparam name="T1">
	/// Type of 1st arg.
	/// </typeparam>
	/// <typeparam name="T2">
	/// Type of 2nd arg.
	/// </typeparam>
	/// <typeparam name="T3">
	/// Type of 3rd arg.
	/// </typeparam>
	public partial class EventHandler<T1, T2, T3> : EventHandlerMethodSet<Func<Context, T1, T2, T3, ValueTask<T1>>>
	{
		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		public EventHandler() : base()
		{
			SetPrimaryType(typeof(T1));
		}
		
		/// <summary>
		/// Triggers this event handler to run.
		/// Fires events in ascending order of the priority number.
		/// Events with the same priority number will occur in the order they were added.
		/// </summary>
		/// <param name="context">
		/// The context which can be used to identify the original user making a particular request.
		/// Important for e.g. returning correctly localised database results automatically.</param>
		/// <param name="v1">1st arg value to pass to the methods. This one is also the default return value.</param>
		/// <param name="v2">2nd arg value to pass to the methods.</param>
		/// <param name="v3">3rd arg value to pass to the methods.</param>
		/// <returns></returns>
		public async ValueTask<T1> Dispatch(Context context, T1 v1, T2 v2, T3 v3)
		{
			if(context == null){
				throw new ArgumentNullException(nameof(context));
			}
			
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1, v2, v3);
			}
			return v1;
		}

	}

	/// <summary>
	/// Event handlers are instanced automatically and form a major part of the pluggable architecture.
	/// Modules can define events via simply extending the Events class.
	/// </summary>
	/// <typeparam name="T1">
	/// Type of 1st arg.
	/// </typeparam>
	/// <typeparam name="T2">
	/// Type of 2nd arg.
	/// </typeparam>
	/// <typeparam name="T3">
	/// Type of 3rd arg.
	/// </typeparam>
	/// <typeparam name="T4">
	/// Type of 4th arg.
	/// </typeparam>
	public partial class EventHandler<T1, T2, T3, T4> : EventHandlerMethodSet<Func<Context, T1, T2, T3, T4, ValueTask<T1>>>
	{
		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		public EventHandler() : base()
		{
			SetPrimaryType(typeof(T1));
		}
		
		/// <summary>
		/// Triggers this event handler to run.
		/// Fires events in ascending order of the priority number.
		/// Events with the same priority number will occur in the order they were added.
		/// </summary>
		/// <param name="context">
		/// The context which can be used to identify the original user making a particular request.
		/// Important for e.g. returning correctly localised database results automatically.</param>
		/// <param name="v1">1st arg value to pass to the methods. This one is also the default return value.</param>
		/// <param name="v2">2nd arg value to pass to the methods.</param>
		/// <param name="v3">3rd arg value to pass to the methods.</param>
		/// <param name="v4">4th arg value to pass to the methods.</param>
		/// <returns></returns>
		public async ValueTask<T1> Dispatch(Context context, T1 v1, T2 v2, T3 v3, T4 v4)
		{
			if(context == null){
				throw new ArgumentNullException(nameof(context), "Required");
			}
			
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1, v2, v3, v4);
			}
			return v1;
		}

	}

	/// <summary>
	/// An event handler specifically for API endpoints.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	public class EndpointEventHandler<T1> : EventHandler<T1, HttpResponse> {

		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		public EndpointEventHandler() : base() { }

	}

	/// <summary>
	/// An event handler specifically for API endpoints.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	public class EndpointEventHandler<T1, T2> : EventHandler<T1, HttpResponse, T2>
	{

		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		public EndpointEventHandler() : base() { }

	}
}
