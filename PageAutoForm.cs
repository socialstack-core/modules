using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Pages
{
    /// <summary>
    /// Used when creating or updating a page
    /// </summary>
    public partial class PageAutoForm : AutoForm<Page>
    {
		/// <summary>
		/// Page URL. Can contain substitutions such as :id.
		/// </summary>
		public string Url;

		/// <summary>
		/// Page canvas JSON. The raw structure of the page. Will often directly inherit some universal canvas.
		/// </summary>
		public string BodyJson;
	}
}
