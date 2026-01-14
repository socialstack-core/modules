using Api.Configuration;

namespace Api.GuestUsers
{
	/// <summary>
	/// Configuration for Guest User Registration
	/// </summary>
	public class GuestUsersConfig : Config
    {
        /// <summary>
		/// Is user registration allowed 
		/// </summary>
        [Frontend]
        public bool IsEnabled { get; set; } = false;
    }
}
