using Api.Permissions;
using Api.Startup;
using Api.Startup.Routing;
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
		/// All healthz events.
		/// </summary>
		public static HealthzGroup Healthz;
	}

	/// <summary>
	/// Page entity specific extensions to events.
	/// </summary>
	public class HealthzGroup : EventGroup
	{
		/// <summary>
		/// Runs during healthz checks.
		/// </summary>
		public EventHandler<HealthzChecks> RunChecks;
	}
}