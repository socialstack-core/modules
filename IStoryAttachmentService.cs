using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.StoryAttachments
{
	/// <summary>
	/// Handles users who follow other users.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IStoryAttachmentService
	{
		/// <summary>
		/// Deletes a story attachment by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Gets a single story attachment by its ID.
		/// </summary>
		Task<StoryAttachment> Get(Context context, int id);

		/// <summary>
		/// Creates a new story attachment.
		/// </summary>
		Task<StoryAttachment> Create(Context context, StoryAttachment storyAttachment);

		/// <summary>
		/// Updates the given story attachment.
		/// </summary>
		Task<StoryAttachment> Update(Context context, StoryAttachment storyAttachment);

		/// <summary>
		/// List a filtered set of story attachments.
		/// </summary>
		/// <returns></returns>
		Task<List<StoryAttachment>> List(Context context, Filter<StoryAttachment> filter);
	}
}
