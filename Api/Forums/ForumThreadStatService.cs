using Amazon.S3.Model;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Forums
{
	/// <summary>
	/// Handles creations of forum threads - containers for forum posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ForumThreadStatService : AutoService<ForumThreadStat>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumThreadStatService() : base(Events.ForumThreadStat)
		{
			Events.ForumThread.AfterCreate.AddEventListener(async (Context context, ForumThread thread) =>
			{
				// Create stat object:
				var statObj = new ForumThreadStat()
				{
					Id = thread.Id,
					ReplyCount = 0,
					LatestReplyUtc = thread.CreatedUtc
				};

				await Create(context, statObj, DataOptions.IgnorePermissions);

				return thread;
			});

			Events.ForumThread.AfterDelete.AddEventListener(async (Context context, ForumThread thread) =>
			{
				// Delete stat object as well:
				var statObj = await Get(context, thread.Id, DataOptions.IgnorePermissions);
				
				if(statObj != null)
				{
					await Delete(context, statObj, DataOptions.IgnorePermissions);
				}
				
				return thread;
			});

			// --- Reply events ---

			Events.ForumReply.AfterCreate.AddEventListener(async (Context context, ForumReply reply) => {

				// Get the forum stats:
				var forumStatsObj = await Get(context, reply.ThreadId);

				// Bump reply count on the thread stat object.
				await Update(context, forumStatsObj, (Context ctx, ForumThreadStat f, ForumThreadStat orig) =>
				{
					f.ReplyCount++;
					f.LatestReplyUtc = reply.CreatedUtc;
				}, DataOptions.IgnorePermissions);

				return reply;
			});

			Events.ForumReply.AfterDelete.AddEventListener(async (Context context, ForumReply reply) => {

				// Get the forum stats:
				var forumStatsObj = await Get(context, reply.ThreadId);

				// Bump reply count on the thread stat object.
				await Update(context, forumStatsObj, (Context ctx, ForumThreadStat f, ForumThreadStat orig) =>
				{
					f.ReplyCount--;
				}, DataOptions.IgnorePermissions);

				return reply;
			});

		}
	}

}
