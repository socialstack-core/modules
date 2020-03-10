using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Eventing
{
	/// <summary>
	/// 
	/// Event handlers are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public static partial class Events
    {
		/// <summary>
		/// Called at the earliest point when all the event handlers are setup and you can start using them.
		/// </summary>
		public static EventHandler<object> EventsAfterStart;

        /// <summary>
        /// A lookup of lowercase event name -> event handler.
        /// </summary>
        public static Dictionary<string, EventHandler> All;


		/// <summary>
		/// Call this to trigger the OnStart event.
		/// </summary>
		public static async Task TriggerStart()
		{
			await EventsAfterStart.Dispatch(null, null);
		}

		/// <summary>
		/// Called to setup all event handlers.
		/// </summary>
		public static void Init() {
			if (All != null)
			{
				return;
			}

			// Must setup this all set first:
			All = new Dictionary<string, EventHandler>();
			
			SetupEventsOnObject(null, typeof(Events), "");
		}
		
		/// <summary>
		/// Sets up any event handler objects on the given target object by looping through fields of the given type.
		/// Note that type is provided as the target object can be null in the case of the static Events class itself.
		/// If it encounters an EventGroup, it recurses and sets up the events on its fields too.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="type"></param>
		/// <param name="prependedNames">
		/// If going through EventGroup objects, this is any additional text to prepend to the field name.
		/// </param>
		public static void SetupEventsOnObject(object target, Type type, string prependedNames)
		{
			var events = type.GetFields();

			foreach (var field in events)
			{
				var currentValue = field.GetValue(target);

				if (currentValue != null)
				{
					// The dev specified a value they'd like to use instead.
					continue;
				}
				
				// Create an instance of the field type.
				object evt = null;

				// If it's an event group, instance the events on it, but prepended with the field name and any other names.
				if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(EventGroup<>))
				{
					// Instance the group:
					evt = Activator.CreateInstance(field.FieldType);

					// Setup all of the fields on it too:
					SetupEventsOnObject(evt, field.FieldType, prependedNames + field.Name);

					field.SetValue(target, evt);

					continue;
				}

				// EventHandler field type? (This excludes e.g. the All dictionary):
				if (!typeof(EventHandler).IsAssignableFrom(field.FieldType))
				{
					continue;
				}
				
				// We specifically use the fields own type here so the dev can specify a custom type there too.
				// It must however have a constructor with 1 string param.
				evt = Activator.CreateInstance(field.FieldType, prependedNames + field.Name);	
				
				// Set it to the field now:
				field.SetValue(target, evt);

			}
		}

		/// <summary>
		/// Finds all event handlers with an entity type which is the given type, implements it (if its an interface type) or inherits it.
		/// For example, find all event handlers that act on all :IWithTags types.
		/// </summary>
		/// <param name="type">Can be a direct type, an inherited type or an interface.</param>
		/// <param name="verb">Optionally also filter by verb.</param>
		/// <param name="placement">Optionally also filter by placement.</param>
		/// <returns></returns>
		public static List<EventHandler> FindByType(Type type, string verb = null, EventPlacement placement = EventPlacement.Any)
		{
			if (All == null)
			{
				Init();
			}

			var set = new List<EventHandler>();

			foreach (var kvp in All)
			{
				var handler = kvp.Value;

				if (verb != null && handler.Verb != verb)
				{
					continue;
				}

				if (placement != EventPlacement.Any && handler.Placement != placement)
				{
					continue;
				}

				if (!type.IsAssignableFrom(handler.PrimaryType))
				{
					continue;
				}

				set.Add(handler);
			}

			return set;
		}
		
		/// <summary>
		/// Finds all event handlers for the given verb ("Update", "Create", "Load", "List", "Delete" are the major ones).
		/// Use this if you want to run something e.g. whenever anything is created.
		/// </summary>
		/// <param name="verb"></param>
		/// <returns></returns>
		public static List<EventHandler> FindByVerb(string verb)
		{
			if (All == null)
			{
				Init();
			}

			var set = new List<EventHandler>();

			foreach (var kvp in All)
			{
				if (kvp.Value.Verb == verb)
				{
					set.Add(kvp.Value);
				}
			}

			return set;
		}

		/// <summary>
		/// Finds all event handlers for the given placement (i.e. all "before" handlers).
		/// </summary>
		/// <param name="placement"></param>
		/// <returns></returns>
		public static List<EventHandler> FindByPlacement(EventPlacement placement)
		{
			if (All == null)
			{
				Init();
			}

			var set = new List<EventHandler>();

			foreach (var kvp in All)
			{
				if (kvp.Value.Placement == placement)
				{
					set.Add(kvp.Value);
				}
			}

			return set;
		}

		/// <summary>
		/// Finds all event handlers for the given verb ("Update", "Create", "Load", "List", "Delete" are the major ones) and optionally the given placement.
		/// Use this if you want to run something e.g. whenever anything is created.
		/// </summary>
		/// <param name="verb"></param>
		/// <param name="placement"></param>
		/// <returns></returns>
		public static List<EventHandler> FindByPlacementAndVerb(EventPlacement placement = EventPlacement.Any, string verb = null)
		{
			if (All == null)
			{
				Init();
			}

			var set = new List<EventHandler>();

			foreach (var kvp in All)
			{
				if (verb == null || kvp.Value.Verb == verb)
				{
					if (placement != EventPlacement.Any && kvp.Value.Placement != placement)
					{
						continue;
					}

					set.Add(kvp.Value);
				}
			}

			return set;
		}

		/// <summary>
		/// Finds all event handlers for the given entity name (i.e. all "Forum" handlers).
		/// </summary>
		/// <param name="entityName"></param>
		/// <returns></returns>
		public static List<EventHandler> FindByEntity(string entityName)
		{
			if (All == null)
			{
				Init();
			}

			var set = new List<EventHandler>();

			foreach (var kvp in All)
			{
				if (kvp.Value.EntityName == entityName)
				{
					set.Add(kvp.Value);
				}
			}

			return set;
		}

	}
}
