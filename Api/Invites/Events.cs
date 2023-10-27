using Api.Invites;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Events on the Invite type.
		/// </summary>
		public static InviteEvents Invite;
	}

	/// <summary>
	/// Events specifically for invites.
	/// </summary>
	public class InviteEvents : EventGroup<Invite>
	{

		/// <summary>
		/// Just before the invite is sent. You can extend the custom payload in this event used to render emails/ SMS if you'd like.
		/// </summary>
		public EventHandler<InviteCustomPayloadData, Invite> BeforeSend;

		/// <summary>
		/// Just after the invite is sucessfully redeemed.
		/// </summary>
		public EventHandler<Invite> AfterRedeem;

	}

}