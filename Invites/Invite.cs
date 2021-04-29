using Api.Startup;
using Api.Users;
using System;

namespace Api.Permissions{
	
	/// <summary>
	/// An invite to a particular type (e.g. Invite[User] for a user invite) which is mapped to some content.
	/// </summary>
	public abstract class Invite<T> : UserCreatedContent<uint>{

		/// <summary>
		/// Accepted/ Rejected time
		/// </summary>
		public DateTime? AcceptRejectUtc;

		/// <summary>
		/// 0 = Pending
		/// 1 = Accepted
		/// 2 = Rejected
		/// </summary>
		public int State;

		/// <summary>
		/// True if this invite has not been accepted yet.
		/// </summary>
		public bool IsPending
		{
			get
			{
				return State == 0;
			}
		}

		/// <summary>
		/// True if this invite is accepted.
		/// </summary>
		public bool IsAccepted
		{
			get
			{
				return State == 1;
			}
		}

		/// <summary>
		/// True if this invite is rejected.
		/// </summary>
		public bool IsRejected
		{
			get
			{
				return State == 2;
			}
		}

	}

	/// <summary>
	/// A user invite.
	/// </summary>
	[ListAs("UserInvites")]
	[HasVirtualField("InvitedUser", typeof(User), "InvitedUserId")]
	public class UserInvite : Invite<User>{

		/// <summary>
		/// The invited user ID.
		/// </summary>
		public uint InvitedUserId;

	}
	
}