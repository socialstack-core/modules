using Api.Configuration;
using System.Collections.Generic;

/// <summary>
/// Configuration for accessing Hubspot API endpoints
/// </summary>
public class HubSpotConfig : Config
{
	/*
    "Mappings": [
        {
        "Entity" : "User",
        "FieldNames" : {
            "Email": "email",
            "FirstName":"firstname",
            "LastName":"lasttname"
            }
        }
    ]   
	*/

	/*
    "DefaultMappings" : {
        "lifecyclestage": "Website Contact",
        }
	*/


	/// <summary>
	/// URL for the endpoints
	/// </summary>
	public string Url = "https://api.hubapi.com/crm/v3/";

	/// <summary>
	/// API key hubspot -> legacy apps -> application key
	/// </summary>
	public string ApiKey;

	/// <summary>
	/// Is the automated sync enabled
	/// </summary>
	public bool EnableSync;

	/// <summary>
	/// List of content type mappings from ss to hubspot
	/// </summary>		
	public List<MappingEntity> Mappings { get; set; }

	/// <summary>
	/// Add any default values to assigned when creating an account 
	/// </summary>
	public IDictionary<string,string> DefaultMappings { get; set; }	

	/// <summary>
	/// Config item to store the mapping between a content type and hubspot
	/// </summary>
	public class MappingEntity
	{
		/// <summary>
		/// The name of the content type e.g. User/Event etc
		/// </summary>
		public string Entity { get; set; }

		/// <summary>
		/// Mapped field names which are passed to hubspot
		/// </summary>
		public IDictionary<string,string> FieldNames { get; set; }
	}
}