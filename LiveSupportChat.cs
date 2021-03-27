using System;
using System.Collections.Generic;
using Api.Database;
using Api.Permissions;
using Api.Translate;
using Api.Users;
using Api.WebSockets;


namespace Api.LiveSupportChats
{
	
	/// <summary>
	/// A LiveSupportChat
	/// </summary>
	public partial class LiveSupportChat : VersionedContent<int>, IAmLive
	{
        /// <summary>
		/// Total messages in this chat.
		/// Updating this triggers the LastEditedUtc date to change.
		/// </summary>
		public int MessageCount;
		
		/// <summary>
		/// User ID of current support worker responding to the chat.
		/// </summary>
		public int? AssignedToUserId;

		/// <summary>
		/// The full name of the user that in initiated this chat.
		/// </summary>
		public string FullName;

		/// <summary>
		/// The time this user entered the queue. When the user is done being serviced, they will be removed from the queue by admin user.
		/// </summary>
		public DateTime? EnteredQueueUtc;

		/// <summary>
		/// Determines whether a user can download the chat. Happens at the end of booking or once an operator chat has been initiated.
		/// </summary>
		public bool CanDownload;

		/// <summary>
		/// The current meeting appointment we are constructing in this chat.
		/// </summary>
		public int? MeetingAppointmentId;

		/// <summary>
		/// The user's who initiated the chat's address.
		/// </summary>
		public string Email;

		/// <summary>
		/// The current mode of this chat. If its null, its standard. Otherwise, project dependent.
		/// </summary>
		public int? Mode;

		/// <summary>
		/// The current expert question we are constructing in this chat.
		/// </summary>
		public int? ExpertQuestionId;
	}

}