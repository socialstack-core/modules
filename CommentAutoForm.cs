using Api.AutoForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.Comments
{
	/// <summary>
	/// Used when creating or updating a comment
	/// </summary>
	public partial class CommentAutoForm : AutoForm<Comment>
	{
		/// <summary>
		/// The ID of the content that this comment is on.
		/// </summary>
		public int ContentId;

		/// <summary>
		/// E.g. "forum" or "comment" (if in reply to another comment). It's just the type name lower cased.
		/// </summary>
		public id ContentTypeId;

		/// <summary>
		/// The comment itself as a JSON canvas. If you just want html/ raw text, use {"content": "The text here"}.
		/// It's a full canvas JSON object so comments can potentially use the full power of embedded media etc.
		/// </summary>
		public string BodyJson;

	}

}