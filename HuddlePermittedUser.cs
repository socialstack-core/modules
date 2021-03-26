using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A HuddlePermittedUser
	/// </summary>
	public partial class HuddlePermittedUser : RevisionEntity<int>
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
		
		/// <summary>
		/// Optional meeting title.
		/// </summary>
		[DatabaseField(Length=100)]
		public string Title;

		/// <summary>
		/// Important: only used specifically by the huddlepermitteduser/list endpoint. It's null everywhere else.
		/// </summary>
		public HuddleMeta HuddleMeta { get; set; }
	}

	/// <summary>
	/// Base info about a huddle.
	/// </summary>
	public class HuddleMeta
	{
		/// <summary>
		/// Huddles raw title.
		/// </summary>
		public string Title;
		/// <summary>
		/// Start time from the huddle.
		/// </summary>
		public DateTime StartTimeUtc;
		/// <summary>
		/// Est end time from the huddle.
		/// </summary>
		public DateTime EstimatedEndTimeUtc;
	}
}