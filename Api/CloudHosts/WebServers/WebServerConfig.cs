using Api.Configuration;
using System.Collections;
using System.Collections.Generic;


namespace Api.CloudHosts
{
	
	/// <summary>
	/// The webserver configuration.
	/// </summary>
	public partial class WebServerConfig: Config
	{
		/// <summary>
		/// Use this to specify additional domain config, such as handling a root domain on a live site alongside www.
		/// For most stage sites this is empty, whilst on prod sites it is usually the root domain in here.
		/// </summary>
		public List<DomainConfig> Domains {get; set;}
		
		/// <summary>
		/// Config specifically for NGINX.
		/// </summary>
		public NGINXConfig NGINX {get; set;}
		
	}
	
	/// <summary>
	/// Config specifically for NGINX.
	/// </summary>
	public class NGINXConfig
	{
	}
	
	/// <summary>
	/// Use this to specify additional domain config, such as handling a root domain on a live site alongside www 
	/// </summary>
	public class DomainConfig
	{
		/// <summary>
		/// The domain.
		/// </summary>
		public string Domain {get; set;}
		
		/// <summary>
		/// Optional - if not set, the target is derived from the configured domains (in frontend service config).
		/// For example if domain is "site.com" and a site URL is "www.site.com" then redirectTo will be "www.site.com".
		/// </summary>
		public string RedirectTo {get; set;}
	}

}