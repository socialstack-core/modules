using Api.Automations;
using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.Startup;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Api.MediaShares
{
	/// <summary>
	/// Handles mediaShares.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class MediaShareService : AutoService<MediaShare>
    {
		private readonly PermalinkService _permalinks;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MediaShareService(PermalinkService permalinks) : base(Events.MediaShare)
        {
			_permalinks = permalinks;

			InstallAdminPages(
				new AdminPageOptions()
				{
					NavMenuLabel = new Api.Translate.Localized<string>("Media shares"),
					NavMenuIcon = "fa:fa-share-alt",
					ListFields = ["id", "slug", "expiryUtc", "downloadCount"]
				}
			);

			Events.MediaShare.BeforeCreate.AddEventListener((Context context, MediaShare share) =>
			{
				if (share == null)
				{
					return new ValueTask<MediaShare>(share);
				}

				if (string.IsNullOrEmpty(share.Slug))
				{
					share.Slug = GenerateSlug();
				}

				if (share.ExpiryUtc == null)
				{
					share.ExpiryUtc = DateTime.UtcNow.AddDays(7);
				}

				return new ValueTask<MediaShare>(share);
			});

			_permalinks.Generate("/shared-media/${content.Slug}", this);

			Events.Automation("media shares expire", "0 0 2 ? * * *", false, "Deletes media shares that have expired")
				.AddEventListener(async (Context context, AutomationRunInfo runInfo) =>
				{
					var expired = await Where("ExpiryUtc<?", DataOptions.NoCacheIgnorePermissions).Bind(DateTime.UtcNow).ListAll(context);

					if (expired != null)
					{
						foreach (var share in expired)
						{
							await Delete(context, share, DataOptions.IgnorePermissions);
						}
					}

					return runInfo;
				});
		}

		/// <summary>
		/// Increments the download counter of the given share. This is automatically called whenever a file or the zip is downloaded.
		/// </summary>
		public async ValueTask<MediaShare> CountDownload(Context context, MediaShare share)
		{
			if (share == null)
			{
				return null;
			}

			return await Update(context, share, (Context ctx, MediaShare toUpdate, MediaShare orig) =>
			{
				toUpdate.DownloadCount++;
			}, DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// True if the given share has expired.
		/// </summary>
		public static bool IsExpired(MediaShare share)
		{
			return share != null && share.ExpiryUtc.HasValue && share.ExpiryUtc.Value < DateTime.UtcNow;
		}

		private static string GenerateSlug()
		{
			const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var bytes = new byte[14];
			RandomNumberGenerator.Fill(bytes);
			var slug = new StringBuilder(bytes.Length);

			foreach (var b in bytes)
			{
				slug.Append(chars[b % chars.Length]);
			}

			return slug.ToString();
		}
	}
    
}