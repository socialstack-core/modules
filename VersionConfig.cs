
namespace Api.VersionChecker
{
	/// <summary>
	/// Latest build versions (Unix millisecond timestamps) of the UI. This whole thing is sent to the frontend.
	/// </summary>
	public partial class VersionConfig{
		
		/// <summary>Latest web UI build version.</summary>
		public long web { get; set; }
		/// <summary>Latest Android UI build version.</summary>
		public long android { get; set; }
		/// <summary>Latest iOS UI build version.</summary>
		public long ios { get; set; }

	}
	
}