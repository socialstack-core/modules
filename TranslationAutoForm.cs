using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Translate
{
    /// <summary>
    /// Used when creating or updating a translation
    /// </summary>
    public partial class TranslationAutoForm : AutoForm<Translation>
    {
		/// <summary>
		/// The locale that the translation is in, e.g. "fr_FR".
		/// </summary>
        public int LocaleId;
		
		/// <summary>
		/// E.g. "forum" or "gallery". It's just the type name lower cased.
		/// </summary>
        public string ContentType;

		/// <summary>
		/// The ID of the content this is a translation for.
		/// Use this when e.g. translating gallery titles.
		/// </summary>
        public int ContentId;

		/// <summary>
		/// The field key. If translating content, it's just the lowercase field name. 
		/// Otherwise it's whatever a component is using as its key during rendering.
		/// </summary>
        public string Key;

		/// <summary>
		/// The HTML translation string. Can also include %d and %s placeholders.
		/// </summary>
        public string Html;
        
    }
}
