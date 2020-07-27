using System;
using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Microsoft.Extensions.Configuration;
using Api.Configuration;
using Api.StackTools;
using System.Diagnostics;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using System.Linq;
using System.IO;

namespace Api.ContentSync
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ContentSyncService : IContentSyncService
	{
		static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
		private ContentSyncConfig _configuration;
		private IDatabaseService _database;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ContentSyncService(IDatabaseService database)
		{
			_database = database;

			if (Services.Started)
			{
				Start();
			}
			else
			{
				// Must happen after services start otherwise the page service isn't necessarily available yet.
				// Notably this happens immediately after services start in the first group
				// (that's before any e.g. system pages are created).
				Events.ServicesAfterStart.AddEventListener(async (Context ctx, object src) =>
				{
					await Start();
					return src;
				}, 1);
			}
		}

		/// <summary>
		/// Starts the content sync service.
		/// Must run after all other services have loaded.
		/// </summary>
		public Task<bool> Start()
		{
			// The content sync service is used to keep content created by multiple instances in sync.
			// (which can be a cluster of servers, or a group of developers)
			// It does this by setting up 'stripes' of IDs which are assigned to particular users.
			// A user is identified by "socialstack sync whoami" or if not set, the computer hostname is used instead.

			var section = AppSettings.GetSection("ContentSync");

			if (section == null)
			{
				return Task.FromResult(false);
			}

			_configuration = section.Get<ContentSyncConfig>();

			if (_configuration == null || _configuration.Users == null || _configuration.Users.Count == 0)
			{
				Console.WriteLine("[WARN] Content sync is installed but not configured.");
				return Task.FromResult(false);
			}

			var taskCompletionSource = new TaskCompletionSource<bool>();
			try {
				// Get the user:
				var stackTools = new NodeProcess("socialstack sync whoami", true);
				var errored = false;
				string name = null;

				stackTools.Process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
				{
					if (string.IsNullOrEmpty(e.Data))
					{
						return;
					}

					name = e.Data.Trim();
				});

				stackTools.Process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
				{
					if (string.IsNullOrEmpty(e.Data))
					{
						return;
					}

					errored = true;
				});

				stackTools.OnStateChange += (NodeProcessState state) =>
				{

					if (state == NodeProcessState.EXITING)
					{
						if (errored || name == null)
						{
							name = System.Environment.MachineName.ToString();
						}
						Console.WriteLine("Content sync starting with config for '" + name + "'");

						Task.Run(async () =>
						{
							await StartFor(name);
							taskCompletionSource.SetResult(true);
						});

					}

				};

				stackTools.Start();
			}
			catch(Exception e)
			{
				taskCompletionSource.SetException(e);
			}

			return taskCompletionSource.Task;
		}

		private string FileSafeName(string name)
		{
			return new string(name.Where(ch => !InvalidFileNameChars.Contains(ch)).ToArray());
		}

		/// <summary>
		/// When in local dev, this is "this" user's sync table set.
		/// </summary>
		public SyncTableFileSet LocalTableSet;

		/// <summary>
		/// Starts using the given username as this instance. Often a hostname on prod servers.
		/// </summary>
		/// <param name="name"></param>
		private async Task StartFor(string name)
		{
			_configuration.Users.TryGetValue(name, out List<StripeRange> myRanges);
			
			if(myRanges == null || myRanges.Count == 0)
			{
				Console.WriteLine("[WARN]: Content sync disabled. This instance (" + name + ") has no allocation in the project appsettings.json ContentSync config.");
				return;
			}

			// Find the biggest max value:
			var overallMax = 0;

			foreach (var kvp in _configuration.Users)
			{
				if (kvp.Value == null || kvp.Value.Count == 0)
				{
					continue;
				}

				foreach (var range in kvp.Value)
				{
					if (range.Max > overallMax)
					{
						overallMax = range.Max;
					}
				}
			}

			// Load the allocations:
			StripeTable table = new StripeTable(myRanges, overallMax);

			await table.Setup(_database);

			Console.WriteLine("Content sync ID information obtained");

#if DEBUG
			// Hello developers!
			// Create syncfile object for each known table and for each user.

			Console.WriteLine("Content sync now checking for changes");

			// Make sure a sync dir exists for this user.
			// Syncfiles go in as Database/FILENAME_SAFE_USERNAME/tableName.txt
			var dirName = FileSafeName(name);
			Directory.CreateDirectory("Database/" + dirName);

			try
			{
				// Load them:
				Dictionary<string, SyncTableFileSet> loadedSyncSets = new Dictionary<string, SyncTableFileSet>();

				foreach (var kvp in _configuration.Users)
				{
					if (kvp.Value == null || kvp.Value.Count == 0)
					{
						continue;
					}

					// Create the table set:
					var syncSet = new SyncTableFileSet("Database/" + FileSafeName(kvp.Key));

					// Set it up:
					// (for "my" files, I'm going to instance them all - regardless of if the actual file exists or not).
					var mine = kvp.Key == name;

					if (mine)
					{
						LocalTableSet = syncSet;
					}

					syncSet.Setup(!mine);
					loadedSyncSets[kvp.Key] = syncSet;

					if (!mine)
					{
						// Apply the sync set:
						await syncSet.Sync(_database);
					}
				}

			}
			catch (Exception e)
			{
				Console.WriteLine("ContentSync failed to handle other user's updates with error: " + e.ToString());
			}
#endif

			// Next, add Create handlers to all types.
			// When the handler fires, we simply assign an ID from our pool.
			// DatabaseService internally handles predefined IDs already.
			foreach (var kvp in ContentTypes.TypeMap)
			{
				if (kvp.Value == typeof(UserCreatedRow) || kvp.Value == typeof(RevisionRow))
				{
					continue;
				}

				// Db table name is..
				var tableName = kvp.Value.TableName();

				// Get the assigner for this table:
				if (!table.DataTables.TryGetValue(tableName, out IdAssigner assigner))
				{
					// If this ever happens, it signals an internal issue with Socialstack.
					// Content types drive the table schema. ID assigners come from the table schema.
					// If a content type was somehow skipped, or its name is mangled, then this would happen.
					Console.WriteLine("[WARN] Content sync integrity issue. The content type '" + kvp.Key + "' does not have an ID assigner.");
					continue;
				}

				// Get the service for this type next:
				var beforeCreate = Events.FindByType(kvp.Value, "Create", EventPlacement.Before);

				if (beforeCreate.Count == 0)
				{
					// This indicates someone has somehow missed adding a BeforeCreate event for a particular type.
					// (Given that it's all automated, this should be actually quite hard to do, but would indicate developer error).
					Console.WriteLine("[WARN] Content sync can't mount a type. The content type '" + kvp.Key + "' does not have a BeforeCreate event.");
					continue;
				}

				beforeCreate[0].AddEventListener((Context context, object[] args) =>
				{
					if (args == null || args.Length == 0)
					{
						return null;
					}

					// Get object as a DB Row instance (so we can actually set the ID):
					var dbRow = args[0] as DatabaseRow;

					if (dbRow != null)
					{
						// Assign an ID now!
						dbRow.Id = (int)assigner.Assign();
					}

					return args[0];
				});

#if DEBUG
				// Hello developers!
				// Add handlers to Create, Delete and Update events, and track these in a syncfile for this user.
				if (LocalTableSet != null)
				{
					// Attempt to get the sync file for this type:
					LocalTableSet.Files.TryGetValue(tableName, out SyncTableFile localSyncFile);

					if (localSyncFile != null)
					{
						// Get the after create event - we want to track creation of objects:
						var afterCreate = Events.FindByType(kvp.Value, "Create", EventPlacement.After);

						if (afterCreate.Count != 0)
						{
							afterCreate[0].AddEventListener((Context context, object[] args) =>
							{
								if (args == null || args.Length == 0)
								{
									return null;
								}

								var firstArg = args[0];

								if (firstArg is DatabaseRow)
								{
									// Write creation to sync file:
									localSyncFile.Write(firstArg, 'C', context == null ? 0 : context.LocaleId);
								}

								return firstArg;
							});
						}

						var afterUpdate = Events.FindByType(kvp.Value, "Update", EventPlacement.After);

						if (afterUpdate.Count != 0)
						{
							afterUpdate[0].AddEventListener((Context context, object[] args) =>
							{
								if (args == null || args.Length == 0)
								{
									return null;
								}

								var firstArg = args[0];

								if (firstArg is DatabaseRow)
								{
									// Write update to sync file:
									localSyncFile.Write(firstArg, 'U', context == null ? 0 : context.LocaleId);
								}

								return firstArg;
							});
						}

						var afterDelete = Events.FindByType(kvp.Value, "Delete", EventPlacement.After);

						if (afterDelete.Count != 0)
						{
							afterDelete[0].AddEventListener((Context context, object[] args) =>
							{
								if (args == null || args.Length == 0)
								{
									return null;
								}

								var firstArg = args[0];

								if (firstArg is DatabaseRow)
								{
									// Write delete to sync file:
									localSyncFile.Write(firstArg, 'D', context == null ? 0 : context.LocaleId);
								}

								return firstArg;
							});
						}
					}
				}
#endif

			}

			// Add event handlers to all caching enabled types, *if* there are any with remote addresses.
			// If a change (update, delete, create) happens, broadcast a cache remove message to all remote addresses.
			// If the link drops, poll until remote is back again. Updates must queue up in the meantime.
#warning todo

			// Instance a sync server if remote addresses are present in the config:
			foreach (var kvp in _configuration.Users)
			{
				if (kvp.Key == name)
				{
					continue;
				}

				// Remote address?
			}
		}
	}
    
}
