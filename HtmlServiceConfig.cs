using Api.Configuration;
using System.Collections.Generic;


namespace Api.Pages
{
	
	/// <summary>
	/// Config for HtmlService
	/// </summary>
	public class HtmlServiceConfig : Config
	{
		
		/// <summary>
		/// True if React should be pre-rendered on pages.
		/// </summary>
		public bool PreRender {get; set; } = false;

		/// <summary>
		/// Tags added to the beginning of the head.
		/// </summary>
		public List<HeadTag> StartHeadTags { get; set; }

		/// <summary>
		/// Tags added to the end of the head.
		/// </summary>
		public List<HeadTag> EndHeadTags { get; set; }

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
	}

	public class HeadTag
    {
		public string Rel { get; set; }

		public string Href { get; set; }

		public string Content { get; set; }

		public string Property { get; set; }
	}

	public class BodyScript
    {
		public string Src { get; set; }

		public bool Async { get; set; } = false;

		public string Type { get; set; }

		public string Id { get; set; }

		public bool Defer { get; set; } = false;

		public string Content { get; set; }
	}
	
}