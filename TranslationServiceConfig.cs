
using Api.Configuration;

namespace Api.Translate
{
	/// <summary>
	/// Config for translation service.
	/// </summary>
	public class TranslationServiceConfig : Config
	{
		/// <summary>
		/// When exportinyt/importing translations via port files should we reformat the canvas elements to simplify the format 
		/// </summary>
		public bool ReformatCanvasElements {get; set;} = false;
	}
	
}