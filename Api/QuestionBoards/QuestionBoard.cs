using System;
using Api.Database;
using Api.Users;

namespace Api.QuestionBoards
{
	
	/// <summary>
	/// A particular question board. These contain lists of questions.
	/// </summary>
	public partial class QuestionBoard : RevisionRow
	{
		/// <summary>
		/// The name of the board in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Name;

		/// <summary>
		/// The page ID that questions will appear on.
		/// </summary>
		public int QuestionPageId;

		/// <summary>
		/// The page ID that the board appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string IconRef;
	}
	
}