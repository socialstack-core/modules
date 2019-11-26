using System;
using Api.Database;
using Api.Users;

namespace Api.Reactions
{
	/// <summary>
	/// A reaction by a particular user to a particular piece of content.
	/// ReactionCount is essentially just a counted version of these.
	/// </summary>
	public partial class Reaction : RevisionRow
	{
		/// <summary>
		/// The content type this is a reaction to.
		/// </summary>
		public int ContentTypeId;
		/// <summary>
		/// The Id of the content that this is a reaction to.
		/// </summary>
		public int ContentId;
		/// <summary>
		/// The type of reaction (like, dislike etc - they can be custom defined).
		/// </summary>
		public int ReactionTypeId;
	}
	
}