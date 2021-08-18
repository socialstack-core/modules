
using Api.ContentSync;
using Api.Contexts;
using Api.Permissions;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


namespace Api.Startup
{
	public partial class StdOutController : ControllerBase
	{
		
		
		/// <summary>
		/// Forces a GC run. Convenience for testing for memory leaks.
		/// </summary>
		[HttpGet("whoami")]
		public async ValueTask WhoAmI()
		{
			var context = await Request.GetContext();

			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("monitoring_whoami", context);
			}

			// Get server ID from csync service:
			var id = Services.Get<ContentSyncService>().ServerId;
			
			var writer = Writer.GetPooled();
			writer.Start(null);

			writer.WriteASCII("{\"id\":");
			
			writer.WriteS(id);
			
			writer.Write((byte)'}');

			// Flush after each one:
			await writer.CopyToAsync(Response.Body);
			writer.Release();
		}

	}
}