
using Api.Configuration;

namespace Api.Translate
{
	/// <summary>
	/// Config for locale service.
	/// </summary>
	public class LocaleServiceConfig : Config
	{
		/// <summary>
		/// True (default) if the accept-lang header will be used to 
		/// establish default language when a user is seen for the first time.
		/// </summary>
		public bool HandleAcceptLanguageHeader {get; set;} = true;

		/// <summary>
		/// True (default) if the CF-IPCountry header will be used to 
		/// establish default language.
		/// </summary>
		public bool HandleCloudFlareHeader { get; set; } = true;

		/// <summary>
		/// Set this to true to disable URL prefixing. Not recommended.
		/// </summary>
		public bool DisableUrlPrefix { get; set; } = false;
	}

}