using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Emails;


namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for generation
		/// </summary>
		public static GenerationEventGroup Generation;
	}

	/// <summary>
	/// Custom user specific events.
	/// </summary>
	public class GenerationEventGroup : EventGroup
	{

		/// <summary>
		/// An AutoController has been found.
		/// </summary>
		public EventHandler<Type> OnControllerFound;

		/// <summary>
		/// An entity has been found.
		/// </summary>
		public EventHandler<Type> OnEntityFound;

	}
}