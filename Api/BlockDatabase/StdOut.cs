using Api.NetworkNodes;
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
			// Get node ID from chain service:
			var id = Services.Get<NetworkNodeService>().NodeId;
			
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