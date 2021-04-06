using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.WebSockets;

namespace Api.Huddles
{

	/// <summary>
	/// A Huddle
	/// </summary>
	public partial class Huddle : IAmLive
	{
		/// <summary>
		/// If there's an active activity, the time it started at.
		/// </summary>
		public DateTime? ActivityStartUtc;
		
		/// <summary>
		/// Activity content type ID, e.g. "Quiz".
		/// </summary>
		public int ActivityContentTypeId;
		
		/// <summary>
		/// E.g. quiz ID.
		/// </summary>
		public uint ActivityContentId;

		/// <summary>
        /// Whenever a new activity is started or restarted, a new id is made here. Primary purpose is for tracking results
        /// for a given instance. 
        /// </summary>
		public uint ActivityInstanceId;
		
		/// <summary>
		/// The current activity, if there is one.
		/// </summary>
		public object Activity {get; set;}

		/// <summary>
		/// Timed activities can store their duartion here. 
		/// </summary>
		public int? ActivityDurationTicks;
	}
}
