using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Api.StoryAttachments
{
	/// <summary>
	/// Handles story attachments. These attachments are e.g. images attached to a feed story or a message in a chat channel.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class StoryAttachmentService : AutoService<StoryAttachment>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public StoryAttachmentService() : base(Events.StoryAttachment)
        {
			
			// Because of IHaveStoryAttachments, must be nestable:
			MakeNestable();
			
			// Define the IHaveStoryAttachments handler:
			DefineIHaveArrayHandler<IHaveStoryAttachments, StoryAttachment>(
				(IHaveStoryAttachments content, List<StoryAttachment> results) =>
				{
					content.Attachments = results;
				}
			);


		}

    }
    
}
