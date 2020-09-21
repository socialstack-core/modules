using Api.AutoForms;
using Api.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Users
{
	/// <summary>
	/// Use this to get a UserId, CreatedUtc and EditedUtc with automatic creator user field support.
	/// Alternatively use DatabaseRow directly if you want total control over your table.
	/// </summary>
	public abstract class UserCreatedRow : DatabaseRow, IHaveCreatorUser
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
		/// Gets the ID of the user who created this content.
		/// </summary>
		/// <returns></returns>
		public int GetCreatorUserId()
		{
			return UserId;
		}
	}
}
