using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.AutoForms;


namespace Api.ForumReplies
{
    /// <summary>
    /// Used when creating or updating a reply
    /// </summary>
    public partial class ForumReplyAutoForm : AutoForm<ForumReply>
    {
		/// <summary>
		/// The thread the reply is in.
		/// </summary>
        public int ThreadId;
		
		/// <summary>
		/// The canvas JSON of the reply. If you just want raw text/ html, use {"content": "text or html here"}.
		/// It's a canvas so you can embed media/ have powerful formatting etc.
		/// </summary>
		public string BodyJson;
    }
}
