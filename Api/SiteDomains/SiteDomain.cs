using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.SiteDomains
{
	
	/// <summary>
	/// A SiteDomain
	/// </summary>
	public partial class SiteDomain : VersionedContent<uint>
	{
        /// <summary>
        /// The full DNS address e.g. "www.site.com" or "forum.site.com".
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Domain;
	}

}