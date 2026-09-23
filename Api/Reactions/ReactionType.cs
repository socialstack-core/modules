using System;
using Api.Database;
using Api.Users;


namespace Api.Reactions
{
	/// <summary>
	/// Admin defined reaction types. These can be likes, dislikes, love hearts etc.
	/// </summary>
	public partial class ReactionType : InstallableContent<uint>
	{
		/// <summary>
		/// The name of the forum in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// The icon ref for reactions of this type. Often Emoji charcodes (emoji:1f600) or fontawesome refs.
		/// </summary>
		public string IconRef;
		
		/// <summary>
		/// When reactions are given a non-zero group ID, they can only be used if a user has not reacted with some other reaction from the same group.
		/// For example, they cannot both upvote and downvote something.
		/// </summary>
		public int GroupId;
	}

}