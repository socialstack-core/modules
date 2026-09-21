
using Api.Configuration;
using System.Collections.Generic;

namespace Api.DynamicSiteMap
{
    /// <summary>
    /// Config for dynamic site mapping service.
    /// </summary>
    public class DynamicSiteMapConfig : Config
    {
		/// <summary>
		/// The maximum number of pages per sitemap file
		/// </summary>
		public int MaxSiteMapPages { get; set; } = 1000;

		/// <summary>
		/// List of exlcusions to apply to the url
		/// </summary>		
		public List<string> UrlExclusions { get; set; }

		/// <summary>
		/// List of inclusions to apply to the url
		/// </summary>		
		public List<string> UrlInclusions { get; set; }

		/// <summary>
		/// Flag to optionally disable service.
		/// </summary>
		public bool Disabled { get; set; } = false;
    }
}