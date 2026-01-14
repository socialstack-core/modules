using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
		psi.RedirectStandardOutput = true;
		psi.RedirectStandardError = true;
		psi.CreateNoWindow = true;

		var tcs = new TaskCompletionSource<int>();

		var process = new Process
		{
			StartInfo = psi,
			EnableRaisingEvents = true
		};

		process.OutputDataReceived += async (s, ea) =>
		{
			if (ea.Data != null)
			{
				var bytes = Encoding.UTF8.GetBytes(ea.Data + Environment.NewLine);
				if (outputStream != null)
				{
					await outputStream.WriteAsync(bytes, 0, bytes.Length);
				}
				else
				{
					Console.Out.WriteLine(ea.Data);
				}
			}
		};

		process.ErrorDataReceived += async (s, ea) =>
		{
			if (ea.Data != null)
			{
				var bytes = Encoding.UTF8.GetBytes(ea.Data + Environment.NewLine);
				if (errStream != null)
				{
					await errStream.WriteAsync(bytes, 0, bytes.Length);
				}
				else
				{
					Console.Error.WriteLine(ea.Data);
				}
			}
		};

		process.Exited += (sender, args) =>
		{
			tcs.SetResult(process.ExitCode);
			process.Dispose();
		};

		if (process.Start())
		{
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
		}

		return tcs.Task;
	}
	
}