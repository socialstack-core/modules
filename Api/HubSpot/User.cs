using Api.HubSpot;

namespace Api.Users
{

    public partial class User : IHaveHubSpotId
    {
        /// <summary>
        /// The hub spot id (if any)
        /// </summary>
        public string HubSpotId;

		/// <summary>
		/// Flag to indicate that the account should be sync'd with hubspot
		/// </summary>
		public bool? SyncToHubSpot;

		/// <summary>
		/// Fulfills the IHaveHubSpotId contract. 
		/// </summary>
		/// <returns></returns>
		public string GetHubSpotId()
        {
            return HubSpotId;
        }

		/// <summary>
		/// Fulfills the IHaveHubSpotId contract. 
		/// </summary>
		/// <returns></returns>
		public void SetHubSpotSync(bool? value)
		{
			SyncToHubSpot = value;
		}

		/// <summary>
		/// Fulfills the IHaveHubSpotId contract. 
		/// </summary>
		/// <returns></returns>
		public void SetHubSpotId(string value)
		{
			HubSpotId = value;
		}

	}
}