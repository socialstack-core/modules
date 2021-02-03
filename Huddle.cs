using System;
using System.Collections.Generic;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A Huddle
	/// </summary>
	public partial class Huddle : RevisionRow
	{
		/// <summary>
		/// 0 = Public (anyone can join)
		/// 1 = Open invite (it's private, but people can essentially permit themselves, up to the limit).
		/// 2 = Closed invite (it's private, and only the person who created it can invite users).
		/// 3 = Administered (it's private, and someone - currently the creator - can set who is able to talk).
		/// 4 = Audience (anyone can join in a one way format, and users are randomly distributed to servers).
		/// </summary>
		public int HuddleType;

		/// <summary>
		/// Note: This is only set when a compatible Huddle server (v1.1 or greater) is in use. All socialstack cloud servers are compatible.
		/// </summary>
		public int UsersInMeeting;

		/// <summary>
		/// Assigned automatically.
		/// </summary>
		public int HuddleServerId;

		/// <summary>
		/// Start time of the huddle. If not provided, "now" is assumed.
		/// </summary>
		public DateTime StartTimeUtc;

		/// <summary>
		/// Scheduled end time of the huddle. Note that this is not a hard deadline - it just helps with load balancing huddles.
		/// If not provided, StartTimeUtc + 1hr is the default.
		/// </summary>
		public DateTime EstimatedEndTimeUtc;

		/// <summary>
		/// Estimated max participant count. If unfilled, 2 is assumed.
		/// </summary>
		public int EstimatedParticipants;

		/// <summary>
		/// Invited users to this huddle.
		/// </summary>
		public List<HuddlePermittedUser> Invites { get; set; }
		
		/// <summary>
		/// Optional meeting title.
		/// </summary>
		[DatabaseField(Length=100)]
		public string Title;
	}

}