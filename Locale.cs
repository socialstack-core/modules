using Api.AutoForms;
using Api.Users;


namespace Api.Translate
{
    /// <summary>
    /// </summary>
    public partial class Locale : VersionedContent<uint>
	{
		/// <summary>
		/// The name.
		/// </summary>
		[Localized]
		[Data("hint", "The name of the locale")]
		public string Name;

		/// <summary>
		/// Usually a 5 letter locale code e.g. "en_GB". May also be just 2 e.g. "fr".
		/// </summary>
		[Data("hint", "The primary locale code, Usually a 5 letter locale code e.g. 'en_GB'. May also be just 2 e.g. 'fr' or client specific such as 'en-en'")]
		public string Code;

		/// <summary>
		/// List of comma seperated aliases for mapping request headers or custom client codes
		/// </summary>
		[Data("hint", "List of comma seperated aliases for mapping request headers or custom client codes")]
		public string Aliases;
    }

}
