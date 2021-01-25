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

		/// <summary>
		/// Specific http methods. * is used if not set.
		/// </summary>
		public string[] Methods { get; set; }

		/// <summary>
		/// Specific headers permitted in request. * is used if not set.
		/// </summary>
		public string[] Headers { get; set; }

		/// <summary>
		/// Specific headers. None if not set.
		/// </summary>
		public string[] ExposedHeaders { get; set; }

		/// <summary>
		/// Set this to true to make sure the Access-Control-Allow-Credentials header is explicitly set and is the value 'true'.
		/// </summary>
		public bool AllowCredentials { get; set; }
	}
	
}