using Api.Configuration;


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
		
	}
	
}