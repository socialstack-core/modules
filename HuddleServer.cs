using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A HuddleServer
	/// </summary>
	public partial class HuddleServer : RevisionRow
	{
        /// <summary>
        /// The server address, excluding protocol. For example, "huddle1.site.com"
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Address;
	}

}