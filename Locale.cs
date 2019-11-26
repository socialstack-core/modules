using Api.Database;


namespace Api.Translate
{
    /// <summary>
    /// </summary>
    public class Locale : DatabaseRow
    {
		/// <summary>
		/// Usually a 5 letter locale code e.g. "en_GB". May also be just 2 e.g. "fr".
		/// </summary>
		public string Code;

		/// <summary>
		/// The list of translations in this locale.
		/// </summary>
		[Newtonsoft.Json.JsonIgnore]
		public Translations Translations { get; set; }
    }

}
