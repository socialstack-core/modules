using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using System.IO;
using Microsoft.Extensions.Primitives;
using Api.Startup;
using System;
using System.Collections.Generic;


namespace Api.Uploader
{
	/// <summary>
	/// Info about the following line.
	/// </summary>
	public enum ManifestLineType
	{
		/// <summary>
		/// The main file itself.
		/// </summary>
		File,
		/// <summary>
		/// A particular video chunk.
		/// </summary>
		Chunk,
		/// <summary>
		/// A substream manifest.
		/// </summary>
		Manifest
	}

	/// <summary>
	/// An m3u8 parser and reconstruction system.
	/// </summary>
	public class VideoManifest
	{
		/// <summary>
		/// The header of the manifest. These are all the # lines which are before a chunk or stream.
		/// </summary>
		public ManifestInfo Metadata = new ManifestInfo();

		/// <summary>
		/// Sub manifest files, identified by EXT-X-STREAM-INF
		/// </summary>
		public List<ManifestInfo> Substreams;

		/// <summary>
		/// List of chunks in the manifest.
		/// </summary>
		public List<ManifestInfo> Chunks;

		/// <summary>
		/// True if the complete chunk list is known (and this file isn't expected to be streaming from source right now).
		/// </summary>
		public bool Complete;

		private DateTime StartTimeUtc;

		/// <summary>
		/// Creates a new manifest, parsing it from the given content file.
		/// </summary>
		/// <param name="fileContent"></param>
		/// <param name="absolutePath">The URL of this manifest file, minus its name.</param>
		/// <param name="startTimeUtc">If provided, Will adjust it into being a live manifest served by the API.</param>
		public VideoManifest(byte[] fileContent, string absolutePath = null, DateTime? startTimeUtc = null)
		{
			// Get the raw text:
			var manifestText = System.Text.Encoding.UTF8.GetString(fileContent);

			// Split by line:
			var lines = manifestText.Replace('\r', '\n').Split('\n');

			// Mode for what the read comments are currently targeting.
			var currentMode = ManifestLineType.File;

			if (startTimeUtc.HasValue)
			{
				StartTimeUtc = startTimeUtc.Value;
			}

			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i].Trim();

				if (string.IsNullOrEmpty(line))
				{
					continue;
				}

				if (line[0] == '#')
				{
					if (line.StartsWith("#EXT-X-ENDLIST"))
					{
						// Otherwise it's live at source right now.
						Complete = true;
					}
					else if (line.StartsWith("#EXT-X-PLAYLIST-TYPE"))
					{
						// Skip this. We'll always insert type:event into a live m3u8.
					}
					else if (line.StartsWith("#EXT-X-STREAM-INF"))
					{
						// The file that follows is another manifest.
						currentMode = ManifestLineType.Manifest;

						var targetInfo = GetLatestInfo(currentMode);
						targetInfo.AddMeta(line);
					}
					else if (line.StartsWith("#EXTINF"))
					{
						// The file that follows is a chunk.
						currentMode = ManifestLineType.Chunk;

						var targetInfo = GetLatestInfo(currentMode);
						targetInfo.AddMeta(line);
					}
					else
					{
						// This line is meta for whatever the current mode is.
						var targetInfo = GetLatestInfo(currentMode);
						targetInfo.AddMeta(line);
					}
				}
				else
				{
					// This is either a chunk or a substream.
					var mode = currentMode == ManifestLineType.File ? ManifestLineType.Chunk : currentMode;
					var targetInfo = GetLatestInfo(mode);

					// In chunk mode, we need the absolute chunk URL.
					targetInfo.OriginalUrl = line;
					
					if(absolutePath != null)
					{
						if (mode == ManifestLineType.Chunk)
						{
							line = absolutePath + line;
						}
						else
						{
							// In substream mode, the URL is the substream index.
							line = (Substreams.Count-1) + ".m3u8";
						}
					}
					
					targetInfo.Url = line;
				}
			}

