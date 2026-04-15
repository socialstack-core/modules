namespace Api.AutomationTasks;


/// <summary>
/// Status of an automation task.
/// </summary>
public enum TaskStatus : int {
	/// <summary>
	/// Task is queued and waiting to be processed.
	/// </summary>
	Pending = 0,
	/// <summary>
	/// Task is currently being processed.
	/// </summary>
	Running = 1,
	/// <summary>
	/// Task completed successfully.
	/// </summary>
	Completed = 2,
	/// <summary>
	/// Task failed during execution.
	/// </summary>
	Failed = 3
}
