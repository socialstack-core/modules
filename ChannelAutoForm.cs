using Api.AutoForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.Channels
{
	/// <summary>
	/// Used when creating or updating a channel
	/// </summary>
	public partial class ChannelAutoForm : AutoForm<Channel>
	{
		/// <summary>
		/// The name of this channel.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// The page that this channel is viewed on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string IconRef;
	}

}