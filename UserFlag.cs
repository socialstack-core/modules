using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.UserFlags
{
	
	/// <summary>
	/// An UserFlag
	/// </summary>
	public partial class UserFlag : VersionedContent<int>
	{
		/// <summary>
		/// Flagged content ID.
		/// </summary>
		public int ContentId;
		
		/// <summary>
		/// Flagged content type.
		/// </summary>
		public int ContentTypeId;

		/// <summary>
		/// The id of the user flag option that was selected for this flag.
		/// </summary>
		public int UserFlagOptionId;

		/// <summary>
		/// The user flag option that was selected for the flag.
		/// </summary>
		public UserFlagOption UserFlagOption { get; set; }
	}

}