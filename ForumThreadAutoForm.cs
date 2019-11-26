using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.AutoForms;


namespace Api.ForumThreads
{
    /// <summary>
    /// Used when creating or updating a thread
    /// </summary>
    public partial class ForumThreadAutoForm : AutoForm<ForumThread>
	{
		/// <summary>
		/// The ID of the forum that the thread will be in.
		/// </summary>
		public int ForumId;

		/// <summary>
		/// The title of the thread.
		/// </summary>
		public string Title;
		
		/// <summary>
		/// The canvas JSON for the thread. If you just want raw text/ html, use {"content": "The text/ html here"}.
		/// It's a full canvas so the thread can support embedded media and powerful formatting.
		/// </summary>
		public string BodyJson;
    }
}
