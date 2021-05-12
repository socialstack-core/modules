using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Huddles;
using Api.Permissions;
using Api.Startup;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Api.Configuration;


namespace Api.UserAgendaEntries
{

	/// <summary>
	/// Listens out for the DatabaseDiff run to add additional revision tables, as well as BeforeUpdate events to then automatically create the revision rows.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		private AddMeetingsToAgendaConfig Config = null;
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			UserAgendaEntryService agenda = null;
			HuddleService huddles = null;
			var huddleContentTypeId = ContentTypes.GetId(typeof(Huddle));
			//AddMeetingsToAgendaConfig Config = null;

			Events.Huddle.BeforeDelete.AddEventListener(async (Context context, Huddle huddle) =>
            {
                if (huddle == null)
                {
                    return huddle;
                }

                // Update the user agenda if needed.
                if (agenda == null)
                {
                    agenda = Services.Get<UserAgendaEntryService>();
                    huddles = Services.Get<HuddleService>();
                }

				var agendaEntries = await agenda.Where("ContentTypeId=? and ContentId=?", DataOptions.IgnorePermissions).Bind(ContentTypes.GetId("Huddle")).Bind(huddle.Id).ListAll(context);

                // For each one, remove the agendaEntries
                foreach (var entry in agendaEntries)
                {
                    await agenda.Delete(context, entry.Id);
                }

                return huddle;
            }, 20);



			Events.Huddle.AfterUpdate.AddEventListener(async (Context context, Huddle huddle) =>
			{
				if (huddle == null)
				{
					return huddle;
				}

                // Update the user agenda if needed.
                if (agenda == null)
                {
                    agenda = Services.Get<UserAgendaEntryService>();
                    huddles = Services.Get<HuddleService>();
                }

				// For each invite, if AgendaEntryId is non-zero, update the entry.
				/* 
				 * TODO: we need to use the new way of view 
				 * 
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

				}*/

				return huddle;
			}, 20);



			/*
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
					agenda = Services.Get<UserAgendaEntryService>();
					huddles = Services.Get<HuddleService>();
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
			}, 20);

			Events.HuddlePermittedUser.BeforeDelete.AddEventListener(async (Context context, HuddlePermittedUser invite) =>
			{
				if (invite == null || !invite.AgendaEntryId.HasValue || invite.AgendaEntryId == 0)
				{
					return invite;
				}

				// Got a permittedUserId.
				// Remove it from their agenda if needed.
				if (agenda == null)
				{
					agenda = Services.Get<UserAgendaEntryService>();
					huddles = Services.Get<HuddleService>();
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


			Events.HuddlePermittedUser.BeforeUpdate.AddEventListener(async (Context context, HuddlePermittedUser invite) =>
			{

				if (invite == null || !invite.AgendaEntryId.HasValue || invite.AgendaEntryId == 0 || invite.PermittedUserId != 0)
				{
					return invite;
				}

				// Has an agenda entry ID but no permitted user.
				// Remove it from their agenda if needed.
				if (agenda == null)
				{
					agenda = Services.Get<UserAgendaEntryService>();
					huddles = Services.Get<HuddleService>();
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
			}, 20);
			*//*
			Events.HuddlePermittedUser.BeforeAccept.AddEventListener(async (Context context, HuddlePermittedUser invite) => {
				
				if(invite == null || invite.PermittedUserId == 0)
				{
					return invite;
				}
				
				// User accepted an invite.
				// Add it to their agenda.
				if(agenda == null)
				{
					agenda = Services.Get<UserAgendaEntryService>();
					huddles = Services.Get<HuddleService>();
				}

				var huddle = await huddles.Get(context, invite.HuddleId);

				if (huddle == null)
				{
					// Hmm!
					return invite;
				}

				// Collision check - does this user have something else on their agenda at this time?
				/*
				* Omit anything that ended before (or equal to) my start
				* Omit anything that started after (or equal to) my end
				* Anything else overlaps
				!(ENTRY.EndUtc <= HUDDLE.StartUtc || ENTRY.StartUtc >= HUDDLE.EndUtc)
				
				Simplifies to:
				
				ENTRY.EndUtc > HUDDLE.StartUtc && ENTRY.StartUtc < HUDDLE.EndUtc
				*//*
				// Before moving on, check the config policy on collisions. 
				if (Config == null)
                {
					var section = AppSettings.GetSection("AddMeetingsToAgenda");

					if (section != null)
					{
						Config = section.Get<AddMeetingsToAgendaConfig>();
					}
				}


				// Config is still null, or if AllowCollisions is false, run the check OR if AllowCollisions is true and forceAccept = false.

				if (Config == null || !Config.AllowCollisions || (Config != null && Config.AllowCollisions && !invite.ForceAccept))
				{
					var overlappers = await agenda.List(
						context,
						new Filter<UserAgendaEntry>()
							.Equals("UserId", invite.PermittedUserId)
							.And()
							.GreaterThan("EndUtc", huddle.StartTimeUtc)
							.And()
							.LessThan("StartUtc", huddle.EstimatedEndTimeUtc)
					);

					if (overlappers != null && overlappers.Count > 0)
					{
						// Are we returning a warning or error:
						if(Config != null && Config.AllowCollisions && !invite.ForceAccept)
                        {
							// We are returning a warning since the Allow collisions is enabled but we are not forcing.
							//throw new PublicException("You're already booked at this time. Please check your calendar and propose a new time", "double_booked_warning");
						}
						else
                        {
							// User has something on the agenda that overlaps.
							// Reject this request.
							throw new PublicException("You're already booked at this time. Please check your calendar and propose a new time", "double_booked");
						}
						
					}
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
			}, 1);*/
		}
	}
}