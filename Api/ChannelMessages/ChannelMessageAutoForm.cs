using Api.AutoForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.ChannelMessages
{
	/// <summary>
	/// Used when creating or updating a message
	/// </summary>
	public partial class ChannelMessageAutoForm : AutoForm<ChannelMessage>
	{
		/// <summary>
		/// The channel the message is in.
		/// </summary>
		public int ChannelId;

		/// <summary>
		/// The canvas JSON of the message. If you just want raw text/ html, use {"content": "text or html here"}.
		/// It's a canvas so you can embed media/ have powerful formatting etc.
		/// </summary>
		public string BodyJson;
	}

}