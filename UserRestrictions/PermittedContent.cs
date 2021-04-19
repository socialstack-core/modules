using Api.Database;
using Api.Startup;
using Api.Users;
using System;

namespace Api.Permissions
{
	/// <summary>
	/// A PermittedContent
	/// </summary>
	public partial class PermittedContent : MappingEntity
	{
		/// <summary>
		/// Creator of this permit (e.g. the person who invited x). Not to be confused with PermittedContentId, which is the invited person.
		/// </summary>
		public uint UserId;

		/// <summary>
		/// Usually the content type for "user" but can also be e.g. a company or something else that represents a "user".
		/// </summary>
		public int PermittedContentTypeId;

		/// <summary>
		/// The ID of the actual permitted content. Usually a user id.
		/// </summary>
		public uint PermittedContentId;

		/// <summary>
		/// The date this was accepted, if it's an accepted invite.
		/// If the parent type doesn't use invites, this is just always null.
		/// </summary>
		public DateTime? AcceptedUtc;
		
		/// <summary>
		/// The date this was rejected, if it's a rejected invite.
		/// If the parent type doesn't use invites, this is just always null.
		/// </summary>
		public DateTime? RejectedUtc;
		
		/// <summary>
		/// True if this is an accepted invite.
		/// </summary>
		public bool Accepted
		{
			get{
				return AcceptedUtc != null;
			}
		}
		
		/// <summary>
		/// True if it's a rejected invite.
		/// </summary>
		public bool Rejected
		{
			get{
				return RejectedUtc != null;
			}
		}
		
		/// <summary>
		/// True if this user is the creator of the parent content.
		/// </summary>
		public bool Creator
		{
			get{
				if(userContentTypeId == 0){
					userContentTypeId = ContentTypes.GetId(typeof(User));
				}
				
				return UserId == ContentId && ContentTypeId == userContentTypeId;
			}
		}
		
		
		private static int userContentTypeId;
	}
}
