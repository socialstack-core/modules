using Api.Configuration;
using Api.Startup;

namespace Api.Database;


/// <summary>
/// General purpose handling of DB connection strings.
/// </summary>
public class ConnectionString
{

	/// <summary>
	/// Gets the connection string info for a specific case sensitive DB prefix.
	/// MySQL does not have a prefix for backwards compatibility.
	/// </summary>
	/// <param name="dbPrefix"></param>
	/// <returns>Null if not configured at all.</returns>
	public static ConnectionString Get(string dbPrefix)
	{
		var envString = System.Environment.GetEnvironmentVariable(dbPrefix + "ConnectionString");

		if (!string.IsNullOrEmpty(envString))
		{
			return new ConnectionString() {
				ConnectionConfig = envString,
				IsPrimaryDatabase = true
			};
		}

		var connectionStrings = AppSettings.GetSection(dbPrefix + "ConnectionStrings");

		if (connectionStrings == null)
		{
			return null;
		}

		string cStringName;

		if (Services.BuildHost == "xunit")
		{
			cStringName = "TestingConnection";
		}
		else
		{
			cStringName = System.Environment.GetEnvironmentVariable(dbPrefix + "ConnectionStringName") ?? "DefaultConnection";
		}

		var cs = connectionStrings[
			cStringName
		];

		if (cs == null)
		{
			return null;
		}

		// Primary is assumed unless stated otherwise:
		var isPrimary = true;

		var ipSection = connectionStrings["IsPrimary"];

		if (ipSection != null && (ipSection.ToLower() == "false" || ipSection.ToLower() == "no" || ipSection.ToLower() == "0"))
		{
			isPrimary = false;
		}

		return new ConnectionString()
		{
			ConnectionConfig = cs,
			IsPrimaryDatabase = isPrimary
		};
	}

	/// <summary>
	/// True if this DB service is the primary one and it must therefore mount 
	/// the locale set and every service nominally.
	/// </summary>
	public bool IsPrimaryDatabase;

	/// <summary>
	/// The connection config itself.
	/// </summary>
	public string ConnectionConfig;

}