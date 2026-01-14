namespace Api.HubSpot
{
    /// <summary>
    /// Used to indicate that a content type can provide a hubspot id.
    /// </summary>
    public interface IHaveHubSpotId
	{
        /// <summary>
        /// A way to get the hubspot Id
        /// </summary>
        /// <returns></returns>
        public string GetHubSpotId();

		/// <summary>
		/// A way to set the hubspot sync value
		/// </summary>
		/// <param name="value"></param>
		public void SetHubSpotSync(bool? value);

		/// <summary>
		/// A way to set the hubspot id value
		/// </summary>
		/// <param name="value"></param>
		public void SetHubSpotId(string value);

	}
}