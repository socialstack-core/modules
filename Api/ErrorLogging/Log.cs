using Api.ErrorLogging;
using Api.Eventing;
using Api.SocketServerLibrary;
using Api.Startup;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System; // For easier access from code formerly using Console.*


/// <summary>
/// A class for logging messages.
/// </summary>
public static class Log
{
	private static object _lock = new object();
	private static Writer _writePoolHead;
	private static Writer _writePoolTail;
	private static int _writePoolSize;

	/// <summary>
	/// Logs an informational success message for the given tag.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Ok(string tag, string message)
	{
		Ok(tag, null, message);
	}

	/// <summary>
	/// Logs an informational message for the given tag.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Ok(string tag, Exception error, string message = null)
	{
		var ts = Write(Api.ErrorLogging.Schema.OkId, tag, error, message, false);

#if DEBUG
		// Write to console as well.
		LogToConsole("\u001b[42;1m OK    " + ts.ToLocalTime().ToString("T") + " \u001b[0m ", message, error);
#endif
	}

	/// <summary>
	/// Logs an informational message for the given tag.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Info(string tag, string message)
	{
		Info(tag, null, message);
	}

	/// <summary>
	/// Logs an informational message for the given tag.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Info(string tag, Exception error, string message = null)
	{
		var ts = Write(Api.ErrorLogging.Schema.InfoId, tag, error, message, false);

#if DEBUG
		// Write to console as well.
		LogToConsole("\u001b[44;1m INFO  " + ts.ToLocalTime().ToString("T") + " \u001b[0m ", message, error);
#endif
	}

	/// <summary>
	/// Logs a warning message for the given tag.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Warn(string tag, string message)
	{
		Warn(tag, null, message);
	}

	/// <summary>
	/// Logs a warning message for the given tag.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Warn(string tag, Exception error, string message = null)
	{
		var ts = Write(Api.ErrorLogging.Schema.WarnId, tag, error, message, false);

#if DEBUG
		// Write to console as well.
		LogToConsole("\u001b[43;1m WARN  " + ts.ToLocalTime().ToString("T") + " \u001b[0m ", message, error);
#endif
	}

	/// <summary>
	/// Logs an error message for the given tag. It is strongly recommended to pass the exception here too such that the stack trace will be logged.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Error(string tag, string message)
	{
		Error(tag, null, message);
	}

	/// <summary>
	/// Logs an error message for the given tag. It is strongly recommended to pass the exception here too such that the stack trace will be logged.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Error(string tag, Exception error, string message = null)
	{
		var ts = Write(Api.ErrorLogging.Schema.ErrorId, tag, error, message, false);

#if DEBUG
		// Write to console as well.
		LogToConsole("\u001b[41;1m ERROR " + ts.ToLocalTime().ToString("T") + " \u001b[0m ", message, error);
#endif
	}

	/// <summary>
	/// Logs a fatal error message for the given tag. It is strongly recommended to pass the exception here too such that the stack trace will be logged.
	/// Fatal exceptions usually happen just before or during a total application failure. For this reason, they are very high priority.
	/// That means logging a fatal error will force all buffers to flush. If the link to the log store is down then they will be flushed 
	/// to a file for safe keeping until service is restored.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Fatal(string tag, string message)
	{
		Fatal(tag, null, message);
	}

	/// <summary>
	/// Logs a fatal error message for the given tag. It is strongly recommended to pass the exception here too such that the stack trace will be logged.
	/// Fatal exceptions usually happen just before or during a total application failure. For this reason, they are very high priority.
	/// That means logging a fatal error will force all buffers to flush. If the link to the log store is down then they will be flushed 
	/// to a file for safe keeping until service is restored.
	/// The tag SHOULD be lowercase separated with hypens or underscores and represents a subset of logs.
	/// It is suggested to use Service.LogTag: the lowercase type name, or the lowercase service name if you don't have a type.
	/// </summary>
	public static void Fatal(string tag, Exception error, string message = null)
	{
		var ts = Write(Api.ErrorLogging.Schema.FatalId, tag, error, message, true);

#if DEBUG
		// Write to console as well.
		LogToConsole("\u001b[41;1m FATAL " + ts.ToLocalTime().ToString("T") + " /!\\ /!\\ /!\\ /!\\ \u001b[0m ", message, error);
#endif
	}

	/// <summary>
	/// Reads the log for this node backwards.
	/// </summary>
	/// <param name="onRead">A callback to run whilst reading entries in the log. You can interrupt it if you wish to stop.</param>
	/// <returns></returns>
	public static async ValueTask ReadSelfBackwards(Action<LogTransactionReader> onRead)
	{
		// Start stream:
		await Task.Run(() => {
			FileStream fs;

			try
			{
				fs = File.Open(LogFilePath(), FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
			}
			catch (Exception e)
			{
				Log.Error("", e);
				return;
			}

			fs.Seek(0, SeekOrigin.End);
			var reader = new LogTransactionReader();
			reader.Init(GetTransactionSchema(), onRead);
			reader.StartReadBackwards(fs);
			fs.Close();
		});
	}


	/// <summary>
	/// The Lumity schema used by the log transactions.
	/// </summary>
	private static Schema _schema;

	private static Schema GetTransactionSchema()
	{
		if (_schema == null)
		{
			_schema = new Schema();
			_schema.CreateDefaults();
		}

		return _schema;
	}

	private static void LogToConsole(string key, string message, Exception error)
	{
		var msg = key;

		if (error != null)
		{
			if (message != null)
			{
				msg += message + "\r\n\r\n";
			}
			msg += error.ToString();
			msg += "\r\n\r\n";
		}
		else if (message != null)
		{
			msg += message;
			msg += "\r\n";
		}

		StdOut.Writer.WriteBase(msg);
	}

	private static DateTime Write(ulong defId, string tag, Exception error, string message, bool highPriority)
	{
		// Get a writer:
		var writer = Writer.GetPooled();
		writer.Start(null);

		// Timestamp:
		var ts = DateTime.UtcNow;
		var timestamp = (ulong)ts.Ticks;

		// OK tx
		writer.WriteInvertibleCompressed(defId);

		uint fieldCount = 2; // Timestamp and tag

		if (message != null)
		{
			fieldCount++;
		}

		if (error != null)
		{
			fieldCount += 2; // Stack trace and error message

			if (error.InnerException != null)
			{
				fieldCount += 2; // Stack trace and error message
			}
		}

		// Field count
		writer.WriteInvertibleCompressed(fieldCount);

		// Timestamp:
		writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.TimestampFieldDefId);
		writer.WriteInvertibleCompressed(timestamp);
		writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.TimestampFieldDefId);

