using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Api.Startup;


/// <summary>
/// Used to invoke commands. Writes command output into the specified stream, or stdout otherwise.
/// </summary>
public static class CommandLine
{
	
	/// <summary>
	/// Execute a command.
	/// </summary>
	public static Task<int> Execute(string command, Stream outputStream = null, Stream errStream = null)
	{

		ProcessStartInfo psi;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			psi = new ProcessStartInfo("/bin/bash", "-c \"" + command + "\"");
		}
		else
		{
			psi = new ProcessStartInfo("cmd.exe", "/c " + command);
		}

		psi.UseShellExecute = false;
		psi.RedirectStandardOutput = outputStream != null;
		psi.RedirectStandardError = errStream != null;
		psi.CreateNoWindow = true;
		
		var tcs = new TaskCompletionSource<int>();

		var process = new Process
		{
			StartInfo = psi,
			EnableRaisingEvents = true
		};

		process.Exited += (sender, args) =>
		{
			tcs.SetResult(process.ExitCode);
			process.Dispose();
		};

		if (outputStream != null)
		{
			process.OutputDataReceived += async (s, ea) =>
			{
				var outBytes = System.Text.Encoding.UTF8.GetBytes(ea.Data);
				await outputStream.WriteAsync(outBytes, 0, outBytes.Length);
			};
		}

		if (errStream != null)
		{
			process.ErrorDataReceived += async (s, ea) =>
			{
				var outBytes = System.Text.Encoding.UTF8.GetBytes(ea.Data);
				await errStream.WriteAsync(outBytes, 0, outBytes.Length);
			};
		}

		if (process.Start())
		{
			if (outputStream != null)
			{
				process.BeginOutputReadLine();

			}
			if (errStream != null)
			{
				process.BeginErrorReadLine();

			}
		}

		return tcs.Task;

	}
	
}