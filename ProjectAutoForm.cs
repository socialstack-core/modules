using Newtonsoft.Json;
using Api.AutoForms;
using System;

namespace Api.Projects
{
    /// <summary>
    /// Used when creating or updating an project
    /// </summary>
    public partial class ProjectAutoForm : AutoForm<Project>
    {
		/// <summary>
		/// The name of the project in the site default language.
		/// </summary>
		public string Name;

		/// <summary>
		/// A short description of the project.
		/// </summary>
		public string Description;

		/// <summary>
		/// The primary ID of the page that this project appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The content of this project.
		/// </summary>
		public string BodyJson;

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
