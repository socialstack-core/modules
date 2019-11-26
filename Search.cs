using Newtonsoft.Json;


namespace Api.Translate
{
    /// <summary>
    /// Used when searching translations
    /// </summary>
    public partial class Search
    {
		/// <summary>
		/// The search query.
		/// </summary>
        [JsonProperty("query")]
        public string Query { get; set; }
    }
}
