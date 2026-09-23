using Api.Database;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using Api.Startup;

namespace Api.Forums
{
	/// <summary>
	/// Handles creations of forum threads - containers for forum posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ForumStatService : AutoService<ForumStat>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ForumStatService() : base(Events.ForumStat)
		{
			Events.Forum.AfterCreate.AddEventListener(async (Context context, Forum forum) =>
			{
				// Create stat object:
				var statObj = new ForumStat()
				{
					Id = forum.Id,
					ReplyCount = 0,
					LatestReplyUtc = null
				};

				await Create(context, statObj, DataOptions.IgnorePermissions);

				return forum;
			});

			Events.Forum.AfterDelete.AddEventListener(async (Context context, Forum forum) =>
			{
				// Delete stat object as well:
				var statObj = await Get(context, forum.Id, DataOptions.IgnorePermissions);
				
				if(statObj != null)
				{
					await Delete(context, statObj, DataOptions.IgnorePermissions);
				}
				
				return forum;
			});

			// --- Thread events ---

			Events.ForumThread.AfterCreate.AddEventListener(async (Context context, ForumThread thread) => {

				// Get the forum stats:
				var forumStatsObj = await Get(context, thread.ForumId);

				// Bump thread count on the forums stat object.
				await Update(context, forumStatsObj, (Context ctx, ForumStat f, ForumStat orig) =>
				{
					f.ThreadCount++;
					f.LatestReplyUtc = thread.CreatedUtc;
				}, DataOptions.IgnorePermissions);

				return thread;
			});

			Events.ForumThread.AfterDelete.AddEventListener(async (Context context, ForumThread thread) => {

				// Get the forum stats:
				var forumStatsObj = await Get(context, thread.ForumId);

				// Bump thread count on the forums stat object.
				await Update(context, forumStatsObj, (Context ctx, ForumStat f, ForumStat orig) =>
				{
					f.ThreadCount--;
				}, DataOptions.IgnorePermissions);

				return thread;
			});

			// --- Reply events ---

			Events.ForumReply.AfterCreate.AddEventListener(async (Context context, ForumReply reply) => {

				// Get the forum stats:
				var forumStatsObj = await Get(context, reply.ForumId);

				// Bump reply count on the forums stat object.
				await Update(context, forumStatsObj, (Context ctx, ForumStat f, ForumStat orig) =>
				{
					f.ReplyCount++;
					f.LatestReplyUtc = reply.CreatedUtc;
				}, DataOptions.IgnorePermissions);

				return reply;
			});

			Events.ForumReply.AfterDelete.AddEventListener(async (Context context, ForumReply reply) => {

				// Get the forum stats:
				var forumStatsObj = await Get(context, reply.ForumId);

				// Bump reply count on the forums stat object.
				await Update(context, forumStatsObj, (Context ctx, ForumStat f, ForumStat orig) =>
				{
					f.ReplyCount--;
				}, DataOptions.IgnorePermissions);

				return reply;
			});

		}
	}

}
