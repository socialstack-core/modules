using System;
using Api.Database;
using Api.Startup;

namespace Api.Reactions
{
	/// <summary>
	/// The total number of Reactions on a particular piece of content.
	/// This is essentially just a more compact version of the Reaction table 
	/// (and its data can be directly derived from the Reaction table).
	/// Used to get e.g. x upvotes, y downvotes.
	/// </summary>
	[ListAs("Reactions")]
	[HasVirtualField("ReactionType", typeof(ReactionType), "ReactionTypeId")]
	public partial class ReactionCount : MappingEntity
	{
		/// <summary>
		/// The type of reaction (like, dislike etc - they can be custom defined).
		/// </summary>
		public uint ReactionTypeId;
		/// <summary>
		/// The number of reactions of this type on the given content.
		/// </summary>
		public int Total;
	}
	
}