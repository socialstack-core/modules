using Api.Configuration;

/// <summary>
/// Config for the SendGrid API.
/// </summary>
public class SendGridConfig : Config
{
	/// <summary>
	/// API key to use.
	/// </summary>
	public string ApiKey {get; set;}
	/// <summary>
	/// Nice name to display in the from field.
	/// </summary>
	public string FromName {get; set;}
	/// <summary>
	/// Email address for the email to be from.
	/// </summary>
	public string FromAddress {get; set;}
	
}