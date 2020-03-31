using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Faqs
{
    /// <summary>
    /// Used when creating or updating an article
    /// </summary>
    public partial class FaqAutoForm : AutoForm<Faq>
    {
		/// <summary>
		/// The question being asked in the site default language.
		/// </summary>
		public string Question;

		/// <summary>
		/// The primary ID of the page that the faq appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The description of the faq
		/// </summary>
		public string AnswerJson;

		/// <summary>
		/// The priority level of the FAQ. The higher the value, the higher 
		/// </summary>
		public int Priority;
	}
}
