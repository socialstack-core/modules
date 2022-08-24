using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.Eventing
{
	/// <summary>
	/// 
	/// Event handlers are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Function as readonly fields but set by reflection.")]
	public static partial class Events
    {
		/// <summary>
		/// Called at the earliest point when all the event handlers are setup and you can start using them.
		/// </summary>
		public static EventHandler<object> EventsAfterStart;

		/// <summary>
		/// Call this to trigger the OnStart event.
		/// </summary>
		public static async Task TriggerStart()
		{
			await EventsAfterStart.Dispatch(new Contexts.Context(), null);
		}

		/// <summary>
		/// Called to setup all event handlers.
		/// </summary>
		public static void Init() {
			SetupEventsOnObject(null, typeof(Events), null);
		}

		/// <summary>
		/// Sets up any event handler objects on the given target object by looping through fields of the given type.
		/// Note that type is provided as the target object can be null in the case of the static Events class itself.
		/// If an EventGroup is instanced, note that it internally calls SetupEventsOnObject.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="type"></param>
		/// <param name="attribs"></param>
		/// <param name="collectInto">Optionally collects all handlers into the given list.</param>
		public static void SetupEventsOnObject(object target, Type type, IEnumerable<Attribute> attribs, List<EventHandler> collectInto = null)
		{
			var events = type.GetFields();

			foreach (var field in events)
			{
				var currentValue = field.GetValue(target);

				if (currentValue != null)
				{
					// The dev specified a value they'd like to use instead.
					if (collectInto != null && currentValue is EventHandler evtHandle)
					{
						collectInto.Add(evtHandle);
					}
					continue;
				}
				
				// Create an instance of the field type.
				object evt;

				var fieldAttribs = field.GetCustomAttributes();

				// EventHandler field type? (This excludes e.g. any internal event system fields):
				if (typeof(EventGroup).IsAssignableFrom(field.FieldType) || typeof(EventHandler).IsAssignableFrom(field.FieldType))
				{
					// We specifically use the fields own type here so the dev can specify a custom type there too.
					evt = Activator.CreateInstance(field.FieldType);

					if (fieldAttribs != null || attribs != null)
					{
						var handler = (evt as EventHandler);
						if (handler != null)
						{
							handler.AddAttributes(fieldAttribs);
							handler.AddAttributes(attribs);

							if (collectInto != null)
							{
								collectInto.Add(handler);
							}
						}
					}

					// Set it to the field now:
					field.SetValue(target, evt);
				}
			}
		}

	}
}
