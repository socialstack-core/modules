using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Uploader;
using Api.Startup;
using Api.CanvasRenderer;
using Api.Translate;
using System;
using System.IO;

namespace Api.VideoSubtitles
{
	/// <summary>
	/// Handles videoSubtitles.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class VideoSubtitleService : AutoService<VideoSubtitle>
    {
		private UploadService _uploads;
		private LocaleService _locales;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public VideoSubtitleService(UploadService uploads, LocaleService locales) : base(Events.VideoSubtitle)
        {
			InstallAdminPages("Video Subtitles", "fa:fa-closed-captioning", new string[] { "id", "name" });
			_uploads = uploads;
			_locales = locales;

			Events.Upload.AfterChunksUploaded.AddEventListener(async (Context context, Upload upload) => {

				if (upload == null)
				{
					return null;
				}

				// A video's chunks have been sent by the transcoder and put into storage.
				await UpdateSubtitles(context, upload);
				return upload;
			});
			
			Events.VideoSubtitle.BeforeCreate.AddEventListener(async (Context context, VideoSubtitle subtitle) => {
				
				if(subtitle == null)
				{
					return null;
				}
				
				await OwnerCheck(context, subtitle);
				
				return subtitle;
			});
			
			Events.VideoSubtitle.BeforeUpdate.AddEventListener(async (Context context, VideoSubtitle subtitle) => {
				
				if(subtitle == null)
				{
					return null;
				}
				
				await OwnerCheck(context, subtitle);
				
				return subtitle;
			});

			Events.VideoSubtitle.AfterCreate.AddEventListener(async (Context context, VideoSubtitle subtitle) => {

				if (subtitle == null)
				{
					return null;
				}

				var upload = await uploads.Get(context, subtitle.UploadId);

				await UpdateSubtitles(context, upload);

				return subtitle;
			});

			Events.VideoSubtitle.AfterUpdate.AddEventListener(async (Context context, VideoSubtitle subtitle) => {

				if (subtitle == null)
				{
					return null;
				}

				var upload = await uploads.Get(context, subtitle.UploadId);

				await UpdateSubtitles(context, upload);

				return subtitle;
			});

			Events.VideoSubtitle.AfterDelete.AddEventListener(async (Context context, VideoSubtitle subtitle) => {

				if (subtitle == null)
				{
					return null;
				}

				var upload = await uploads.Get(context, subtitle.UploadId);

				await UpdateSubtitles(context, upload);

				return subtitle;
			});

		}

		private async ValueTask UpdateSubtitles(Context context, Upload upload)
		{
			// Check if there are any subtitles to add to it.
			if (upload == null)
			{
				return;
			}

			var subtitleSet = await Where("UploadId=?", DataOptions.IgnorePermissions).Bind(upload.Id).ListAll(context);

			if (subtitleSet.Count == 0)
			{
				return;
			}

			// Read the manifest:
			byte[] manifestBytes;

			try
			{
				manifestBytes = await _uploads.ReadFile(upload, "chunks/manifest.m3u8");

				if (manifestBytes == null || manifestBytes.Length == 0)
				{
					// Doesn't exist yet.
					return;
				}
			}catch(Exception e)
			{
				Console.WriteLine("Ignored ReadFile exception when trying to get manifest for an upload: " + e.ToString());
				// Doesn't exist yet
				return;
			}

			var manifest = new VideoManifest(manifestBytes);

			// Remove existing subtitle refs inside it:
			if (manifest.Metadata.MetaLines != null)
			{
				var metaLines = manifest.Metadata.MetaLines;

				for (var i = metaLines.Count - 1; i >= 0; i--)
				{

					var metaLine = metaLines[i];

					if (metaLine.StartsWith("#EXT-X-MEDIA:TYPE=SUBTITLES"))
					{
						metaLines.RemoveAt(i);
					}
				}
			}

			if (manifest.Substreams != null)
			{
				// Add SUBTITLES="subs" to each stream
				foreach (var substream in manifest.Substreams)
				{
					var metaLines = substream.MetaLines;
					if (metaLines != null)
					{
						for (var i = metaLines.Count - 1; i >= 0; i--)
						{
							var metaLine = metaLines[i];

							if (metaLine.StartsWith("#EXT-X-STREAM-INF"))
							{
								if (metaLine.Contains("SUBTITLES"))
								{
									var pieces = metaLine.Split(',');
									var newLine = "";

									for (var o = 0; o < pieces.Length; o++)
									{
										if (pieces[o].Trim().StartsWith("SUBTITLES="))
										{
											// Omit this whole piece.
										}
										else
										{
											if (newLine.Length > 0)
											{
												newLine += ',';
											}
											newLine += pieces[o];
										}
									}

									metaLines[i] = newLine + ",SUBTITLES=\"subs\"";
								}
							}
						}
					}
				}
			}

			var tempPlayListFile = System.IO.Path.GetTempFileName();

			foreach (var subtitle in subtitleSet)
			{

				// Get the locale:
				var locale = await _locales.Get(context, subtitle.LocaleId);

				if (locale == null)
				{
					System.Console.WriteLine("Ignored a subtitle because locale '" + subtitle.LocaleId + "' does not exist.");
					continue;
				}

				// Need to create a subtitle playlist file which follows the same pattern:
				var fileRefInfo = FileRef.Parse(subtitle.SubtitleFileRef);

				// Content relative subtitle file:
				var contentRelativeSubtitleFile = fileRefInfo.GetRelativePath("original");

				// Assuming vtt for now.
				var subtitleFileUrl = (fileRefInfo.Scheme == "private" ? "/content-private/" : "/content/") + contentRelativeSubtitleFile;

				var subtitleFileContent = await _uploads.ReadFile(fileRefInfo, "original");

				var sttLength = GetSubtitleLength(subtitleFileContent);

				// Subtitle playlist content is therefore:
				var subtitlePlaylistVOD = "#EXTM3U\r\n#EXT-X-VERSION:3\r\n#EXT-X-TARGETDURATION:" + sttLength + "\r\n#EXT-X-MEDIA-SEQUENCE:0\r\n#EXT-X-PLAYLIST-TYPE:VOD\r\n#EXTINF:" + sttLength + ".0000,\r\n" + subtitleFileUrl + "\r\n#EXT-X-ENDLIST";

				System.IO.File.WriteAllText(tempPlayListFile, subtitlePlaylistVOD);

				var isDefault = (locale.Id == 1) ? "YES" : "NO";

				manifest.Metadata.AddMeta("#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"" + locale.Name + "\",DEFAULT=" + isDefault + ",AUTOSELECT=" + isDefault + ",FORCED=NO,LANGUAGE=\"" + locale.Code + "\",URI=\"sub_" + subtitle.Id + ".m3u8\"");

				// Store it in the upload as well:
				await Events.Upload.StoreFile.Dispatch(context, upload, tempPlayListFile, "chunks/sub_" + subtitle.Id + ".m3u8");

				// Subtitle playlist content is therefore:
				var subtitlePlaylistEVT = "#EXTM3U\r\n#EXT-X-VERSION:3\r\n#EXT-X-TARGETDURATION:" + sttLength + "\r\n#EXT-X-MEDIA-SEQUENCE:0\r\n#EXT-X-PLAYLIST-TYPE:EVENT\r\n#EXT-X-DISCONTINUITY\r\n#EXTINF:" + sttLength + ".0000,\r\n" + subtitleFileUrl + "\r\n#EXT-X-ENDLIST";

				System.IO.File.WriteAllText(tempPlayListFile, subtitlePlaylistEVT);

				// Store it in the upload as well:
				await Events.Upload.StoreFile.Dispatch(context, upload, tempPlayListFile, "chunks/sub_" + subtitle.Id + "_evt.m3u8");

			}

			// Next, update the manifest.
			var ms = new MemoryStream();
			manifest.WriteTo(ms);

			var newManifestBytes = ms.ToArray();

			// Write the original manifest:
			System.IO.File.WriteAllBytes(tempPlayListFile, manifestBytes);
			await Events.Upload.StoreFile.Dispatch(context, upload, tempPlayListFile, "chunks/manifest_src.m3u8");

			// And the updated one:
			System.IO.File.WriteAllBytes(tempPlayListFile, newManifestBytes);
			await Events.Upload.StoreFile.Dispatch(context, upload, tempPlayListFile, "chunks/manifest.m3u8");
			
		}

		private int GetSubtitleLength(byte[] fileContent)
		{
			if (fileContent == null || fileContent.Length == 0)
			{
				return 0;
			}

			var fileStr = System.Text.Encoding.UTF8.GetString(fileContent);

			var lines = fileStr.Replace('\r', '\n').Split('\n');

			for (var i = lines.Length - 1; i >= 0; i--)
			{
				var line = lines[i];

				if (line.Contains("-->"))
				{
					var pieces = line.Split("-->", 2);
					var ending = pieces[1].Trim().Split(' ');

					// The following is a time in the 00:00:00.000
					var endTime = ending[0];

					return (int)Math.Ceiling(TimeSpan.Parse(endTime).TotalSeconds);
				}
			}

			return 0;
		}

		private async ValueTask OwnerCheck(Context context, VideoSubtitle subtitle)
		{
			
			if(subtitle.UploadId == 0)
			{
				return;
			}
			
			var upload = await _uploads.Get(context, subtitle.UploadId);
			
			if(upload == null)
			{
				// It must exist.
				throw new PublicException("Unable to add subtitles on an upload that does not exist.", "upload_required");
			}
			
			// Must be the owner of the upload, or an admin.
			if(upload.UserId != context.UserId && (context.Role == null || !context.Role.CanViewAdmin))
			{
				// Acts like it doesn't exist.
				throw new PublicException("Unable to add subtitles on an upload that does not exist.", "upload_required");
			}
		}
	}
    
}
