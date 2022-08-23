using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Api.Startup;

/// <summary>
/// Captures OS resource statistics such as CPU usage on Windows hosts
/// </summary>
internal class WindowsStatistics : PlatformStatistics
{
	/// <summary>
	/// Collects stats into the given object.
	/// </summary>
	/// <param name="intoObject"></param>
	/// <returns></returns>
	public override ValueTask Collect(HostDetails intoObject)
	{

		return new ValueTask();
	}
}