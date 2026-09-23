using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;
using System;


namespace Api.Reviews
{

	/// <summary>
	/// A Review
	/// </summary>
	[ListAs("Reviews", Explicit = true, Module = "Admin/MultiReviewSelect")]
	public partial class Review : VersionedContent<uint>
	{
		/// <summary>
		/// The content of the review in markdown with basic formatting for bold etc.
		/// </summary>
		public string ReviewTextMarkdown;

		/// <summary>
		/// Their rating (0-1).
		/// </summary>
		public float Rating;

		/// <summary>
		/// Clients name
		/// </summary>
		public string ClientName;

		/// <summary>
		/// A title of the review. 
		/// If the name is present then it will be provided with their name. If not, then it will be provided as a standalone title.
		/// </summary>
		public string Title;
	}

}