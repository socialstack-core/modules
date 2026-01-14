using Api.Database;
using Api.Permissions;
using Api.Startup;
using Newtonsoft.Json;
using System;

namespace Api.Payments
{

	[HasVirtualField("Purchase", typeof(Purchase), "PurchaseId")]

	/// <summary>
	/// A token to allow anon access to a purchase during 3d auth and via email links.
	/// </summary>
	public partial class PurchaseToken : Content<uint>
	{
		/// <summary>
		/// The randomly generated token, used by the client, to prove ownership of the 2nd channel.
		/// </summary>
		[DatabaseField(Length =40)]
		[Permissions(ReadRule = "IsEmail()", Roles = "*")]
		[Permissions(WriteRule = "false", Roles = "*")]
		public string Token;

		/// <summary>
		/// True if this token has ever been used.
		/// </summary>
        [JsonIgnore]
		public uint UsedCount;

        /// <summary>
        /// Is the token single use only.
        /// </summary>
        [JsonIgnore]
        public bool IsSingleUse = true;

		/// <summary>
		/// Created date UTC. This is used to establish when the token was created
		/// </summary>
		[JsonIgnore]
		public DateTime CreatedUtc;

        /// <summary>
		/// Expires date UTC. This is used to establish the lifespan of the token 
		/// </summary>
		[JsonIgnore]
		public DateTime ExpiresUtc;

		/// <summary>
		/// The purchase this request is for.
		/// </summary>
		[Permissions(ReadRule = "IsEmail()", Roles = "*")]
		[Permissions(WriteRule = "false", Roles = "*")]
		public uint PurchaseId;

        /// <summary>
		/// The scope/usage for the token (View/Authenticate/etc).
		/// </summary>
		[JsonIgnore]
		public string Scope;

        /// <summary>
		/// The ip address(es) used to access the token
		/// </summary>
		[JsonIgnore]
		public string IpAddress;

	}
}