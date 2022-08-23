using Api.Configuration;

namespace Api.Startup;


/// <summary>
/// Config for the monitor service.
/// </summary>
public class ReportToConfig : Config
{
	
	/// <summary>
	/// The URL to post the payload to.
	/// </summary>
	public string Url {get; set;}
	
	/// <summary>
	/// Frequency in seconds of the reports.
	/// </summary>
	public uint Frequency {get; set;} = 5;
	
	/// <summary>
	/// A project key if there is one. Included in the post payload as "key".
	/// </summary>
	public string Key {get; set;}

	/// <summary>
	/// Server type if one is set.
	/// </summary>
	public uint ServerType { get; set; }
}