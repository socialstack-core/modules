using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A HuddlePermittedUser
	/// </summary>
	public partial class HuddlePermittedUser : RevisionRow
	{
		/// <summary>
		/// Usually the content type for "user".
		/// </summary>
		public int InvitedContentTypeId;

		/// <summary>
		/// The ID of the invited content (usually a user ID, but can be broader - e.g. invite a company to join a meeting).
		/// </summary>
		public int InvitedContentId;

		/// <summary>
		/// The invited content. Usually a user, but can also be e.g. a company.
		/// </summary>
		public object InvitedContent { get; set; }
		
		/// <summary>
		/// User allowed to a private huddle.
		/// </summary>
		public int PermittedUserId;
		
		/// <summary>
		/// An invited user has rejected the invite.
		/// </summary>
		public bool Rejected;
		
		/// <summary>
		/// The huddle ID.
		/// </summary>
		public int HuddleId;

		/// <summary>
		/// The permitted user (if there is one - can be null).
		/// </summary>
		public UserProfile PermittedUser { get; set; }

		/// <summary>
		/// Huddle start time that was accepted (if this invite is an accepted one).
		/// </summary>
		public DateTime? AcceptedStartUtc;

		/// <summary>
		/// Huddle end time that was accepted (if this invite is an accepted one).
		/// </summary>
		public DateTime? AcceptedEndUtc;

		/// <summary>
		/// True if this user is the creator of the meeting.
		/// </summary>
		public bool Creator;
	}

}