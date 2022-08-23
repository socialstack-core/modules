using System;
using System.Threading.Tasks;

namespace Api.Startup;


/// <summary>
/// Base class for platform specific statistics.
/// See also LinuxStatistics and WindowsStatistics.
/// </summary>
public class PlatformStatistics
{
	
	/// <summary>
	/// Collects the host stats into the given details object.
	/// </summary>
	public virtual ValueTask Collect(HostDetails intoObject)
	{
		throw new NotImplementedException();
	}
	
}