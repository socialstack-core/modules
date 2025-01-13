using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.CloudHosts
{
	
	/// <summary>
	/// A DomainCertificate
	/// </summary>
	public partial class DomainCertificate : VersionedContent<uint>
	{
        /// <summary>
        /// The domain that has been requested.
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Domain;

		/// <summary>
		/// True if the certificate is ready. Status is also 1.
		/// </summary>
		public bool Ready;

		/// <summary>
		/// A random file key to avoid ID collisions in the filesystem.
		/// </summary>
		public string FileKey;

		/// <summary>
		/// Expiry time (UTC) of this certificate.
		/// </summary>
		public DateTime? ExpiryUtc;
		
		/// <summary>
		/// The serverId of the server in a cluster which created this request.
		/// </summary>
		public uint ServerId;

		/// <summary>
		/// Let's encrypt order URL for debugging the status.
		/// </summary>
		public string OrderUrl;

		/// <summary>
		/// 0=Not yet started, 1=Completed (may have failed), 2=Requesting, 3=Ordered, 4=Waiting for verification
		/// </summary>
		public uint Status;

		/// <summary>
		/// Clustered servers can be all waiting for a singular request.
		/// This informs the cluster that the server being waited on is still here.
		/// </summary>
		public DateTime LastPingUtc;
	}

}