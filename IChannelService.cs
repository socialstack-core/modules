using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Channels
{
	/// <summary>
	/// Handles forum replies.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IChannelService
	{
		/// <summary>
		/// Deletes an channel by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id, bool deleteContent = true);

		/// <summary>
		/// Gets a single channel by its ID.
		/// </summary>
		Task<Channel> Get(Context context, int id);

		/// <summary>
		/// Creates a new channel.
		/// </summary>
		Task<Channel> Create(Context context, Channel channel);

		/// <summary>
		/// Updates the given channel.
		/// </summary>
		Task<Channel> Update(Context context, Channel channel);

		/// <summary>
		/// List a filtered set of channels.
		/// </summary>
		/// <returns></returns>
		Task<List<Channel>> List(Context context, Filter<Channel> filter);
	}
}
