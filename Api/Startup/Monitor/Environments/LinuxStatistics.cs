using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Api.Startup;

/// <summary>
/// Captures OS resource statistics such as CPU usage on Linux hosts
/// </summary>
internal class LinuxStatistics : PlatformStatistics
{
	private const float KB = 1024f;

	private ulong? _ramMax;

	public float? _cpuUsage;

	public long? AvailableMemory { get; private set; }

	private long MemoryUsage => GC.GetTotalMemory(false);

	private const string MEMINFO_FILEPATH = "/proc/meminfo";
	private const string CPUSTAT_FILEPATH = "/proc/stat";
	
	/// <summary>
	/// Collects the host stats into the given details object.
	/// </summary>
	public override async ValueTask Collect(HostDetails intoObject)
	{
		if(_ramMax == null)
		{
			_ramMax = await CollectTotalPhysicalMemory();
		}
		
		if(_ramMax.HasValue)
		{
			intoObject.RamMax = _ramMax.Value;
			
			if(_ramMax.Value != 0)
			{
				var availableRam = await CollectAvailableMemory();

				if (availableRam.HasValue)
				{
					intoObject.Ram = 1d - ((double)availableRam / (double)_ramMax.Value);
				}
			}
		}
		
		var cpu = await CollectCpuUsage();

		if (cpu.HasValue)
		{
			intoObject.Cpu = cpu.Value;
		}

	}
	
	private async Task<ulong?> CollectTotalPhysicalMemory()
	{
		var memTotalLine = await ReadLineStartingWithAsync(MEMINFO_FILEPATH, "MemTotal");

		if (string.IsNullOrWhiteSpace(memTotalLine))
		{
			return null;
		}
		
		// Format: "MemTotal:       16426476 kB"
		if (!ulong.TryParse(new string(memTotalLine.Where(char.IsDigit).ToArray()), out var totalMemInKb))
		{
			return null;
		}
		
		return totalMemInKb * 1_000;
	}

	private long _prevIdleTime;
	private long _prevTotalTime;

	private async Task<float?> CollectCpuUsage()
	{
		var cpuUsageLine = await ReadLineStartingWithAsync(CPUSTAT_FILEPATH, "cpu  ");

		if (string.IsNullOrWhiteSpace(cpuUsageLine))
		{
			return 0f;
		}

		// Format: "cpu  20546715 4367 11631326 215282964 96602 0 584080 0 0 0"
		var cpuNumberStrings = cpuUsageLine.Split(' ').Skip(2);

		if (cpuNumberStrings.Any(n => !long.TryParse(n, out _)))
		{
			return null;
		}

		var cpuNumbers = cpuNumberStrings.Select(long.Parse).ToArray();
		var idleTime = cpuNumbers[3];
		var iowait = cpuNumbers[4]; // Iowait is not real cpu time
		var totalTime = cpuNumbers.Sum() - iowait;
		
		var deltaIdleTime = idleTime - _prevIdleTime;
		var deltaTotalTime = totalTime - _prevTotalTime;

		// When running in gVisor, /proc/stat returns all zeros, so check here and leave _cpuUsage unset.
		// see: https://github.com/google/gvisor/blob/master/pkg/sentry/fs/proc/stat.go#L88-L95
		if (deltaTotalTime == 0f)
		{
			return null;
		}

		var currentCpuUsage = (1.0f - deltaIdleTime / ((float)deltaTotalTime)) * 100f;

		var previousCpuUsage = _cpuUsage ?? 0f;
		_cpuUsage = (previousCpuUsage + 2 * currentCpuUsage) / 3;
		
		_prevIdleTime = idleTime;
		_prevTotalTime = totalTime;

		return _cpuUsage;
	}
	
	private async Task<ulong?> CollectAvailableMemory()
	{
		var memAvailableLine = await ReadLineStartingWithAsync(MEMINFO_FILEPATH, "MemAvailable");

		if (string.IsNullOrWhiteSpace(memAvailableLine))
		{
			memAvailableLine = await ReadLineStartingWithAsync(MEMINFO_FILEPATH, "MemFree");
			if (string.IsNullOrWhiteSpace(memAvailableLine))
			{
				return null;
			}
		}

		if (!ulong.TryParse(new string(memAvailableLine.Where(char.IsDigit).ToArray()), out var availableMemInKb))
		{
			return null;
		}

		return availableMemInKb * 1_000;
	}
	
	private static async Task<string> ReadLineStartingWithAsync(string path, string lineStartsWith)
	{
		using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 512, FileOptions.SequentialScan | FileOptions.Asynchronous))
		using (var r = new StreamReader(fs, Encoding.ASCII))
		{
			string line;
			while ((line = await r.ReadLineAsync()) != null)
			{
				if (line.StartsWith(lineStartsWith, StringComparison.Ordinal))
					return line;
			}
		}

		return null;
	}
}