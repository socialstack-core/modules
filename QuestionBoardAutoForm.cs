using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.QuestionBoards
{
    /// <summary>
    /// Used when creating or updating a question board
    /// </summary>
    public partial class QuestionBoardAutoForm : AutoForm<QuestionBoard>
	{
		/// <summary>
		/// The name of the question board, in the site default language.
		/// </summary>
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
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string IconRef;
	}
}
