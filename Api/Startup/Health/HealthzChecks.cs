using Newtonsoft.Json;

namespace Api.Startup;


/// <summary>
/// Extend this with custom healthz checks and run them during Events.Healthz.RunChecks.
/// </summary>
public partial class HealthzChecks
{
	/// <summary>
	/// True if all checks are ok.
	/// </summary>
	[JsonIgnore]
	public bool Ok;

	/// <summary>
	/// True if the router is ready.
	/// </summary>
	public bool RouterReady;
}