namespace Api.StackTools
{
	
	/// <summary>
	/// A request to a node process.
	/// </summary>
	public class Request
	{
		/// <summary>
		/// The action to request.
		/// </summary>
		public string action { get; set; }

		/// <summary>
		/// Request ID - set automatically and used to locate the response.
		/// </summary>
		public int _id { get; set; }

	}

	/// <summary>
	/// A request to start watching file changes
	/// </summary>
	public class WatchRequest : Request
	{
		/// <summary>
		/// True if the output should be minified
		/// </summary>
		public bool minified { get; set; }

		/// <summary>
		/// True if the output should be compressed
		/// </summary>
		public bool compress { get; set; }

		public WatchRequest()
		{
			action = "watch";
		}
	}
	
}