		// Tag:
		writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.TagFieldDefId);
		writer.WriteInvertibleUTF8(tag);
		writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.TagFieldDefId);

		if (message != null)
		{
			// Message:
			writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.MessageFieldDefId);
			writer.WriteInvertibleUTF8(message);
			writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.MessageFieldDefId);
		}

		if (error != null)
		{
			// Exception message:
			writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.MessageFieldDefId);
			writer.WriteInvertibleUTF8(error.Message);
			writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.MessageFieldDefId);

			// Stack trace
			writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.StackTraceFieldDefId);
			writer.WriteInvertibleUTF8(error.StackTrace);
			writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.StackTraceFieldDefId);

			if (error.InnerException != null)
			{
				var inner = error.InnerException;

				// Exception message:
				writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.MessageFieldDefId);
				writer.WriteInvertibleUTF8(inner.Message);
				writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.MessageFieldDefId);

				// Stack trace
				writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.StackTraceFieldDefId);
				writer.WriteInvertibleUTF8(inner.StackTrace);
				writer.WriteInvertibleCompressed(Api.ErrorLogging.Schema.StackTraceFieldDefId);
			}

		}

		// Field count again (for readers travelling backwards):
		writer.WriteInvertibleCompressed(fieldCount);

		// tx again (for readers travelling backwards):
		writer.WriteInvertibleCompressed(defId);

		// Add the writer to the log:
		Add(writer, highPriority);

		return ts;
	}

	/// <summary>
	/// Don't use this directly. It exists to track log entries written via stdout (Console.WriteLine et al).
	/// </summary>
	public static void FromStdOut(string message)
	{
		Write(Api.ErrorLogging.Schema.InfoId, "", null, message, false);
	}

	/// <summary>
	/// Adds the given writer to the in memory buffer.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="highPriority">True if this message should not sit in the buffer and forces an immediate flush.</param>
	private static void Add(Writer writer, bool highPriority)
	{
		writer.NextInLine = null;
		var size = writer.Length;
		bool doFlush;
		bool willStartTimer = false;
		
		lock (_lock)
		{
			if (_writePoolHead == null)
			{
				_writePoolHead = writer;
			}
			else
			{
				_writePoolTail.NextInLine = writer;
			}
			_writePoolTail = writer;
			size += _writePoolSize;
			_writePoolSize = size;
			doFlush = highPriority || size > 5000000;

			if (!doFlush && !_flushTimer)
			{
				_flushTimer = true;
				willStartTimer = true;
			}
		}

		if (doFlush)
		{
			// Pool is getting large or this message is HP
			Flush();
		}
		else if(willStartTimer)
		{
			// If flush timer is not running, start it now. Flush no more often than once every 3s.
			Task.Delay(3000).ContinueWith(t =>
			{

				// Flush now:
				Flush();

			});
			
		}
	}

	private static bool _flushTimer;

	private static string _logFilePath;

	/// <summary>
	/// Gets the file path that the main log is stored at.
	/// </summary>
	/// <returns></returns>
	public static string LogFilePath()
	{
		if (_logFilePath != null)
		{
			return _logFilePath;
		}

		// Ensures logs dir exists:
		Directory.CreateDirectory("Logs");
		_logFilePath = "Logs/debug.binlog";

		return _logFilePath;
	}

	private static void Flush()
	{
		// Write anything in the buffer to the file
		// Raise an event to indicate a series of messages are being flushed
		// Release the buffers
		Writer current = null;

		lock (_lock)
		{
			current = _writePoolHead;
			_writePoolHead = null;
			_writePoolTail = null;
			_writePoolSize = 0;
			_flushTimer = false;
		}

		// If poolHead is not null, start writing them out to file now.
		if (current == null)
		{
			return;
		}

		// File write itself MUST be inline.
		// This minimises the risk of fatal exceptions being dropped from memory.

		// Open filestream:
		var fs = File.Open(LogFilePath(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
		fs.Seek(0, SeekOrigin.End);
		var startPoint = fs.Position;

		while (current != null)
		{
			current.CopyTo(fs);
			var next = current.NextInLine;
			current.Release();
			current = next;
		}

		var totalLength = fs.Length;
		fs.Close();

		// Start a task which informs other services about the log file being appended.
		Task.Run(async () => {

			await Events.Logging.FileAppended.Dispatch(new Api.Contexts.Context(), _logFilePath, startPoint, totalLength);

		});
	}
}