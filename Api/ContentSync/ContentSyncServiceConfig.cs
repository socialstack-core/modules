using Api.Configuration;


namespace Api.ContentSync
{
	/// <summary>
	/// The appsettings.json config block for push notification config.
	/// </summary>
    public partial class ContentSyncServiceConfig : Config
    {
		/// <summary>
		/// Verbose messaging mode
		/// </summary>
		public bool Verbose {get; set;}
		
		/// <summary>
		/// True if this cluster is global and will instead bind the any interface.
		/// </summary>
		public bool GlobalCluster { get; set;}
		
		/// <summary>
		/// Custom hostname override. Usually leave this blank.
		/// </summary>
		public string HostName { get; set; }
	}
	
}
