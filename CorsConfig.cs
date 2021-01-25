namespace Api.Startup
{
	/// <summary>
	/// The appsettings.json config block for cors.
	/// </summary>
    public class CorsConfig
    {
		/// <summary>
		/// Specific origins. * is used if not set.
		/// </summary>
		public string[] Origins { get; set; }
	}
	
}