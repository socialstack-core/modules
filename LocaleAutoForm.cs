using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Translate
{
    /// <summary>
    /// Used when creating or updating a blog
    /// </summary>
    public partial class LocaleAutoForm : AutoForm<Locale>
    {
		/// <summary>
		/// The name of the blog in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// Usually a 5 letter locale code e.g. "en_GB". May also be just 2 e.g. "fr".
		/// </summary>
		public string Code;
	}
}
