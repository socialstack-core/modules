using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Huddles;
using Api.Permissions;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.UserAgendaEntries
{

	/// <summary>
	/// Listens out for the DatabaseDiff run to add additional revision tables, as well as BeforeUpdate events to then automatically create the revision rows.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			IUserAgendaEntryService agenda = null;
			IHuddleService huddles = null;
			var huddleContentTypeId = ContentTypes.GetId(typeof(Huddle));

			Events.Huddle.AfterUpdate.AddEventListener(async (Context context, Huddle huddle) =>
			{
				if (huddle == null)
				{
					return huddle;
				}

				// Foreach invite, if AgendaEntryId is non-zero, update the entry.
				if (huddle.Invites != null && huddle.Invites.Count > 0)
				{
					var ids = new List<int>();

					foreach (var invite in huddle.Invites)
					{
						if (invite.AgendaEntryId.HasValue && invite.AgendaEntryId != 0)
						{
							ids.Add(invite.AgendaEntryId.Value);
						}
					}

					if (ids.Count > 0)
					{
						// Get the entries:
						var agendaEntries = await agenda.List(context, new Filter<UserAgendaEntry>().Id(ids));

						// For each one, update start/ end:
						foreach (var entry in agendaEntries)
						{
							entry.StartUtc = huddle.StartTimeUtc;
							entry.EndUtc = huddle.EstimatedEndTimeUtc;
							await agenda.Update(context, entry);
						}
					}

				}

				return huddle;
			});

			Events.HuddlePermittedUser.BeforeCancel.AddEventListener(async (Context context, HuddlePermittedUser invite) =>
			{

				if (invite == null || !invite.AgendaEntryId.HasValue || invite.AgendaEntryId == 0)
				{
					return invite;
				}

				// Got a permittedUserId.
				// Remove it from their agenda if needed.
				if (agenda == null)
				{
					agenda = Services.Get<IUserAgendaEntryService>();
					huddles = Services.Get<IHuddleService>();
				}

				// Get agenda entry:
				var entry = await agenda.Get(
					context,
					invite.AgendaEntryId.Value
				);

				if (entry != null)
				{
					// Delete it:
					await agenda.Delete(context, entry.Id);
				}

				invite.AgendaEntryId = 0;
				return invite;
			});

			Events.HuddlePermittedUser.BeforeAccept.AddEventListener(async (Context context, HuddlePermittedUser invite) => {
				
				if(invite == null || invite.PermittedUserId == 0)
				{
					return invite;
				}
				
				// User accepted an invite.
				// Add it to their agenda.
				if(agenda == null)
				{
					agenda = Services.Get<IUserAgendaEntryService>();
					huddles = Services.Get<IHuddleService>();
				}

				var huddle = await huddles.Get(context, invite.HuddleId);

				if (huddle == null)
				{
					// Hmm!
					return invite;
				}

				// Create an agenda entry now:
				var entry = await agenda.Create(
					context,
					new UserAgendaEntry(){
						ContentTypeId = huddleContentTypeId,
						ContentId = invite.HuddleId,
						StartUtc = huddle.StartTimeUtc,
						EndUtc = huddle.EstimatedEndTimeUtc,
						CreatedUtc = DateTime.UtcNow,
						EditedUtc = DateTime.UtcNow,
						UserId = invite.PermittedUserId
					}
				);

				if (entry != null)
				{
					invite.AgendaEntryId = entry.Id;
				}

				return invite;
			}, 1);
		}
	}
}