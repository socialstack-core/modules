namespace Api.Eventing;

/// <summary>
/// Event group for logging related events.
/// </summary>
public partial class LoggingEventGroup : EventGroup
{
	/// <summary>
	/// Raised when the log file has been appended to. It is provided with the file path, append start and new total length.
	/// </summary>
	public EventHandler<string, long, long> FileAppended;
}

/// <summary>
/// Events are instanced automatically. 
/// You can however specify a custom type or instance them yourself if you'd like to do so.
/// </summary>
public partial class Events
{
	/// <summary>
	/// All logging events.
	/// </summary>
	public static LoggingEventGroup Logging;
}