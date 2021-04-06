using Api.Configuration;
using System.Collections.Generic;


namespace Api.CanvasRenderer
{
	
	/// <summary>
	/// Config for HtmlService
	/// </summary>
	public class FrontendCodeServiceConfig : Config
	{
		/// <summary>
		/// True if it should load the prebuilt UI. This is implied as true if no Source directory is found (i.e. you can force a true by just not deploying your UI/Source directory).
		/// </summary>
		public bool Prebuilt {get; set; } = false;

		/// <summary>
		/// True if the watcher mode should run minified JS.
		/// </summary>
		public bool Minified { get; set; } = false;

		/// <summary>
		/// True to use React instead of Preact (Preact is the default).
		/// </summary>
		public bool React { get; set; } = false;
	}	
}