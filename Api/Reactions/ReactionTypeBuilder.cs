using Api.Users;

namespace Api.Reactions;


/// <summary>
/// Used to auto install reaction types
/// </summary>
public class ReactionTypeBuilder : ContentBuilder
{
	/// <summary>
	/// The name of the reaction type.
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