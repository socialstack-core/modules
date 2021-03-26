using System;
using Api.Database;


namespace Api.Reactions
{
	/// <summary>
	/// The total number of Reactions on a particular piece of content.
	/// This is essentially just a more compact version of the Reaction table 
	/// (and its data can be directly derived from the Reaction table).
	/// Used to get e.g. x upvotes, y downvotes.
	/// </summary>
	public partial class ReactionCount : MappingEntity
	{
		/// <summary>
		/// The type of reaction (like, dislike etc - they can be custom defined).
		/// </summary>
		public int ReactionTypeId;
		/// <summary>
		/// The number of reactions of this type on the given content.
		/// </summary>
		public int Total;
		/// <summary>
		/// The reaction type that this is a count of. Set by demand.
		/// </summary>
		public ReactionType ReactionType { get; set; }
	}
	
}