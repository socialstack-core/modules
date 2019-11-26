using Api.AutoForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.StoryAttachments
{
	/// <summary>
	/// Used when creating or updating a story attachment
	/// </summary>
	public partial class StoryAttachmentAutoForm : AutoForm<StoryAttachment>
	{
		/// <summary>
		/// The original content type ID, if there is one.
		/// </summary>
		public int? ContentTypeId;
		
		/// <summary>
		/// The content ID of this story, if there is one.
		/// </summary>
		public int? ContentId;
		
		/// <summary>
		/// Target URL where this attachment can be found. Can be a ref such as page:1 or module:ComponentName
		/// </summary>
		public string Url;
		
		/// <summary>
		/// Ref for image content in this attachment.
		/// </summary>
		public string FeatureRef;
		
		/// <summary>
		/// A title for this story attachment.
		/// </summary>
		public string Title;
		
		/// <summary>
		/// A small piece of text which describes what this attachment is.
		/// </summary>
		public string Excerpt;
		
	}

}