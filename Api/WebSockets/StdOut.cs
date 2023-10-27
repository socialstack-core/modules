using Api.Contexts;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.WebSockets;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Startup
{
	
	/// <summary>
	/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
	/// Not required to use these - you can also just directly use ControllerBase if you want.
	/// Like AutoService this isn't in a namespace due to the frequency it's used.
	/// </summary>
	public partial class StdOutController : ControllerBase
	{
		
		/// <summary>
		/// Gets the latest number of websocket clients.
		/// </summary>
		[HttpGet("clients")]
		public async ValueTask GetWsClientCount()
		{
			var context = await Request.GetContext();
			
			Response.ContentType = _applicationJson;
			
			if(context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("monitoring_stdout", context);
			}
			
			var writer = Writer.GetPooled();
			writer.Start(null);

			writer.WriteASCII("{\"clients\":");

			writer.WriteS(Services.Get<WebSocketService>().GetClientCount());
			
			writer.Write((byte)'}');
			
			// Flush after each one:
			await writer.CopyToAsync(Response.Body);
			writer.Release();
		}
		
		
	}
	
}