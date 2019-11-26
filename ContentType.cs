using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.AvailableEndpoints
{
	/// <summary>
	/// A particular type of content available through this API.
	/// </summary>
	public class ContentType
	{
		/// <summary>
		/// The ID of the content type.
		/// There's a fixed function to generate these IDs - you don't actually need to hit the API to establish what the ID is.
		/// See also: Api.Database.ContentTypes.GetId
		/// </summary>
		public int Id;
		/// <summary>
		/// The name of the content type as-is, e.g. "ForumReply".
		/// </summary>
		public string Name;
	}
}
