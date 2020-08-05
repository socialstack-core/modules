using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Connections
{
	/// <summary>
	/// Handles users who follow other users.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IConnectionService
	{
		/// <summary>
		/// Gets a single connection by its ID.
		/// </summary>
		Task<Connection> Get(Context context, int id);

		/// <summary>
		/// Creates a new connection.
		/// </summary>
		Task<Connection> Create(Context context, Connection connection);

		/// <summary>
		/// Updates the given connection.
		/// </summary>
		Task<Connection> Update(Context context, Connection connection);

		/// <summary>
		/// List a filtered set of connections.
		/// </summary>
		/// <returns></returns>
		Task<List<Connection>> List(Context context, Filter<Connection> filter);
	}
}
