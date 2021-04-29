using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Api.Comments
{
	/// <summary>
	/// Handles comments.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// Functional restriction: Each "layer" of comments when they're nested in a tree structure can have a max of 65k replies.
	/// That's because the Order field stores comment IDs as 2 byte numbers.
	/// </summary>
	public partial class CommentService : AutoService<Comment>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CommentService() : base(Events.Comment)
        {
			InstallAdminPages("Comments", "fa:fa-comment", new string[] { "id", "name" });

			var childCommentCountField = GetChangeField("ChildCommentCount");
			var childCommentDeleteCount = GetChangeField("ChildCommentDeleteCount");
			var deletedField = GetChangeField("Deleted");

			Events.Comment.BeforeCreate.AddEventListener(async (Context context, Comment comment) => {
				
				if(comment == null)
				{
					return comment;
				}
				
				var hasParent = comment.ParentCommentId.HasValue && comment.ParentCommentId != 0;
				
				if(hasParent)
				{
					// Get parent comment:
					var parent = await Get(context, comment.ParentCommentId.Value, DataOptions.IgnorePermissions);
					
					if(parent == null)
					{
						// Can't comment as a child of this one.
						return null;
					}
					
					// Its comment number is the total child comments the parent has so far:
					if (await StartUpdate(context, parent, DataOptions.IgnorePermissions))
					{
						parent.ChildCommentCount++;
						parent.MarkChanged(childCommentCountField);
						await FinishUpdate(context, parent);
					}
					
					comment.Depth = parent.Depth + 1;
					
					if(comment.Depth > 9){
						// This comment is too deep. Depth reset to 0.
						comment.Depth = 0;
						comment.DepthPage = parent.DepthPage + 1;
						comment.RootParentCommentId = parent.Id;
					}
					else
					{
						comment.RootParentCommentId = parent.RootParentCommentId;
					}
					
					comment.ChildCommentNumber = parent.ChildCommentCount;
					
					// Construct the Order field.
					// It's just the parent order field concatted with the new child comment number.
					
					comment.Order = new byte[(comment.Depth * 2) + 4];

					var maxLength = comment.Order.Length - 2;

					if (parent.Order != null)
					{
						// Parent order length should be comment.Order.Length - 2, but for resiliance we'll permit less.
						if(parent.Order.Length < maxLength)
						{
							maxLength = parent.Order.Length;
						}
						
						// Copy now:
						Array.Copy(parent.Order, 0, comment.Order, 0, maxLength);
					}
					
					comment.Order[maxLength] = (byte)(comment.ChildCommentNumber >> 8);
					comment.Order[maxLength + 1] = (byte)(comment.ChildCommentNumber & 255);
				}
				else
				{
					// A root comment. The initial order field is based directly on the timestamp in seconds.
					var nowOffset = (uint)(DateTime.UtcNow.Subtract(new DateTime(2021, 4, 1))).TotalSeconds;

					// Construct the Order field:
					comment.Order = new byte[]{
						(byte)((nowOffset >> 24) & 255),
						(byte)((nowOffset >> 16) & 255),
						(byte)((nowOffset >> 8) & 255),
						(byte)(nowOffset & 255)
					};
				}
				
				return comment;
				
			});

			Events.Comment.BeforeUpdate.AddEventListener(async (Context ctx, Comment comment) =>
			{
				// We need to handle child comment count. Since that value determines ChildCommentNumber, we don't want
				// to mess with that value, so we will keep track of the currently soft deleted messages through updates.
				// The diff of ChildCommentCount and ChildCommentDeleteCount will give you the number of comments.
				if (ctx == null || comment == null)
                {
					return null;
                }

				// Is there even a parent comment for this comment?
				if (comment.ParentCommentId == null || comment.ParentCommentId.Value == 0)
                {
					// Nope, nothing to update in terms of counts.
					return comment;
                }

				// Let's grab the parent comment.
				var parentComment = await Get(ctx, comment.ParentCommentId.Value, DataOptions.IgnorePermissions);

				// Is the parent comment valid?
				if (parentComment == null)
                {
					// No valid parent comment, let's move along.
					return comment;
                }

				// Excellent, has the delete state changed for this object?
				if (comment.HasChanged(deletedField))
                {
					// We will either increment or decrement count based on the new deleted state.
					
					// Let's update the parent comment now.
					parentComment = await Update(ctx, parentComment, (Context ctx, Comment parent) => {

						if (comment.Deleted)
						{
							parent.ChildCommentDeleteCount++;
							parent.MarkChanged(childCommentDeleteCount);
						}
						else
						{
							if (parent.ChildCommentDeleteCount < 0)
							{
								parent.ChildCommentDeleteCount = 0;
								parent.MarkChanged(childCommentDeleteCount);
							}
							else if(parent.ChildCommentDeleteCount != 0)
							{
								parent.ChildCommentDeleteCount--;
								parent.MarkChanged(childCommentDeleteCount);
							}
						}

					}, DataOptions.IgnorePermissions);
                }

				return comment;
			});
		}
	}
    
}
