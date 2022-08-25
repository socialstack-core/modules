
using Api.Configuration;
using System.Collections.Generic;

namespace Api.Translate
{
	/// <summary>
	/// Config for translation service.
	/// </summary>
	public class TranslationServiceConfig : Config
	{
		/// <summary>
		/// When exporting/importing translations via port files should we reformat the canvas elements to simplify the format 
		/// </summary>
		public bool ReformatCanvasElements {get; set;} = false;

		/// <summary>
		/// Automatically add any new translation elements when parsing content (DEV ONLY)
		/// </summary>
		public bool AutoAddTranslationElements { get; set;} = false;

		/// <summary>
		/// List of modules to exclude from translation auto add 
		/// </summary>		
		public List<string> AutoAddExcludeModules { get; set; }

	}
	
}