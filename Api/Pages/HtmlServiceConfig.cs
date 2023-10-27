using Api.Configuration;
using System;
using System.Collections.Generic;


namespace Api.Pages
{
	
	/// <summary>
	/// Config for HtmlService
	/// </summary>
	public class HtmlServiceConfig : Config
	{
		/// <summary>
		/// Can specify more than one html config for different locales.
		/// LocaleId of 0 or 1 is the fallback. If both exist, 0 is used as the fallback.
		/// </summary>
		public uint LocaleId { get; set; } = 0;

		/// <summary>
		/// Turn on caching of pages. This also implies PreRender of true for requests where the contextual role is public and the user is 0.
		/// </summary>
		public bool CacheAnonymousPages { get; set; } = false;

		/// <summary>
		/// True if core URLs should be fully qualified with your site's PublicUrl.
		/// </summary>
		public bool FullyQualifyUrls { get; set; } = false;

		/// <summary>
		/// Block wall password. Explicitly don't define this if you don't want the block wall to appear.
		/// </summary>
		public string BlockWallPassword { get; set; } = null;

		/// <summary>
		/// A date that the block wall is active until (UTC)
		/// </summary>
		public DateTime? BlockWallActiveUntil { get; set; }

		/// <summary>
		/// True if React should be pre-rendered on pages.
		/// </summary>
		public bool PreRender {get; set; } = false;

		/// <summary>
		/// True if the page graphs should be executed server side
		/// </summary>
		public bool PreExecuteGraphs {get; set;} = false;
		
		/// <summary>
		/// Defer the main js file.
		/// </summary>
		public bool DeferMainJs { get; set;} = false;

		/// <summary>
		/// Tags added to the beginning of the head.
		/// </summary>
		public List<HeadTag> StartHeadTags { get; set; }

		/// <summary>
		/// Tags added to the end of the head.
		/// </summary>
		public List<HeadTag> EndHeadTags { get; set; }

		/// <summary>
		/// Scripts added to the beginning of the head.
		/// </summary>
		public List<BodyScript> StartHeadScripts { get; set; }

		/// <summary>
		/// Scripts added to the end of the head.
		/// </summary>
		public List<BodyScript> EndHeadScripts { get; set; }

		/// <summary>
		/// Scripts added to the config that will be added at the start of the body.
		/// </summary>
		public List<BodyScript> StartBodyJs { get; set; }

		/// <summary>
		/// Scripts added before the main.generated script
		/// </summary>
		public List<BodyScript> BeforeMainJs { get; set; }

		/// <summary>
		/// Scripts added after the main.generated script
		/// </summary>
		public List<BodyScript> AfterMainJs { get; set; }

		/// <summary>
		/// Scripts added to the config that will be added at the end of the body.
		/// </summary>
		public List<BodyScript> EndBodyJs { get; set; }

		/// <summary>
		/// Lines to be added to the robots.txt file
		/// </summary>
		public List<string> RobotsTxt { get; set; }

		/// <summary>
		/// The maximum time in seconds cacheable page should be stored in an external cache
		/// </summary>
		public ulong CacheMaxAge = 86400;
	}

	/// <summary>
	/// A head tag.
	/// </summary>
	public class HeadTag
	{
		/// <summary>
		/// Link rel="" attribute.
		/// </summary>
		public string Rel { get; set; }

		/// <summary>
		/// Link crossorigin="" attribute used by preloads.
		/// </summary>
		public string CrossOrigin { get; set; } = "anonymous";

		/// <summary>
		/// Custom attributes
		/// </summary>
		public Dictionary<string, string> Attributes { get; set; }

		/// <summary>
		/// Link as="" attribute for preloads.
		/// </summary>
		public string As { get; set; }

		/// <summary>
		/// Link href="" attribute.
		/// </summary>
		public string Href { get; set; }

		/// <summary>
		/// Meta content="" attribute.
		/// </summary>
		public string Content { get; set; }

		/// <summary>
		/// Meta name="" attribute.
		/// </summary>
		public string Name { get; set; }
	}

	/// <summary>
	/// A script tag for use in the body of the page.
	/// </summary>
	public class BodyScript
    {
		/// <summary>
		/// Script src.
		/// </summary>
		public string Src { get; set; }

		/// <summary>
		/// True if is async.
		/// </summary>
		public bool Async { get; set; } = false;

		/// <summary>
		/// Usually "text/javascript".
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Optional ID.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Defer the script until later
		/// </summary>
		public bool Defer { get; set; } = false;

		/// <summary>
		/// Raw js. Not recommended but available for quick and dirty drop-ins.
		/// </summary>
		public string Content { get; set; }

		/// <summary>
		/// Raw html content. Used for quick dropins. 
		/// </summary>
		public string NoScriptText { get; set; }

		/// <summary>
		/// Custom attributes
		/// </summary>
		public Dictionary<string, string> Attributes { get; set; }

	}

}