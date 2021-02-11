using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Comments
{
	/// <summary>
	/// Handles commentSets.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CommentSetService : AutoService<CommentSet>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CommentSetService() : base(Events.CommentSet)
        {
			
			// A comment set is a list of comments for a particular piece of content.
			// They exist to track things like total number of comments on that piece of content.
		}
	}
    
}
