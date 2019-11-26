using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.ChannelUsers
{
	/// <summary>
	/// Handles users who have access to particular channels.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IChannelUserService
	{
		/// <summary>
		/// Deletes a channel user by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id, bool deleteContent = true);

		/// <summary>
		/// Gets a single channel user by its ID.
		/// </summary>
		Task<ChannelUser> Get(Context context, int id);

		/// <summary>
		/// Creates a new channel user.
		/// </summary>
		Task<ChannelUser> Create(Context context, ChannelUser channelUser);

		/// <summary>
		/// Updates the given channel user.
		/// </summary>
		Task<ChannelUser> Update(Context context, ChannelUser channelUser);

		/// <summary>
		/// List a filtered set of channel users.
		/// </summary>
		/// <returns></returns>
		Task<List<ChannelUser>> List(Context context, Filter<ChannelUser> filter);
	}
}
