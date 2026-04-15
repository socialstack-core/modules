using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.AutomationTasks
{
	
	/// <summary>
	/// An AutomationTask
	/// </summary>
	public partial class AutomationTask : UserCreatedContent<uint>
	{
		/// <summary>
		/// The name of the automation. Matches AutomationRunInfo.Name.
		/// </summary>
		[DatabaseIndex]
		[DatabaseField(Length = 200)]
		public string AutomationName;

		/// <summary>
		/// Identifier for the server holding the lock.
		/// </summary>
		public string ServerId;

		/// <summary>
		/// Current status of the task. Uses values from TaskStatus.
		/// </summary>
		public int Status;

		/// <summary>
		/// Lock expiry timestamp.
		/// </summary>
		public DateTime? LockedUntilUtc;

		/// <summary>
		/// When the last attempt started.
		/// </summary>
		public DateTime? LastAttemptStartUtc;

		/// <summary>
		/// When the last attempt ended.
		/// </summary>
		public DateTime? LastAttemptEndUtc;
	}

}