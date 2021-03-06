using System;
using Api.Database;
using Api.Users;
using Api.WebSockets;
using Api.UserFlags;
using Newtonsoft.Json;

namespace Api.LiveChats
{
	
	/// <summary>
	/// A chat message in the live chat.
	/// </summary>
	public partial class LiveChatMessage : VersionedContent<uint>, IAmFlaggable
	{	
		/// <summary>
		/// The message itself. Can contain emoji.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Message;
		
		/// <summary>
		/// # of profanity filter hits in this text. The message is hidden if this is non-zero; a mod must permit it through.
		/// </summary>
		public int ProfanityWeight;
		
		/// <summary>
		/// Number of user flags this content received.
		/// 5 or more will auto hide. 1 or more will make it appear in the admin area.
		/// </summary>
		public int UserFlags;


		/// <summary>
		/// Number of user flags this content received.
		/// 5 or more will auto hide. 1 or more will make it appear in the admin area.
		/// </summary>
		[JsonIgnore]
		public int UserFlagCount
		{
			get {
				return UserFlags;
			}
			set {
				UserFlags = value;
			}
		}
	}
	
}