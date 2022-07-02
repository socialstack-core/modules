using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.FeedStories
{
	/// <summary>
	/// Handles users who follow other users.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IFeedStoryService
	{
		/// <summary>
		/// Deletes a feed story by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Gets a single feed story by its ID.
		/// </summary>
		Task<FeedStory> Get(Context context, int id);

		/// <summary>
		/// Creates a new feed story.
		/// </summary>
		Task<FeedStory> Create(Context context, FeedStory feedStory);

		/// <summary>
		/// Updates the given feed story.
		/// </summary>
		Task<FeedStory> Update(Context context, FeedStory feedStory);

		/// <summary>
		/// List a filtered set of feed stories.
		/// </summary>
		/// <returns></returns>
		Task<List<FeedStory>> List(Context context, Filter<FeedStory> filter);
	}
}
