using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Automations;
using System;
using Api.Startup;

namespace Api.PublishGroups
{
	/// <summary>
	/// Handles publishGroups.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PublishGroupService : AutoService<PublishGroup>
    {
		private PublishGroupContentService _groupContents;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PublishGroupService(PublishGroupContentService groupContents) : base(Events.PublishGroup)
        {
			_groupContents = groupContents;
			InstallAdminPages("Publish Groups","fa:fa-book-open", ["id", "name"]);

			Events.Automation("publisher", "0 * 0 ? * * *").AddEventListener(async (Context context, AutomationRunInfo run) => {

				if (run == null)
				{
					return run;
				}

				var pendingGroups = await Where("IsPublished=? and ReadyForPublishing=?", DataOptions.IgnorePermissions)
					.Bind(false)
					.Bind(true)
					.ListAll(context);

				foreach (var pendingGroup in pendingGroups)
				{
					var goLive = pendingGroup.AutoPublishTimeUtc;

					if (goLive == null)
					{
						continue;
					}

					if (goLive.Value > DateTime.UtcNow)
					{
						continue;
					}

					await Publish(context, pendingGroup);
				}

				return run;
			});

#if !DEBUG
			Cache();
#endif
		}

		/// <summary>
		/// Publishes the given group of draft content now.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		public async ValueTask Publish(Context context, PublishGroup group)
		{
			if (group.IsPublished)
			{
				// No-op - already published.
				return;
			}

			var result = await Update(context, group, (Context ctx, PublishGroup toUpdate, PublishGroup orig) => {

				toUpdate.IsPublished = true;

			}, DataOptions.IgnorePermissions | DataOptions.CheckNotChanged);

			if (result == null)
			{
				// Some other server beat us to it most likely!
				return;
			}

			// It's up to this server to publish it now.
			var contents = await _groupContents
				.Where("PublishGroupId=?", DataOptions.IgnorePermissions)
				.Bind(group.Id)
				.ListAll(context);

			foreach (var content in contents)
			{
				var type = content.ContentType;

				if (string.IsNullOrEmpty(type))
				{
					continue;
				}

				var contentService = Services.Get(type + "Service");

				if (contentService == null)
				{
					continue;
				}

				// Locate the draft with the specified content ID and publish it.
				var revisionService = contentService.GetRevisions();

				if (revisionService == null)
				{
					continue;
				}

				await revisionService.PublishGenericId(context, content.RevisionId);
			}

		}
	}
    
}
