using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using System;

namespace Api.Connections
{
    /// <summary>
    /// Handles connection endpoints.
    /// </summary>
    [Route("v1/connection")]
	public partial class ConnectionController : AutoController<Connection>
    {

        /// <summary>
        /// Used to accept an invitaiton to connect.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("{id}/accept")]
        public async Task<Connection> Accept(uint id)
        {
            var context = await Request.GetContext();
            var user = context.User;

            // Is the user valid?
            if(user == null)
            {
                return null;
            }

            // Grab the connection
            var connection = await _service.Get(context, id, DataOptions.IgnorePermissions);

            if (connection == null)
            {
                return null;
            }

            // Is the user the target user. 
            if (connection.ConnectedToId != user.Id && connection.Email != user.Email)
            {
                return null;
            }

            // Is this connection already accepted?
            if (connection.AcceptedUtc != null)
            {
                return null;
            }

            connection = await _service.Update(context, connection, (Context ctx, Connection con, Connection originalConn) => {
                // Now we need to update the DB. First off, if the ConnectedToUserId is not set, let's set it. 
                if (con.ConnectedToId == 0)
                {
                    con.ConnectedToId = user.Id;
                }

                con.AcceptedUtc = DateTime.UtcNow;
            }, DataOptions.IgnorePermissions);

            return connection;
        }
	}
}