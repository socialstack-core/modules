
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Startup;
using Api.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class AutoService<T, ID>
{
	/// <summary>
	/// Installs the given content builders. Thread-safe and deferred if services not yet started.
	/// Only works for services where T : InstallableContent.
	/// </summary>
	public void InstallContent(params Api.Users.ContentBuilder[] builders)
	{
		if (!(typeof(T) == typeof(Api.Users.InstallableContent<uint>) || typeof(Api.Users.InstallableContent<uint>).IsAssignableFrom(typeof(T))))
		{
			throw new InvalidOperationException("InstallContent can only be called on services where T : InstallableContent");
		}

		bool scheduleStart = false;

		var toInstall = new List<Api.Users.ContentBuilder>();
		var installLocker = new object();

		lock (installLocker)
		{
			toInstall.AddRange(builders);
			scheduleStart = true;
		}

		if (scheduleStart)
		{
			if (Services.Started)
			{
				Task.Run(async () =>
				{
					List<Api.Users.ContentBuilder> set;
					lock (installLocker)
					{
						set = toInstall;
					}
					await InstallContentInternal(new Context(), set);
				});
			}
			else
			{
				Events.Service.AfterStart.AddEventListener(async (Context ctx, object src) =>
				{
					List<Api.Users.ContentBuilder> set;
					lock (installLocker)
					{
						set = toInstall;
					}
					await InstallContentInternal(ctx, set);
					return src;
				});
			}
		}
	}

	/// <summary>
	/// Override this method to populate/ update content from a builder.
	/// </summary>
	protected virtual ValueTask PopulateContent(Context context, T content, Api.Users.ContentBuilder builder)
	{
		// Default implementation does nothing
		return new ValueTask();
	}

	/// <summary>
	/// Internal installation logic.
	/// </summary>
	private async ValueTask InstallContentInternal(Context context, List<Api.Users.ContentBuilder> builders)
	{
		if (builders == null || builders.Count == 0)
			return;

#if DEBUG
		var buildTime = DateTime.UtcNow;
#else
		var buildTime = new DateTime(Services.Get<Api.CanvasRenderer.FrontendCodeService>().Version);
#endif

		// Get existing content by Key
		var existingList = await this.Where("Key=[?]", DataOptions.NoCacheIgnorePermissions)
			.Bind(builders.Select(b => b.Key))
			.ListAll(context);

		var existingLookup = existingList.ToDictionary(c => (c as InstallableContent<ID>)?.Key);

		foreach (var builder in builders)
		{
			var key = builder.Key;

			// Dispatch OnInstall event - allows modification of builder
			await EventGroup.OnInstall.Dispatch(context, builder);

			if (existingLookup.TryGetValue(key, out var existing))
			{
				var installable = existing as InstallableContent<ID>;
				if (installable == null) continue;

				// Check if build time is current
				if (installable.LastInstallBuildTimeUtc >= buildTime)
					continue;

				// Check for admin edits via revisions
				if (installable.LastInstallRevisionId.HasValue)
				{
					var revs = await Revisions
						.Where("Id>=? and ContentId=?", DataOptions.IgnorePermissions)
						.Bind(installable.LastInstallRevisionId.Value)
						.Bind(ReverseId(installable.Id))
						.ListAll(context);

					int skipRevisions = revs.Count > 0 && ReverseId(revs[0].Id) == installable.LastInstallRevisionId.Value ? 2 : 1;
					if ((revs.Count - skipRevisions) > 0)
						continue; // Admin edited - don't overwrite
				}

				// Update existing
				await Update(context, existing, async (ctx, toUpdate, original) => {
					await PopulateContent(ctx, toUpdate, builder);
					var updateInstallable = toUpdate as InstallableContent<ID>;
					updateInstallable.LastInstallRevisionId = (uint)installable.Revision;
					updateInstallable.LastInstallBuildTimeUtc = buildTime;
				}, DataOptions.IgnorePermissions);
			}
			else
			{
				// Create new
				var newContent = (T)Activator.CreateInstance(InstanceType);
				var newInstallable = newContent as InstallableContent<ID>;
				if (newInstallable == null) continue;
				newInstallable.Key = builder.Key;
				await PopulateContent(context, newContent, builder);
				newInstallable.LastInstallRevisionId = null;
				newInstallable.LastInstallBuildTimeUtc = buildTime;
				await Create(context, newContent, DataOptions.IgnorePermissions);
			}
		}
	}
}
