using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.CloudHosts
{
	
	/// <summary>
	/// A DomainCertificateChallenge
	/// </summary>
	public partial class DomainCertificateChallenge : VersionedContent<uint>
	{
        /// <summary>
        /// The value to return when a request for the token arrives.
        /// </summary>
		public string VerificationValue;
		
		/// <summary>
		/// The token.
		/// </summary>
		public string Token;

		/// <summary>
		/// The request which this challenge spawned from.
		/// </summary>
		public uint DomainCertificateId;

	}

}