using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Permissions;
using Api.Pages;
using Api.CanvasRenderer;
using Api.SocketServerLibrary;

namespace Api.Startup;

/// <summary>
/// </summary>
public partial class StdOutController : ControllerBase
{

	/// <summary>
	/// V8 status.
	/// </summary>
	[HttpGet("bufferpool/status")]
	public async ValueTask<BufferPoolStatus> BufferPoolStatus()
	{
		var context = await Request.GetContext();

		if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
		{
			throw PermissionException.Create("monitoring_bpstatus", context);
		}

		var writers = BinaryBufferPool.OneKb.WriterPoolSize();
		var buffers = BinaryBufferPool.OneKb.BufferPoolSize();

		var size = buffers * BinaryBufferPool.OneKb.BufferSize;

		return new BufferPoolStatus()
		{
			WriterCount = writers,
			BufferCount = buffers,
			ByteSize = size
		};
	}

	/// <summary>
	/// Attempts to purge V8 engines from the canvas renderer service.
	/// </summary>
	[HttpGet("bufferpool/clear")]
	public async ValueTask BufferPoolClear()
	{
		var context = await Request.GetContext();

		if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
		{
			throw PermissionException.Create("monitoring_bpclear", context);
		}

		BinaryBufferPool.OneKb.Clear();
	}

}

/// <summary>
/// The buffer pool status
/// </summary>
public struct BufferPoolStatus
{
	/// <summary>
	/// The number of writers
	/// </summary>
	public int WriterCount;

	/// <summary>
	/// The number of buffers
	/// </summary>
	public int BufferCount;

	/// <summary>
	/// The number of bytes
	/// </summary>
	public int ByteSize;
}

