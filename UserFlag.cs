using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.UserFlags
{
	
	/// <summary>
	/// An UserFlag
	/// </summary>
	public partial class UserFlag : RevisionRow
	{
		/// <summary>
		/// Flagged content ID.
		/// </summary>
		public int ContentId;
		
		/// <summary>
		/// Flagged content type.
		/// </summary>
		public int ContentTypeId;
	}

}