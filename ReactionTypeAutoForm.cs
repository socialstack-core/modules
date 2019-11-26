using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Reactions
{
    /// <summary>
    /// Used when creating or updating a reaction
    /// </summary>
    public partial class ReactionTypeAutoForm : AutoForm<ReactionType>
	{
		/// <summary>
		/// Friendly name of this reaction type.
		/// </summary>
		public string Name;

		/// <summary>
		/// The internal key for this reaction type. It should never change and is usually the lowercased with underscores version of the name.
		/// </summary>
		public string Key;

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
