using Newtonsoft.Json;


namespace Api.Translate
{
    /// <summary>
    /// Used when updating a translation
    /// </summary>
    public class TranslationUpdate
    {
		/// <summary>
		/// The new html for the translation.
		/// </summary>
        [JsonProperty("html")]
        public string Html { get; set; }
    }
}
