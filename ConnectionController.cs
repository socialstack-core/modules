using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Users;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Connections
{
    /// <summary>
    /// Handles connection endpoints.
    /// </summary>

    [Route("v1/connection")]
	[ApiController]
	public partial class ConnectionController : ControllerBase
    {
        private IConnectionService _connections;
        private IUserService _users;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public ConnectionController(
            IConnectionService connections,
			IUserService users
        )
        {
            _connections = connections;
            _users = users;
        }

		/// <summary>
		/// GET /v1/connection/2/
		/// Returns the connection data for a single connection.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Connection> Load([FromRoute] int id)
        {
			var context = Request.GetContext();
            var result = await _connections.Get(context, id);
			return await Events.ConnectionLoad.Dispatch(context, result, Response);
        }

		/// <summary>
		/// DELETE /v1/connection/2/
		/// Deletes a connection
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _connections.Get(context, id);
			result = await Events.ConnectionDelete.Dispatch(context, result, Response);

			if (result == null || !await _connections.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
            return new Success();
        }

		/// <summary>
		/// GET /v1/connection/list
		/// Lists all connections available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Connection>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/connection/list
		/// Lists filtered connections available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Connection>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Connection>(filters);

			filter = await Events.ConnectionList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _connections.List(context, filter);
			return new Set<Connection>() { Results = results };
		}

		/// <summary>
		/// POST /v1/connection/
		/// Creates a new connection. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<Connection> Create([FromBody] ConnectionAutoForm form)
		{
			var context = Request.GetContext();

			// Get the target user:
			var targetUser = await _users.Get(context, form.ConnectedToId);
			
			if(targetUser == null){
				return null;
			}
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var connection = new Connection
			{
				UserId = context.UserId
			};

			if (!ModelState.Setup(form, connection))
			{
				return null;
			}

			form = await Events.ConnectionCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			connection = await _connections.Create(context, form.Result);

			if (connection == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return connection;
        }

		/// <summary>
		/// POST /v1/connection/1/
		/// Creates a new connection. Returns the ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Connection> Update([FromRoute] int id, [FromBody] ConnectionAutoForm form)
		{
			var context = Request.GetContext();

			var connection = await _connections.Get(context, id);
			
			if (!ModelState.Setup(form, connection)) {
				return null;
			}

			form = await Events.ConnectionUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			connection = await _connections.Update(context, form.Result);

			if (connection == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return connection;
		}

	}

}
