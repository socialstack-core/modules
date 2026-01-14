using System;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.AuditEvents
{

	/// <summary>
	/// An Audit event
	/// </summary>
	[HasVirtualField("realUser", typeof(User), "RealUserId")]
	public partial class AuditEvent : UserCreatedContent<uint>
	{
		/// <summary>
		/// Non-zero if this event was created whilst someone was being impersonated.
		/// </summary>
		public uint RealUserId;

		/// <summary>
		/// Set if the event is actually relating to some specific content type.
		/// </summary>
		public string ContentType;

		/// <summary>
		/// Related content ID.
		/// </summary>
		public ulong ContentId;

		/// <summary>
		/// 1 = Create, 2 = Update, 3 = Delete.
		/// </summary>
		public uint ActionType = 2;

	}

}