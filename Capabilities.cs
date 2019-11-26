namespace Api.Permissions
{

#warning outmode
	/// <summary>
	/// Capabilities are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Capabilities
    {
		
		/// <summary>
		/// Create a translation.
		/// </summary>
        public static Capability TranslationCreate;
		
		/// <summary>
		/// Update a translation.
		/// </summary>
		public static Capability TranslationUpdate;

		/// <summary>
		/// Delete a translation.
		/// </summary>
		public static Capability TranslationDelete;

		/// <summary>
		/// Upload a new PO file.
		/// </summary>
		public static Capability TranslationPoUpload;

		/// <summary>
		/// Search for locales.
		/// </summary>
		public static Capability TranslationLocaleSearch;
		
    }
	
}
