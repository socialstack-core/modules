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
		[Data("hint", "The primary locale code, usually a 5 letter locale code e.g. 'en_GB'. May also be just 2 e.g. 'fr' or client specific such as 'en-en'")]
		public string Code;

		/// <summary>
		/// Associated flag image representing the locale.
		/// </summary>
		[Data("hint", "Associated flag image representing the locale")]
		public string FlagIconRef;

		/// <summary>
		/// List of comma seperated aliases for mapping request headers or custom client codes
		/// </summary>
		[Data("hint", "List of comma seperated aliases for mapping request headers or custom client codes")]
		public string Aliases;

		/// <summary>
		/// Indicates if the locale is not currently available
		/// </summary>
		[Data("hint", "Indicates if the locale is not currently available")]
		public bool isDisabled;

		/// <summary>
		/// Indicates this locale goes primarily right to left, such as Hebrew or Arabic.
		/// </summary>
		[Data("hint", "Set this if the locale goes right to left, such as Hebrew or Arabic")]
		public bool RightToLeft;
	}

}
