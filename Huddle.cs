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
		public int ActivityContentId;
		
		/// <summary>
		/// The current activity, if there is one.
		/// </summary>
		public object Activity {get; set;}
	}
}
