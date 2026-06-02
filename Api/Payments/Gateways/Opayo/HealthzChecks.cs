using Newtonsoft.Json;

namespace Api.Startup;


/// <summary>
/// Extend this with custom healthz checks and run them during Events.Healthz.RunChecks.
/// </summary>
public partial class HealthzChecks
{
	/// <summary>
	/// Opayo status info
	/// </summary>
	public object opayo;
}