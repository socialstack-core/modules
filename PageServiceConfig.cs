using Api.Configuration;
using System;
using System.Collections.Generic;


namespace Api.Pages
{
	
	/// <summary>
	/// Config for PageService
	/// </summary>
	public class PageServiceConfig : Config
	{
		/// <summary>
		/// True if default pages should be installed when they don't exist. This is only checked at startup.
		/// </summary>
		public bool InstallDefaultPages { get; set; } = true;
	}
}