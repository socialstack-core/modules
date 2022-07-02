using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.UserFlags
{
	
	/// <summary>
	/// An UserFlag
	/// </summary>
	public partial class UserFlagOption : VersionedContent<uint>
	{
		/// <summary>
		/// The user flag option body json
		/// </summary>
		public string BodyJson;
	}

}