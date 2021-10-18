using Api.Database;
using Api.WebSockets;
using Api.Startup;

namespace Api.PhotosphereTracking
{

	/// <summary>
	/// A Video
	/// </summary>
	[CacheOnly]
	public partial class PhotosphereTrack : Content<uint>
	{
		/// <summary>
		/// Current page this user is on. Because of url tokens, these aren't as unique as URL is.
		/// </summary>
		public uint PageId;

		/// <summary>
		/// The user id of the thing being tracked
		/// </summary>
		public uint UserId;

		/// <summary>
        /// The server Id that this record is associated to
        /// </summary>
        public uint ServerId;

		/// <summary>
		/// Websocket connection ID.
		/// </summary>
		public uint WebSocketId;

		/// <summary>
		/// The page url the user is on
		/// </summary>
		public string Url;

		/// <summary>
		/// The X position on this page
		/// </summary>
		public double PosX;

		/// <summary>
		/// The Y position on this page
		/// </summary>
		public double PosY;

		/// <summary>
		/// The Z position on this page
		/// </summary>
		public double PosZ;

		/// <summary>
		/// The X rotation on this page
		/// </summary>
		public double RotationX;

		/// <summary>
		/// The Y rotation on this page
		/// </summary>
		public double RotationY;
	}

}