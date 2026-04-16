using Api.Database;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.AutomationTasks
{
	/// <summary>
	/// Handles automationTasks.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class AutomationTaskService : AutoService<Api.AutomationTasks.AutomationTask>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AutomationTaskService() : base(Events.AutomationTask)
        {
			ServerId = System.Environment.MachineName;
		}

		/// <summary>
		/// Gets the local server identifier.
		/// </summary>
		public readonly string ServerId;

		/// <summary>
		/// Obtains a distributed lease for the given automation. Returns true if the lease was acquired.
		/// </summary>
		/// <param name="automationName">The name of the automation to obtain a lease for.</param>
		/// <param name="runInfo">The run info to populate with lease information.</param>
		/// <param name="ctx">The context for this request.</param>
		public async ValueTask<bool> ObtainLease(Context ctx, string automationName, Api.Automations.AutomationRunInfo runInfo)
		{
			var task = await Where("AutomationName=?", DataOptions.IgnorePermissions)
				.Bind(automationName)
				.First(ctx);

			if (task == null)
			{
				var newTask = new AutomationTask
				{
					AutomationName = automationName,
					Status = (int)TaskStatus.Pending
				};

				try
				{
					task = await Create(ctx, newTask, DataOptions.IgnorePermissions);
				}
				catch(Exception e)
				{
					// This happens when two servers attempt to run a task for the first time at exactly the same time.
					// Expected to be rare.

					Log.Warn("automations", e, "Rare automation first time collision. You can ignore this if the error is for an already existing index key.");

					task = await Where("AutomationName=?", DataOptions.IgnorePermissions)
						.Bind(automationName)
						.First(ctx);
				}
			}

			if (task == null)
			{
				return false;
			}

			runInfo.TaskId = task.Id;

			// Check if some other server already has a claim on this task.
			if (task.ServerId != ServerId && (task.LockedUntilUtc.HasValue && task.LockedUntilUtc.Value >= DateTime.UtcNow))
			{
				return false;
			}

			// Attempt the claim now. Only one server can win a "CheckNotChanged" update race.
			var claimed = await Update(ctx, task, (Context c, AutomationTask toUpdate, AutomationTask orig) =>
			{
				toUpdate.ServerId = ServerId;
				toUpdate.LockedUntilUtc = DateTime.UtcNow.AddMinutes(1);
				toUpdate.Status = (int)TaskStatus.Running;
				toUpdate.LastAttemptStartUtc = DateTime.UtcNow;
			}, DataOptions.IgnorePermissions | DataOptions.CheckNotChanged);

			if (claimed != null)
			{
				runInfo.ActiveLease = claimed;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Ends the distributed lease for the given automation.
		/// </summary>
		/// <param name="runInfo">The run info with the active lease.</param>
		/// <param name="ctx">The context for this request.</param>
		/// <param name="success">True if the task completed successfully, false if it failed.</param>
		public async ValueTask EndLease(Context ctx, Api.Automations.AutomationRunInfo runInfo, bool success)
		{
			var activeLease = runInfo.ActiveLease;

			if (activeLease == null)
			{
				return;
			}

			runInfo.ActiveLease = null;

			await Update(ctx, activeLease, (Context c, AutomationTask toUpdate, AutomationTask orig) =>
			{
				toUpdate.LockedUntilUtc = null;
				toUpdate.Status = success ? (int)TaskStatus.Completed : (int)TaskStatus.Failed;
				toUpdate.LastAttemptEndUtc = DateTime.UtcNow;
				toUpdate.ServerId = null;
			}, DataOptions.IgnorePermissions);
		}
	}
     
}