			if (absolutePath != null && Chunks != null && Chunks.Count != 0)
			{
				// Insert live list type into its header.
				Metadata.AddMeta("#EXT-X-PLAYLIST-TYPE:EVENT");
				Metadata.AddMeta("#EXT-X-DISCONTINUITY");
			}

		}

		private byte[] _cachedBuffer;
		private double _lastBufferStreamTime;
		private double nextRequiredTime;
		private bool _finished;

		private void BuildBuffer(double currentStreamTime)
		{
			var ms = new MemoryStream();

			_lastBufferStreamTime = currentStreamTime;

			// File header lines:
			if (Metadata != null)
			{
				Metadata.WriteTo(ms);
			}

			if (Substreams != null && Substreams.Count > 0)
			{
				// This is a root manifest.
				// In this case, we don't write the substream urls exactly as they were, but we do match the meta.
				for (var i=0;i<Substreams.Count;i++)
				{
					var substream = Substreams[i];
					substream.WriteTo(ms);
				}
			}
			else
			{
				// Actual chunk list. The chunks that we output are impacted by the stream time.

				double totalStreamTime = 0;
				bool allChunks = true;

				for (var i = 0; i < Chunks.Count; i++)
				{
					if (totalStreamTime > currentStreamTime)
					{
						// This chunk is in the future.
						allChunks = false;
						nextRequiredTime = totalStreamTime;
						break;
					}

					var chunk = Chunks[i];

					totalStreamTime += chunk.LengthInSeconds;

					chunk.WriteTo(ms);
				}

				if (allChunks)
				{
					_finished = true;
					// ENDLIST declaration indicates the stream is over.
					ms.Write(System.Text.Encoding.UTF8.GetBytes("#EXT-X-ENDLIST\r\n"));
				}
			}

			_cachedBuffer = ms.ToArray();
		}

		/// <summary>
		/// Writes this manifest to the given stream, using a cached byte block as frequently as it can.
		/// </summary>
		/// <param name="outputStream"></param>
		/// <returns></returns>
		public async ValueTask WriteTo(Stream outputStream)
		{
			if (_cachedBuffer == null)
			{
				var currentStreamTime = (DateTime.UtcNow - StartTimeUtc).TotalSeconds;
				BuildBuffer(currentStreamTime);
			}

			if (Chunks != null && !_finished)
			{
				// The buffer might be stale.
				var currentStreamTime = (DateTime.UtcNow - StartTimeUtc).TotalSeconds;

				if (currentStreamTime >= nextRequiredTime)
				{
					// Now in the future of the prev last chunk. Rebuild the buffer.
					BuildBuffer(currentStreamTime);
				}
			}

			await outputStream.WriteAsync(_cachedBuffer);
		}

		private ManifestInfo GetLatestInfo(ManifestLineType currentLineType)
		{
			ManifestInfo info;

			switch (currentLineType)
			{
				case ManifestLineType.File:

					return Metadata;

				default:
				case ManifestLineType.Chunk:
					if (Chunks == null)
					{
						info = new ManifestInfo();
						Chunks = new List<ManifestInfo>();
						Chunks.Add(info);
						return info;
					}

					// Latest chunk:
					info = Chunks[Chunks.Count - 1];

					if (info.Url == null)
					{
						return info;
					}

					// New one is required as the info is considered to be closed when the URL is set.
					info = new ManifestInfo();
					Chunks.Add(info);
					return info;
				case ManifestLineType.Manifest:

					if (Substreams == null)
					{
						info = new ManifestInfo();
						Substreams = new List<ManifestInfo>();
						Substreams.Add(info);
						return info;
					}

					// Latest substream:
					info = Substreams[Substreams.Count - 1];

					if (info.Url == null)
					{
						return info;
					}
						
					// New one is required as the info is considered to be closed when the URL is set.
					info = new ManifestInfo();
					Substreams.Add(info);
					return info;
			}
		}
		
	}

	/// <summary>
	/// A particular chunk of a manifest.
	/// </summary>
	public class ManifestInfo
	{
		/// <summary>
		/// The raw # metadata lines.
		/// </summary>
		public List<string> MetaLines;

		/// <summary>
		/// Chunk runtime if EXTINF is present.
		/// </summary>
		public double LengthInSeconds;

		/// <summary>
		/// The chunk/ substream URL.
		/// </summary>
		public string Url;

		/// <summary>
		/// The chunk/ substream URL, as it was in the file.
		/// </summary>
		public string OriginalUrl;

		/// <summary>
		/// Adds a MetaLine including the hash and excluding the newline itself.
		/// </summary>
		/// <param name="line"></param>
		public void AddMeta(string line)
		{
			if (MetaLines == null)
			{
				MetaLines = new List<string>();
			}

			if (line.StartsWith("#EXTINF"))
			{
				var pieces = line.Split(':', 2);
				if (pieces.Length == 2)
				{
					var runtimeAndName = pieces[1].Split(',', 2);

					if (double.TryParse(runtimeAndName[0], out double lis))
					{
						LengthInSeconds = lis;
					}

				}
			}

			MetaLines.Add(line);
		}

		/// <summary>
		/// The meta as its raw byte array.
		/// </summary>
		private byte[] _metaAndUrl;


		/// <summary>
		/// Writes the meta for this info to the given stream.
		/// </summary>
		/// <param name="ms"></param>
		public void WriteTo(MemoryStream ms)
		{
			if (_metaAndUrl == null)
			{
				MemoryStream ms2 = new MemoryStream();

				if (MetaLines != null)
				{
					for (var i = 0; i < MetaLines.Count; i++)
					{
						var line = MetaLines[i];

						var lineBytes = System.Text.Encoding.UTF8.GetBytes(line + "\r\n");

						ms2.Write(lineBytes);
					}
				}

				if (Url != null)
				{
					ms2.Write(System.Text.Encoding.UTF8.GetBytes(Url + "\r\n"));
				}

				_metaAndUrl = ms2.ToArray();
			}

			ms.Write(_metaAndUrl);
		}
	}

}