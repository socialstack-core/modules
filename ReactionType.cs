using System;
using Api.Database;
using Api.Users;


namespace Api.Reactions
{
	/// <summary>
	/// Admin defined reaction types. These can be likes, dislikes, love hearts etc.
	/// </summary>
	public partial class ReactionType : VersionedContent<int>
	{
		/// <summary>
		/// The name of the forum in the site default language.
		/// </summary>
		[DatabaseField(Length = 40)]
		public string Name;

		/// <summary>
		/// A consistent key for this reaction type. Usually the lowercase and underscores instead of spaces variant of the first name.
		/// Should not change once set.
		/// I.e. "Happy face" (Name) => "happy_face" (Key), "Like" => "like" etc.
		/// </summary>
		[DatabaseField(Length = 40)]
		public string Key;

		/// <summary>
		/// The icon ref for reactions of this type. Often Emoji charcodes (emoji:1f600) or fontawesome refs.
		/// </summary>
		[DatabaseField(Length = 40)]
		public string IconRef;

		/// <summary>
		/// When reactions are given a non-zero group ID, they can only be used if a user has not reacted with some other reaction from the same group.
		/// For example, they cannot both upvote and downvote something.
		/// </summary>
		public int GroupId;
	}

}