using Api.AutoForms;
using Api.Database;
using System;

namespace Api.Users
{
	/// <summary>
	/// Use this to get a UserId, CreatedUtc and EditedUtc with automatic creator user field support.
	/// Alternatively use DatabaseRow directly if you want total control over your table.
	/// </summary>
	public abstract class UserCreatedContent<T> : Content<T>, IHaveTimestamps, IHaveCreatorUser where T: struct
	{
		/// <summary>
		/// The user who created this content.
		/// </summary>
		[Module(Hide = true)]
		public int UserId;

		/// <summary>
		/// The UTC creation date.
		/// </summary>
		[Module(Hide = true)]
		public DateTime CreatedUtc;

		/// <summary>
		/// The UTC last edited date.
		/// </summary>
		[Module(Hide = true)]
		public DateTime EditedUtc;

		/// <summary>
		/// The user who created this content.
		/// </summary>
		[Module(Hide = true)]
		public UserProfile CreatorUser { get; set; }

		/// <summary>
		/// Gets the CreatedUtc 
		/// </summary>
		/// <returns></returns>
		public DateTime GetCreatedUtc()
		{
			return CreatedUtc;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public DateTime GetEditedUtc()
        {
			return EditedUtc;
        }

		/// <summary>
		/// Gets the ID of the user who created this content.
		/// </summary>
		/// <returns></returns>
		public int GetCreatorUserId()
		{
			return UserId;
		}
	}
}
