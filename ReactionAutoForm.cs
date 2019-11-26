using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Reactions
{
    /// <summary>
    /// Used when creating or updating a reaction
    /// </summary>
    public partial class ReactionAutoForm : AutoForm<Reaction>
	{
		/// <summary>
		/// The ID of the content that the user is reacting to.
		/// </summary>
		public int ContentId;

		/// <summary>
		/// E.g. "forum" or "comment". It's just the type name lower cased.
		/// </summary>
		public string ContentType;

		/// <summary>
		/// The Id of the reaction type. Used to identify if it's an upvote/ downvote/ like/ heart etc.
		/// </summary>
		public int ReactionTypeId;

	}
}
