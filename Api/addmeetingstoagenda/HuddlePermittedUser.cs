using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A HuddlePermittedUser
	/// </summary>
	public partial class HuddlePermittedUser
	{
		
		/// <summary>
		/// If on a user agenda, the entry ID.
		/// </summary>
		public int? AgendaEntryId;

		/// <summary>
		/// If this value is true and the config allows for collision, this entry will be created in the case of a collision.
		/// If this value is false and the config allows collision, a prompt will be returned for a confirmation submission
		/// </summary>
		public bool ForceAccept { get; set; }
	}
	
}