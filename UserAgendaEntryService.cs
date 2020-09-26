using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.UserAgendaEntries
{
	/// <summary>
	/// Handles userAgendaEntries.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class UserAgendaEntryService : AutoService<UserAgendaEntry>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserAgendaEntryService() : base(Events.UserAgendaEntry)
        {
			Events.UserAgendaEntry.AfterLoad.AddEventListener(async (Context context, UserAgendaEntry agendaEntry) =>
			{
				if (agendaEntry == null)
				{
					return null;
				}

				if (agendaEntry.ContentId != 0)
				{
					// Get the content info:
					agendaEntry.Content = await Content.Get(context, agendaEntry.ContentTypeId, agendaEntry.ContentId);
				}

				return agendaEntry;
			}, 5);

			Events.UserAgendaEntry.BeforeUpdate.AddEventListener(async (Context context, UserAgendaEntry agendaEntry) =>
			{
				if (agendaEntry == null)
				{
					return null;
				}

				if (agendaEntry.ContentId != 0)
				{
					// Get the content info (throws if permission fails):
					agendaEntry.Content = await Content.Get(context, agendaEntry.ContentTypeId, agendaEntry.ContentId, true);
				}

				return agendaEntry;
			}, 5);
			
			Events.UserAgendaEntry.BeforeCreate.AddEventListener(async (Context context, UserAgendaEntry agendaEntry) =>
			{
				if (agendaEntry == null)
				{
					return null;
				}

				if (agendaEntry.ContentId != 0)
				{
					// Get the content info (throws if permission fails):
					agendaEntry.Content = await Content.Get(context, agendaEntry.ContentTypeId, agendaEntry.ContentId, true);
				}

				return agendaEntry;
			}, 5);

			Events.UserAgendaEntry.AfterList.AddEventListener(async (Context context, List<UserAgendaEntry> list) =>
			{
				if(list == null)
				{
					return list;
				}
				
				// Can be mixed content so we'll use the Content.ApplyMixed helper:
				await Content.ApplyMixed(
					context,
					list,
					src => {
						// Never invoked with null.
						var uae=(UserAgendaEntry)src;
						return new ContentTypeAndId(uae.ContentTypeId, uae.ContentId);
					},
					(object src, object content) => {
						var uae=(UserAgendaEntry)src;
						uae.Content = content;
					}
				);
				
				return list;
			}, 5);
			
		}
	}
    
}
