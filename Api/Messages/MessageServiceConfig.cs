
using Api.Configuration;

namespace Api.Messages
{
	
	/// <summary>
	/// Config for message service
	/// </summary>
	public class MessageServiceConfig : Config
	{
		
		/// <summary>
		/// If set, the site is able to accept support emails.
		/// </summary>
		public string SupportEmailAddress {get;set;}
		
	}
	
}