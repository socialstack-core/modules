using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Comments
{
    /// <summary>
    /// Handles comment endpoints.
    /// </summary>

    [Route("v1/comment")]
	[ApiController]
	public partial class CommentController : ControllerBase
    {
        private ICommentService _comments;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public CommentController(
			ICommentService comments
        )
        {
			_comments = comments;
        }

		/// <summary>
		/// GET /v1/comment/2/
		/// Returns the reply data for a single reply.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Comment> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _comments.Get(context, id);
			return await Events.CommentLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/comment/2/
		/// Deletes a comment
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _comments.Get(context, id);
			result = await Events.CommentDelete.Dispatch(context, result, Response);

			if (result == null || !await _comments.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}

			return new Success();
		}

		/// <summary>
		/// GET /v1/comment/list
		/// Lists all comments available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Comment>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/comment/list
		/// Lists filtered comments available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Comment>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Comment>(filters);

			filter = await Events.CommentList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _comments.List(context, filter);
			return new Set<Comment>() { Results = results };
		}

		/// <summary>
		/// POST /v1/comment/
		/// Creates a new comment. Returns the ID.
		/// To reply to a particular comment, use that as the content ID/ content type.
		/// </summary>
		[HttpPost]
		public async Task<Comment> Create([FromBody] CommentAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var comment = new Comment
			{
				UserId = context.UserId
			};
			
			// Tidy up content type:
			form.ContentType = form.ContentType.ToLower().Trim();
			
			if (!ModelState.Setup(form, comment))
			{
				return null;
			}
			
			// Special case if commenting on a comment. 
			// We need to grab the original content to set the parent content Id correctly.
			if (form.ContentType == "comment")
			{
				var parent = await _comments.Get(context, form.ContentId);

				if (parent == null)
				{
					Response.StatusCode = 400;
					return null;
				}
				
				comment.ContentId = parent.ContentId;
				comment.ContentTypeId = parent.ContentTypeId;
				comment.ParentCommentId = form.ContentId;
			}
			else
			{
				comment.ContentId = form.ContentId;
				comment.ContentTypeId = ContentTypes.GetId(form.ContentType);
			}
			
			form = await Events.CommentCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			comment = await _comments.Create(context, form.Result);

			if (comment == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            var id = comment.Id;
            return comment;
        }

		/// <summary>
		/// POST /v1/comment/1/
		/// Updates a comment with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Comment> Update([FromRoute] int id, [FromBody] CommentAutoForm form)
		{
			var context = Request.GetContext();

			var comment = await _comments.Get(context, id);
			
			if (!ModelState.Setup(form, comment)) {
				return null;
			}

			form = await Events.CommentUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			comment = await _comments.Update(context, form.Result);

			if (comment == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return comment;
		}
		
    }

}
