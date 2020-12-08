using Api.ChatBotSimple;
using Api.Permissions;
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
		/// Set of events for a chatBotDecision.
		/// </summary>
		public static EventGroup<ChatBotDecision> ChatBotDecision;
	}
}