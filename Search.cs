using Newtonsoft.Json;


namespace Api.Users
{
    /// <summary>
    /// Used when searching users
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
