using Api.Database;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json;
using System;

namespace Api.Invites{
	
	/// <summary>
	/// An invite which typically invites a user to join.
	/// </summary>
	public partial class Invite : UserCreatedContent<uint>{
		
		/// <summary>
		/// The date this expires. If not specified by creator, it defaults to 48h from now.
		/// </summary>
		public DateTime ExpiryUtc = DateTime.UtcNow.AddDays(2);
		
		/// <summary>
		/// Accepted/ Rejected time
		/// </summary>
		public DateTime? AcceptRejectUtc;

		/// <summary>
		/// Token to use in e.g. an email or message to signify this invite.
		/// </summary>
		[DatabaseField(Length =40)]
		[JsonIgnore]
		public string Token;

		/// <summary>
		/// Invite type. If this is non-zero, this uses the template with the type on the end such as "invited_join_1". 
		/// This allows people to be invited to fulfil different roles.
		/// </summary>
		public uint InviteType;

		/// <summary>
		/// The joining user ID.
		/// </summary>
		public uint? InvitedUserId;
		
		/// <summary>
		/// 0 = Pending
		/// 1 = Accepted
		/// 2 = Rejected
		/// </summary>
		public int State;
		
		/// <summary>
		/// A temporary email address/ phone number, used only when creating an invite.
		/// </summary>
		[JsonIgnore]
		public string UserLocator {get; set;}

		/// <summary>
		/// A temporary first name, used only when creating an invite.
		/// </summary>
		[JsonIgnore]
		public string FirstName { get; set; }

		/// <summary>
		/// A temporary last name, used only when creating an invite.
		/// </summary>
		[JsonIgnore]
		public string LastName { get; set; }

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
}