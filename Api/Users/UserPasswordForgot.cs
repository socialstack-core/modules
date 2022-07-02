using Newtonsoft.Json;


namespace Api.Users
{
    /// <summary>
    /// Used when someone has forgot their password
    /// </summary>
    public partial class UserPasswordForgot
    {
		/// <summary>
		/// The email address to submit a request for.
		/// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
