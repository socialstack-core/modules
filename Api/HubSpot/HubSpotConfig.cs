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
        "lifecyclestage": "111111111",
        }
	*/

	/*
	to get the lifecycle stage 

	. goto contacts 
	. click on settings
	. click on lkifecycle stage 
	. find the relevant entry such as "Website contact"
	. open the entry and ignore any popups
	. click on the tag icon </> 
	. use the internal id to replace "111111111" above
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
	public List<MappingEntity> Mappings;

	/// <summary>
	/// Add any default values to assigned when creating an account 
	/// </summary>
	public IDictionary<string, string> DefaultMappings;	

	/// <summary>
	/// Config item to store the mapping between a content type and hubspot
	/// </summary>
	public class MappingEntity
	{
		/// <summary>
		/// The name of the content type e.g. User/Event etc
		/// </summary>
		public string Entity;

		/// <summary>
		/// Mapped field names which are passed to hubspot
		/// </summary>
		public IDictionary<string, string> FieldNames;
	}
}