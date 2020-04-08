using Api.AutoForms;
using Api.Contexts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Eventing
{
	/// <summary>
	/// Used when an event handler is hooked up generically.
	/// </summary>
	/// <param name="context">
	/// The context which can be used to identify the original user making a particular request.
	/// Important for e.g. returning correctly localised database results automatically.</param>
	/// <param name="args"></param>
	/// <returns></returns>
	public delegate object GenericEventHandler(Context context, params object[] args);

	/// <summary>
	/// Used when an event handler is hooked up generically.
	/// </summary>
	/// <param name="context">
	/// The context which can be used to identify the original user making a particular request.
	/// Important for e.g. returning correctly localised database results automatically.</param>
	/// <param name="args"></param>
	/// <returns></returns>
	public delegate Task<object> GenericEventHandlerAsync(Context context, params object[] args);

	/// <summary>
	/// Event handlers are instanced automatically and form a major part of the pluggable architecture.
	/// Modules can define events via simply extending the Events class.
	/// Handlers are also heaviest on add - they're designed to maximise repeat run performance - so avoid rapidly adding and removing them.
	/// Instead add one handler at startup and then do a check inside it to see if it should run or not.
	/// </summary>
	public abstract partial class EventHandler
	{
		/// <summary>
		/// Event name. Always the same as the field name in the Events class. Of the form "ForumBeforeCreate".
		/// Word order is always EntityPlacementVerb such that events for the same entity group together in generated docs.
		/// The verb name is required, placement is optional (but must be "Before", "After", or "On").
		/// This strict structure helps event consistency and also allows them to be searchable in different ways - see also Events.Find.
		/// </summary>
		public string Name;

		/// <summary>
		/// E.g. "Update", "Load", "Create", "Delete", "List". The last word pulled from the name.
		/// </summary>
		public string Verb;

		/// <summary>
		/// Attributes on the various event handler fields. Can be null.
		/// </summary>
		public List<Attribute> Attributes;

		/// <summary>
		/// The primary type of this event handler. If set, it's the type of the first arg (and also its return type).
		/// Usually relates to, but isn't necessarily the same as, EntityName.
		/// If an Api.Results.Set/ Filter type is applied here, it will be resolved into the contained type.
		/// If you want the full original type - i.e you're after Api.Results.Set/ Filter types too - use SourcePrimaryType.
		/// </summary>
		public Type PrimaryType;

		/// <summary>
		/// The primary type of this event handler. If set, it's the type of the first arg (and also its return type).
		/// Usually relates to, but isn't necessarily the same as, EntityName.
		/// Unlike primary type, this is exactly as-is in the source.
		/// </summary>
		public Type SourcePrimaryType;

		/// <summary>
		/// The rest of the name with the verb and placement excluded.
		/// </summary>
		public string EntityName;

		/// <summary>
		/// "Before", "After", "On", null. The second last word from the name if it matches any of these 3, or null otherwise.
		/// </summary>
		public EventPlacement Placement = EventPlacement.NotSpecified;

		/// <summary>
		/// An index for high speed lookups.
		/// Not consistent across runs - don't store in the database. Use Name instead.
		/// </summary>
		public readonly int InternalId;

		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		/// <param name="name">Always the same as the field name in the Events class. Of the form "ForumBeforeCreate".</param>
		public EventHandler(string name)
		{
			Name = name;

			if (string.IsNullOrEmpty(name))
			{
				return;
			}

			if (Events.All == null)
			{
				// Note that this will cause this constructor to be invoked, as it collects all 'other' event handlers.
				// The Init method sets up the All dictionary before doing that however.
				Events.Init();
			}
			
			// Add to name lookup and set the internal ID:
			InternalId = Events.All.Count;
			Events.All[name.ToLower()] = this;

			// Split by capitals (note: This will split e.g. "SMS => S M S") but the few words we care about here won't contain this scenario.
			var wordsFromName = System.Text.RegularExpressions.Regex.Replace(
				name,
				"([A-Z])",
				" $1",
				System.Text.RegularExpressions.RegexOptions.Compiled
			).Trim().Split(' ');
			
			// Verb is always the last word:
			Verb = wordsFromName[wordsFromName.Length-1];

			var maxEntityNameLength = wordsFromName.Length - 1;

			// Placement is optional but when it's present it is second last:
			if (wordsFromName.Length > 1)
			{
				var secondLast = wordsFromName[wordsFromName.Length-2];

				switch(secondLast)
				{
					case "Before":
						maxEntityNameLength--;
						Placement = EventPlacement.Before;
					break;
					case "After":
						maxEntityNameLength--;
						Placement = EventPlacement.After;
					break;
					case "On":
						maxEntityNameLength--;
						Placement = EventPlacement.On;
					break;
				}
			}

			// Build the entity name with the remaining words:
			EntityName = "";

			for (var i = 0; i < maxEntityNameLength; i++)
			{
				EntityName += wordsFromName[i];
			}
			
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
				var gDef = type.GetGenericTypeDefinition();
				Type[] typeArguments = type.GetGenericArguments();
				if(typeArguments != null && typeArguments.Length > 0)
				{
					PrimaryType = typeArguments[0];
				}
			}
			
		}
		
		/// <summary>
		/// Triggers this event handler to run.
		/// Fires events in ascending order of the priority number.
		/// Events with the same priority number will occur in the order they were added.
		/// </summary>
		public virtual Task<object> Dispatch(Context context, params object[] args)
		{
			return Task.FromResult(args != null && args.Length > 0 ? args[0] : null);
		}

		/// <summary>
		/// Adds a generic method handler.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public virtual void AddEventListener(GenericEventHandler evt, int priority = 10)
		{
			throw new NotImplementedException("Attempted to add an event listener to a base EventHandler. Use one of the generic EventHandler<> types instead.");
		}

		/// <summary>
		/// Adds an async generic method handler.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public virtual void AddEventListener(GenericEventHandlerAsync evt, int priority = 10)
		{
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
		/// <param name="name">Always the same as the field name in the Events class. Of the form "ForumBeforeCreate".</param>
		public EventHandlerMethodSet(string name) : base(name) {}

		/// <summary>
		/// The raw ordered set of methods.
		/// </summary>
		protected EventMethodSet<T> MethodSet = new EventMethodSet<T>();
		
		/// <summary>
		/// Adds a new event listener with the given priority.
		/// Lower priorities mean it executes sooner. The default is 10.
		/// To add a non-async handler, use Task.FromResult.
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
	public partial class EventHandler<T1> : EventHandlerMethodSet<Func<Context, T1, Task<T1>>>
	{
		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		/// <param name="name">Always the same as the field name in the Events class. Of the form "ForumBeforeCreate".</param>
		public EventHandler(string name) : base(name) {
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
		/// <param name="args"></param>
		public async override Task<object> Dispatch(Context context, params object[] args)
		{
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			var v1 = (T1)args[0];
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1);
			}
			return v1;
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
		public async Task<T1> Dispatch(Context context, T1 v1)
		{
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1);
			}
			return v1;
		}

		/// <summary>
		/// Adds a generic method handler.
		/// To add a non-async handler, use Task.FromResult.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public override void AddEventListener(GenericEventHandler evt, int priority = 10)
		{
			if (evt == null)
			{
				throw new NullReferenceException("Listener is required.");
			}

			MethodSet.Add((Context context, T1 v1) =>
			{
				var result = evt(context, v1);
				return Task.FromResult((T1)result);
			}, priority);
		}

		/// <summary>
		/// Adds a generic method handler.
		/// To add a non-async handler, use Task.FromResult.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public override void AddEventListener(GenericEventHandlerAsync evt, int priority = 10)
		{
			if (evt == null)
			{
				throw new NullReferenceException("Listener is required.");
			}

			MethodSet.Add(async (Context context, T1 v1) =>
			{
				var result = (T1)await evt(context, v1);
				return result;
			}, priority);
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
	public partial class EventHandler<T1, T2> : EventHandlerMethodSet<Func<Context, T1, T2, Task<T1>>>
	{
		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		/// <param name="name">Always the same as the field name in the Events class. Of the form "ForumBeforeCreate".</param>
		public EventHandler(string name) : base(name)
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
		/// <param name="args"></param>
		public async override Task<object> Dispatch(Context context, params object[] args)
		{
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			var v1 = (T1)args[0];
			var v2 = (T2)args[1];
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1, v2);
			}
			return v1;
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
		public async Task<T1> Dispatch(Context context, T1 v1, T2 v2)
		{
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1, v2);
			}
			return v1;
		}
		
		/// <summary>
		/// Adds a generic method handler.
		/// To add a non-async handler, use Task.FromResult.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public override void AddEventListener(GenericEventHandler evt, int priority = 10)
		{
			if (evt == null)
			{
				throw new NullReferenceException("Listener is required.");
			}

			MethodSet.Add((Context context, T1 v1, T2 v2) =>
			{
				var result = evt(context, v1, v2);
				return Task.FromResult((T1)result);
			}, priority);
		}

		/// <summary>
		/// Adds a generic method handler.
		/// To add a non-async handler, use Task.FromResult.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public override void AddEventListener(GenericEventHandlerAsync evt, int priority = 10)
		{
			if (evt == null)
			{
				throw new NullReferenceException("Listener is required.");
			}

			MethodSet.Add(async (Context context, T1 v1, T2 v2) =>
			{
				var result = (T1)await evt(context, v1, v2);
				return result;
			}, priority);
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
	public partial class EventHandler<T1, T2, T3> : EventHandlerMethodSet<Func<Context, T1, T2, T3, Task<T1>>>
	{
		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		/// <param name="name">Always the same as the field name in the Events class. Of the form "ForumBeforeCreate".</param>
		public EventHandler(string name) : base(name)
		{
			SetPrimaryType(typeof(T1));
		}
		
		/// <summary>
		/// Triggers this event handler to run.
		/// Fires events in ascending order of the priority number.
		/// Events with the same priority number will occur in the order they were added.
		/// </summary>
		public async override Task<object> Dispatch(Context context, params object[] args)
		{
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			var v1 = (T1)args[0];
			var v2 = (T2)args[1];
			var v3 = (T3)args[2];
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1, v2, v3);
			}
			return v1;
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
		public async Task<T1> Dispatch(Context context, T1 v1, T2 v2, T3 v3)
		{
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1, v2, v3);
			}
			return v1;
		}

		/// <summary>
		/// Adds a generic method handler.
		/// To add a non-async handler, use Task.FromResult.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public override void AddEventListener(GenericEventHandler evt, int priority = 10)
		{
			if (evt == null)
			{
				throw new NullReferenceException("Listener is required.");
			}

			MethodSet.Add((Context context, T1 v1, T2 v2, T3 v3) =>
			{
				var result = evt(context, v1, v2, v3);
				return Task.FromResult((T1)result);
			}, priority);
		}

		/// <summary>
		/// Adds a generic method handler.
		/// To add a non-async handler, use Task.FromResult.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public override void AddEventListener(GenericEventHandlerAsync evt, int priority = 10)
		{
			if (evt == null)
			{
				throw new NullReferenceException("Listener is required.");
			}

			MethodSet.Add(async (Context context, T1 v1, T2 v2, T3 v3) =>
			{
				var result = (T1)await evt(context, v1, v2, v3);
				return result;
			}, priority);
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
	public partial class EventHandler<T1, T2, T3, T4> : EventHandlerMethodSet<Func<Context, T1, T2, T3, T4, Task<T1>>>
	{
		/// <summary>
		/// A place where event methods can be attached to handle events of a particular type.
		/// You often don't need to construct these - they'll be created automatically during startup.
		/// Just declare the field in the Events class.
		/// </summary>
		/// <param name="name">Always the same as the field name in the Events class. Of the form "ForumBeforeCreate".</param>
		public EventHandler(string name) : base(name)
		{
			SetPrimaryType(typeof(T1));
		}
		
		/// <summary>
		/// Triggers this event handler to run.
		/// Fires events in ascending order of the priority number.
		/// Events with the same priority number will occur in the order they were added.
		/// </summary>
		public async override Task<object> Dispatch(Context context, params object[] args)
		{
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			var v1 = (T1)args[0];
			var v2 = (T2)args[1];
			var v3 = (T3)args[2];
			var v4 = (T4)args[3];
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1, v2, v3, v4);
			}
			return v1;
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
		public async Task<T1> Dispatch(Context context, T1 v1, T2 v2, T3 v3, T4 v4)
		{
			var methods = MethodSet.Methods;
			var count = MethodSet.HandlerCount;
			for (var i = 0; i < count; i++)
			{
				v1 = await methods[i](context, v1, v2, v3, v4);
			}
			return v1;
		}

		/// <summary>
		/// Adds a generic method handler.
		/// To add a non-async handler, use Task.FromResult.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public override void AddEventListener(GenericEventHandler evt, int priority = 10)
		{
			if (evt == null)
			{
				throw new NullReferenceException("Listener is required.");
			}

			MethodSet.Add((Context context, T1 v1, T2 v2, T3 v3, T4 v4) =>
			{
				var result = evt(context, v1, v2, v3, v4);
				return Task.FromResult((T1)result);
			}, priority);
		}

		/// <summary>
		/// Adds a generic method handler.
		/// To add a non-async handler, use Task.FromResult.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="priority"></param>
		public override void AddEventListener(GenericEventHandlerAsync evt, int priority = 10)
		{
			if (evt == null)
			{
				throw new NullReferenceException("Listener is required.");
			}

			MethodSet.Add(async (Context context, T1 v1, T2 v2, T3 v3, T4 v4) =>
			{
				var result = (T1)await evt(context, v1, v2, v3, v4);
				return result;
			}, priority);
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
		/// <param name="name">Always the same as the field name in the Events class. Of the form "ForumBeforeCreate".</param>
		public EndpointEventHandler(string name) : base(name) { }

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
		/// <param name="name">Always the same as the field name in the Events class. Of the form "ForumBeforeCreate".</param>
		public EndpointEventHandler(string name) : base(name) { }

	}
}
