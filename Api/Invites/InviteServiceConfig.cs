using Api.Configuration;
using System;
using System.Collections.Generic;


namespace Api.Invites
{
	
	/// <summary>
	/// Config for InviteService
	/// </summary>
	public class InviteServiceConfig : Config
	{
		/// <summary>
		/// True if sending an invite is permitted if the account exists.
		/// </summary>
		public bool CanSendIfAlreadyExists { get; set; } = false;
	}
}