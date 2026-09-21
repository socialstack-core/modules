using System.Text;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Set of events for a configuration.
		/// </summary>
		public static EventGroup<Api.Configuration.Configuration> Configuration;

		/// <summary>
		/// Called when frontend config JSON bytes are being built.
		/// Listeners write raw JSON object members (e.g. ,"currencies":{"GBP":1,"USD":1.35})
		/// into the StringBuilder, including the leading comma.
		/// </summary>
		public static EventHandler<StringBuilder> OnFrontendConfigBuild;
	}
}