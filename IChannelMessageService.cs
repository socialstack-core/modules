using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.ChannelMessages
{
	/// <summary>
	/// Handles channel message.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IChannelMessageService
	{
		/// <summary>
		/// Deletes an message by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id, bool deleteContent = true);

		/// <summary>
		/// Gets a single message by its ID.
		/// </summary>
		Task<ChannelMessage> Get(Context context, int id);

		/// <summary>
		/// Creates a new message.
		/// </summary>
		Task<ChannelMessage> Create(Context context, ChannelMessage message);

		/// <summary>
		/// Updates the given message.
		/// </summary>
		Task<ChannelMessage> Update(Context context, ChannelMessage message);

		/// <summary>
		/// List a filtered set of messages.
		/// </summary>
		/// <returns></returns>
		Task<List<ChannelMessage>> List(Context context, Filter<ChannelMessage> filter);
	}
}
