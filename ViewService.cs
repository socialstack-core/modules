using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Views
{
	/// <summary>
	/// Handles views
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ViewService : AutoService<View>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ViewService() : base(Events.View)
        {
		}

		/// <summary>
		/// Marks a content item of the given type as viewed.
		/// </summary>
		public async Task MarkViewed(Context context, int contentTypeId, uint id)
		{
			
			// Get the view state for this user:
			var viewEntries = await List(context, 
				new Filter<View>().Equals("Id", id).And().Equals("ContentTypeId", contentTypeId).And().Equals("UserId", context.UserId)
			);

			var viewEntry = viewEntries == null || viewEntries.Count == 0 ? null : viewEntries[0];

			if(viewEntry == null){
				// New viewer! Insert and increase view total by 1.
				
				// Use the regular insert so we can add events to the viewed state:
				viewEntry = new View();
				viewEntry.UserId = context.UserId;
				viewEntry.ContentId = id;
				viewEntry.ContentTypeId = contentTypeId;
				
				await Create(context, viewEntry);
				
			}else{
				// Update viewed time:
				await Update(context, viewEntry, (Context c, View v) => {
					// Only updating time
				});
			}
			
		}
	}

}
