using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Api.Startup;


/// <summary>
/// A service which records resource health and sends status updates if they are required.
/// The status updates are sent to a service defined in config either declared as "ReportTo" in the database or appsettings.
/// </summary>
public class MonitorService : AutoService
{
	private bool _disabled;
	private bool _startedAutomation;
	private ReportToConfig _config;
	private HostDetails _latestStats;
	private PlatformStatistics _environmentSpecificStatProvider;
	/// <summary>
	/// URL to post to. The response may indicate that a different url and key should be used.
	/// </summary>
	private string _url;
	private Context _monitorContext;

	/// <summary>
	/// Instanced automatically.
	/// </summary>
	public MonitorService()
	{

		// Get the config:
		_config = GetConfig<ReportToConfig>();

		Setup();

		_config.OnChange += () => {
			Setup();
			return new System.Threading.Tasks.ValueTask();
		};
	}

	/// <summary>
	/// Initially sets up the host details object.
	/// </summary>
	/// <param name="into"></param>
	/// <returns></returns>
	private async ValueTask InitialPopulation(HostDetails into)
	{
		// Get the processor count:
		into.CoreCount = (uint)Environment.ProcessorCount;

		into.HostName = System.Environment.MachineName;
		into.Environment = Services.Environment;
		into.ServerType = _config.ServerType;
		into.Key = _config.Key;
		// Because we just set the key, the url should also be set just in case this initial pop is being called > once.
		_url = _config.Url;

		// In large clusters, using IMDSv2 is suggested.
		// In all other situations, the below fallbacks are provided as well.
		// An event is triggered allowing IMDS or similar to be hooked up, probably via a module like Api/CloudHosts.
		into = await Events.Monitor.Setup.Dispatch(_monitorContext, into);

		if (into.IPv4 == null)
		{
			// Can use some other IP data source in the before populate event.

			// Get the ip addresses:
			var ips = await IpDiscovery.Discover();

			into.PrivateIPv4 = ips.PrivateIPv4?.ToString();
			into.PrivateIPv6 = ips.PrivateIPv6?.ToString();
			into.IPv6 = ips.PublicIPv6?.ToString();
			into.IPv4 = ips.PublicIPv4?.ToString();
		}

		if (into.HostGroupId == 0)
		{
			// Derive a simple default group ID.
			into.HostGroupId = (uint)GetId(into.Environment);
		}
	}
	
	/// <summary>
	/// Gets a group ID from the given value. Just a simple hash function.
	/// </summary>
	/// <param name="typeName"></param>
	/// <returns></returns>
	private int GetId(string typeName)
	{
		// Note: Caching this would be nice but isn't worthwhile
		// because it _is_ the deterministic .NET hash function. If it was cached in a dictionary 
		// you'd end up running this code anyway during the lookup!

		typeName = typeName.ToLower();

		unchecked
		{
			int hash1 = (5381 << 16) + 5381;
			int hash2 = hash1;

			for (int i = 0; i < typeName.Length; i += 2)
			{
				hash1 = ((hash1 << 5) + hash1) ^ typeName[i];
				if (i == typeName.Length - 1)
					break;
				hash2 = ((hash2 << 5) + hash2) ^ typeName[i + 1];
			}

			return hash1 + (hash2 * 1566083941);
		}
	}
	
	/// <summary>
	/// Collects the host details ready for sending to a remote tracking service or similar
	/// </summary>
	/// <returns></returns>
	public async ValueTask<HostDetails> GetHostDetails()
	{
		if (_latestStats == null)
		{
			_latestStats = new HostDetails();

			await InitialPopulation(_latestStats);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				_environmentSpecificStatProvider = new LinuxStatistics();
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_environmentSpecificStatProvider = new WindowsStatistics();
			}
			else
			{
				Console.WriteLine("[Info] Monitor is active but is unable to provide detailed resource stats from this platform.");
			}
		}

		if (_environmentSpecificStatProvider != null)
		{
			// This platform can provide more detailed stats
			await _environmentSpecificStatProvider.Collect(_latestStats);
		}

		return _latestStats;
	}

	private HttpClient _client;

	/// <summary>
	/// Used to post the given details to the monitor service.
	/// </summary>
	/// <param name="details"></param>
	/// <returns></returns>
	private async Task PostDetails(HostDetails details)
	{
		if (_client == null)
		{
			_client = new HttpClient();
		}

		var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(details), System.Text.Encoding.UTF8, "application/json");
		var result = await _client.PostAsync(_url, content);
		var responseJson = await result.Content.ReadAsStringAsync();

		if (!result.IsSuccessStatusCode)
		{
			Console.WriteLine("[Warn] monitor remote service indicated a failure. This is what '" + _url + "' replied with, and a " + result.StatusCode + " status code: " + responseJson);
			return;
		}

		if (!string.IsNullOrEmpty(responseJson))
		{
			// We got a response. This can currently contain two things: either a list of servers
			// (because we indicated we're a loadbalancer) or an alternate URL and key to use.
			var response = Newtonsoft.Json.JsonConvert.DeserializeObject(responseJson) as JObject;

			// Run an event to pass of the complete response:
			await Events.Monitor.AfterReply.Dispatch(_monitorContext, response);

			// Check for url/ key changes.
			var urlJson = response["url"];

			if (urlJson != null)
			{
				_url = urlJson.ToString();
			}

			var keyJson = response["key"];

			if (keyJson != null)
			{
				// Key change:
				details.Key = keyJson.ToString();
			}
		}
	}

	/// <summary>
	/// Runs when the monitor system is ticked.
	/// </summary>
	private async ValueTask OnMonitor()
	{
		try
		{
			// Get the details:
			var details = await GetHostDetails();

			// Post the details:
			await PostDetails(details);

		}
		catch (Exception e)
		{
			Console.WriteLine("[WARN] stat monitor threw an error: " + e.ToString());
		}

	}

	private void Setup()
	{
		_disabled = string.IsNullOrEmpty(_config.Url);

		if (_disabled || _startedAutomation)
		{
			// Don't add the event listener if it is already added, or isn't needed
			return;
		}

		// Set initial URL:
		_url = _config.Url;
		_startedAutomation = true;
		_monitorContext = new Context();

		Events.Automation("monitor", "*/" + _config.Frequency + " * * ? * * *").AddEventListener(async (Context context, Api.Automations.AutomationRunInfo runInfo) => {

			if (_disabled)
			{
				return runInfo;
			}

			await OnMonitor();

			return runInfo;
		});

	}
	
}
