using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A HuddleServer
	/// </summary>
	public partial class HuddleServer : VersionedContent<uint>
	{
        /// <summary>
        /// The server address, excluding protocol. For example, "huddle1.site.com"
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Address;

		/// <summary>
		/// Optional region ID. Can be used to localise people to a nearby huddle server.
		/// </summary>
		public uint RegionId;
		
		/// <summary>
		/// The server public key in hex format. Used to validate status updates are legitimate.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string PublicKey;
	}

}