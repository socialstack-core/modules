using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;

namespace Api.ContentSync
{
	/// <summary>
	/// Handles an endpoint which describes the permissions on each role.
	/// </summary>

	[Route("v1/contentsync")]
	[ApiController]
	public partial class ContentSyncController : ControllerBase
	{
		private ContentSyncService _contentSyncService;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ContentSyncController(
			ContentSyncService contentSyncService
		)
		{
			_contentSyncService = contentSyncService;
		}
		
		/// <summary>
		/// Gets a fileset within a particular folder in the public content directory.
		/// </summary>
		/// <param name="subfolder"></param>
		/// <returns></returns>
		[HttpGet("fileset")]
		public async ValueTask<List<ContentFileInfo>> FileSet(string subfolder = null)
		{
			var ctx = await Request.GetContext();

			if (ctx.Role == null || !ctx.Role.CanViewAdmin)
			{
				throw new PublicException("Must be an administrative user to do this.", "admin_only");
			}
			
			if(subfolder != null && subfolder.IndexOf('.') != -1)
			{
				throw new PublicException("Invalid subfolder - it must not contain '.'", "invalid_subfolder");
			}
			
			return _contentSyncService.GetLocalFiles(subfolder);
		}

		/// <summary>
		/// Gets a fileset within a particular folder in the public content directory.
		/// </summary>
		/// <returns></returns>
		[HttpGet("videosync")]
		public async ValueTask<bool> VideoSync([FromQuery] int videoId, int firstChunk, int lastChunkId)
		{
			var result = await _contentSyncService.VideoSync(videoId, firstChunk, lastChunkId);
			return result;
		}
		
		/// <summary>
		/// Gets a fileset within a particular folder in the public content directory.
		/// </summary>
		/// <param name="subfolder"></param>
		/// <returns></returns>
		[HttpGet("diff")]
		public async Task<ContentFileDiffSet> Diff(string subfolder = null)
		{
			var ctx = await Request.GetContext();

			if (ctx.Role == null || !ctx.Role.CanViewAdmin)
			{
				throw new PublicException("Must be an administrative user to do this.", "admin_only");
			}

			if (subfolder != null && subfolder.IndexOf('.') != -1)
			{
				throw new PublicException("Invalid subfolder - it must not contain '.'", "invalid_subfolder");
			}

			return await _contentSyncService.Diff(subfolder);
		}

		/// <summary>
		/// Performs a sync with the configured upstream host.
		/// </summary>
		/// <param name="subfolder"></param>
		/// <returns></returns>
		[HttpGet("files")]
		public async Task<SyncStats> ContentFiles(string subfolder = null)
		{
			var ctx = await Request.GetContext();

			if (ctx.Role == null || !ctx.Role.CanViewAdmin)
			{
				throw new PublicException("Must be an administrative user to do this.", "admin_only");
			}

			if (subfolder != null && subfolder.IndexOf('.') != -1)
			{
				throw new PublicException("Invalid subfolder - it must not contain '.'", "invalid_subfolder");
			}

			return await _contentSyncService.SyncContentFiles(subfolder);
		}

	}

	/// <summary>
	/// Diff between local + remote.
	/// </summary>
	public class ContentFileDiffSet
	{
		/// <summary>
		/// Locally added or updated files.
		/// </summary>
		public List<ContentFileInfo> LocalOnly = new List<ContentFileInfo>();

		/// <summary>
		/// Remote added or updated files.
		/// </summary>
		public List<ContentFileInfo> RemoteOnly = new List<ContentFileInfo>();

		/// <summary>
		/// Create a set
		/// </summary>
		/// <param name="local"></param>
		/// <param name="remote"></param>
		public ContentFileDiffSet(List<ContentFileInfo> local, List<ContentFileInfo> remote)
		{

			var lookupLocal = CreateLookup(local);
			var lookupRemote = CreateLookup(remote);

			foreach (var kvp in lookupRemote)
			{
				if (lookupLocal.TryGetValue(kvp.Key, out ContentFileInfo localFile))
				{
					// Changed?
					if (localFile.Size != kvp.Value.Size || localFile.ModifiedTicksUtc != kvp.Value.ModifiedTicksUtc)
					{
						var tickDiff = ((kvp.Value.ModifiedTicksUtc - localFile.ModifiedTicksUtc) / TimeSpan.TicksPerSecond);

						Log.Info("contentsyncservice", "Tick diff " + tickDiff + "s");

						if (localFile.Size == kvp.Value.Size && tickDiff < 30)
						{
							// Tick diff is too small to care about this file.
							continue;
						}

						if (localFile.ModifiedTicksUtc > kvp.Value.ModifiedTicksUtc)
						{
							// Local newer
							LocalOnly.Add(localFile);
						}
						else
						{
							// Remote newer
							RemoteOnly.Add(kvp.Value);
						}
					}
				}
				else
				{
					// Remote only
					RemoteOnly.Add(kvp.Value);
				}

			}

			foreach (var kvp in lookupLocal)
			{
				if (!lookupRemote.ContainsKey(kvp.Key))
				{
					// Local only
					LocalOnly.Add(kvp.Value);
				}
			}

		}

		private Dictionary<string, ContentFileInfo> CreateLookup(List<ContentFileInfo> fileSet)
		{
			var dict = new Dictionary<string, ContentFileInfo>();

			foreach (var file in fileSet)
			{
				dict[file.Path.ToLower()] = file;
			}

			return dict;
		}

	}

	/// <summary>
	/// 
	/// </summary>
	public struct SyncStats
	{
		/// <summary>
		/// 
		/// </summary>
		public int Downloaded;
	}

	/// <summary>
	/// Info about a file in the content tree.
	/// </summary>
	public struct ContentFileInfo
	{
		/// <summary>
		/// Path rel to content directory
		/// </summary>
		public string Path;

		/// <summary>
		/// File size.
		/// </summary>
		public long Size;

		/// <summary>
		/// UTC ticks last modded.
		/// </summary>
		public long ModifiedTicksUtc;
	}
}
