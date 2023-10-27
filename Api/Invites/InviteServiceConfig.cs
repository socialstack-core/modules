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

		/// <summary>
		/// True if sending an invite can occur via SMS. The UserLocator would be a phone number.
		/// </summary>
		public bool CanSendViaSms { get; set; } = false;
	}
}