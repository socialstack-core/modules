using System;
using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using System.Threading.Tasks;
using Api.Translate;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using Api.Permissions;
using Api.Users;
using Api.CanvasRenderer;
using System.Reflection.Metadata;

namespace Api.Database
{

	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class Init
	{
		/// <summary>
		/// True if the DB version has been checked.
		/// </summary>
		private bool? VersionCheckResult;

		/// <summary>
		/// The current schema for the database.
		/// </summary>
		// TODO: In a cluster this will gradually diverge.
		private Schema CurrentDbSchema;

		/// <summary>
		/// Database version text.
		/// </summary>
		private string VersionText;

		private MySQLDatabaseService _database;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Init()
		{
			var setupHandlersMethod = GetType().GetMethod(nameof(SetupService));

			// Add handler for the initial locale list:
			Events.Locale.InitialList.AddEventListener(async (Context context, List<Locale> locales) => {

				if (_database == null)
				{
					_database = Services.Get<MySQLDatabaseService>();
				}
				
				try
				{
					locales = await _database.List<Locale>(new Context(), Query.List(typeof(Locale), nameof(Locale)), typeof(Locale));
				}
				catch
				{
					// The table doesn't exist. Locale set is just the default one:
					locales = new List<Locale>();
				}

				if (locales.Count == 0)
				{
					locales.Add(new Locale()
					{
						Code = "en",
						Name = "English",
						Id = 1
					});
				}

				return locales;
			});

			/*
			Events.Service.AfterInstanceTypeUpdate.AddEventListener((Context context, AutoService service) => {

				if (service == null || service.ServicedType == null)
				{
					return service;
				}

				return service;
			});
			*/

			Events.Service.AfterCreate.AddEventListener(async (Context context, AutoService service) => {

				if (service == null || service.ServicedType == null)
				{
					return service;
				}

				if (_database == null)
				{
					_database = Services.Get<MySQLDatabaseService>();
				}
				
				var servicedType = service.ServicedType;

				if (servicedType != null)
				{
					// Add data load events:
					var setupType = setupHandlersMethod.MakeGenericMethod(new Type[] {
						servicedType,
						service.IdType
					});

					var task = (Task)setupType.Invoke(this, new object[] {
						service
					});

					await task;
				}

				// Service can now attempt to load its cache:
				await service.SetupCacheIfNeeded();

				return service;
			}, 2);
			
		}

		/// <summary>
		/// Sets up for the given type with its event group along with updating any DB tables.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="service"></param>
		public async Task SetupService<T, ID>(AutoService<T, ID> service)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.

			// Special case if it is a mapping type.
			var entityName = service.EntityName;
			var isDbStored = service.DataIsPersistent;

			service.EventGroup.Delete.AddEventListener((Context context, T result) =>
			{
				if (result == null)
				{
					return new ValueTask<T>((T)null);
				}

				// Remove from the primary cache:
				var cache = service.GetCacheForLocale(1);

				if (cache != null)
				{
					cache.Remove(context, result.Id);
				}

				return new ValueTask<T>(result);
			}, 100);

			service.EventGroup.Update.AddEventListener(async (Context context, T entity, ChangedFields changes, DataOptions opts) =>
			{
				if (entity == null)
				{
					return entity;
				}

				// Cache update.
				var locale = context == null ? 1 : context.LocaleId;
				var cache = service.GetCacheForLocale(locale);

				if (cache == null)
				{
					return entity;
				}

				// Future improvement: rather than copying all fields and
				// writing all fields, instead focus only on the ones which changed.

				// Copy fields from entity -> orig.
				var orig = cache.Get(entity.Id);

				// Anything that makes an assumption that the object doesn't change can continue with that assumption.
				service.CloneEntityInto(entity, orig);

				var id = orig.Id;

				T raw = null;

				if (locale == 1)
				{
					raw = orig;
				}
				else
				{
					if (cache != null)
					{
						raw = cache.GetRaw(id);
					}

					if (raw == null)
					{
						raw = new T();
					}

					// Must also update the raw object in the cache (as the given entity is _not_ the raw one).
					T primaryEntity;

					if (cache == null)
					{
						primaryEntity = await service.Get(new Context(1, context.User, context.RoleId), id, DataOptions.IgnorePermissions);
					}
					else
					{
						primaryEntity = service.GetCacheForLocale(1).Get(id);
					}

					service.PopulateRawEntityFromTarget(raw, orig, primaryEntity);
				}

				if (cache != null)
				{
					cache.Add(context, orig, raw);

					if (locale == 1)
					{
						service.OnPrimaryEntityChanged(orig);
					}

				}
				
				return entity;
			}, 100);

			service.EventGroup.Load.AddEventListener((Context context, T item, ID id) =>
			{
				if (item != null)
				{
					return new ValueTask<T>(item);
				}

				// Load from cache if there is one.
				var cache = service.GetCacheForLocale(context == null ? 1 : context.LocaleId);

				if (cache != null)
				{
					item = cache.Get(id);
				}

				return new ValueTask<T>(item);
			}, 5);

			service.EventGroup.CreatePartial.AddEventListener((Context context, T newEntity) => {

				// If this is a cached type, must add it to all locale caches.
				if (service.CacheAvailable)
				{
					// If the newEntity is not in the primary locale, we will need to derive the raw object.
					// Any localised fields should be set to their default value (null/ 0).
					// [May2023] The above causes issues where an entity is created on a locale other than 1 and is then used by an include
					// which iterates over the cache for locale #1 to collect IDs. DB engine gets it correct but cache does not.

					var raw = context.LocaleId == 1 ? newEntity : new T();

					if (context.LocaleId != 1)
					{
						service.CloneEntityInto(newEntity, raw);
					}

					var localeSet = ContentTypes.Locales;

					for (var i = 0; i < localeSet.Length; i++)
					{
						var locale = localeSet[i];

						if (locale == null)
						{
							continue;
						}

						var cache = service.GetCacheForLocale(locale.Id);

						if (cache == null)
						{
							continue;
						}

						if (i == 0)
						{
							// Primary locale cache - raw and target are the same object.
							cache.Add(context, raw, raw);
						}
						else if (locale.Id == context.LocaleId)
						{
							// Add the given object as-is.
							cache.Add(context, newEntity, raw);
						}
						else
						{
							// Secondary locale. The target object is just a clone of the raw object.
							var entity = (T)Activator.CreateInstance(service.InstanceType);
							service.PopulateTargetEntityFromRaw(entity, raw, raw);

							var localeRaw = (T)Activator.CreateInstance(service.InstanceType);
							service.PopulateTargetEntityFromRaw(localeRaw, raw, raw);

							cache.Add(context, entity, localeRaw);
						}
					}
				}

				return new ValueTask<T>(newEntity);
			});

			service.EventGroup.List.AddEventListener(async (Context context, QueryPair<T, ID> queryPair) => {

				if (queryPair.Handled)
				{
					return queryPair;
				}

				// Do we have a cache?
				var cache = (queryPair.QueryA.DataOptions & DataOptions.CacheFlag) == DataOptions.CacheFlag ? service.GetCacheForLocale(context.LocaleId) : null;

				if (cache != null)
				{
					queryPair.Handled = true;

					// Great - we're using the cache:
					queryPair.Total = await cache.GetResults(context, queryPair, queryPair.OnResult, queryPair.SrcA, queryPair.SrcB);
				}

				return queryPair;
			}, 5);

			var deleteQuery = Query.Delete(service.InstanceType, entityName);
			var createQuery = Query.Insert(service.InstanceType, entityName);
			var createWithIdQuery = Query.Insert(service.InstanceType, entityName, true);
			var mainTableAs = createQuery.MainTableAs;

			var selectQuery = Query.Select(service.InstanceType, entityName);
			var listQuery = Query.List(service.InstanceType, entityName);
			var listRawQuery = Query.List(service.InstanceType, entityName);
			listRawQuery.Raw = true;

			service.EventGroup.AfterInstanceTypeUpdate.AddEventListener(async (Context context, AutoService s) => {

				if (s == null)
				{
					return s;
				}

				// Recreate the cached query objects. This ensures their field set matches the desired field set.
				deleteQuery = Query.Delete(s.InstanceType, entityName);
				createQuery = Query.Insert(s.InstanceType, entityName);
				createWithIdQuery = Query.Insert(s.InstanceType, entityName, true);

				selectQuery = Query.Select(s.InstanceType, entityName);
				listQuery = Query.List(s.InstanceType, entityName);
				listRawQuery = Query.List(s.InstanceType, entityName);
				listRawQuery.Raw = true;

				if (isDbStored)
				{
					await HandleDatabaseType(s);
				}

				return s;
			});
			
			if (isDbStored)
			{
				service.EventGroup.Delete.AddEventListener(async (Context context, T result) =>
				{
					// Delete the entry:
					if (!await _database.RunWithId(context, deleteQuery, result.Id))
					{
						return null;
					}

					// Successful delete from the db.
					return result;
				});

				service.EventGroup.Create.AddEventListener(async (Context context, T entity) =>
				{
					if (!entity.GetId().Equals(default(ID)))
					{
						// Explicit ID has been provided.
						await _database.Run<T, ID>(context, createWithIdQuery, entity);
					}
					else if (!await _database.Run<T, ID>(context, createQuery, entity))
					{
						return default;
					}

					return entity;
				});

				service.EventGroup.Update.AddEventListener(async (Context context, T entity, ChangedFields changes, DataOptions opts) => {

					if (entity == null || changes.None)
					{
						return entity;
					}

					DateTime? prevEdit = changes.PreviousEditedUtc;

					// Attempt the db update.
					using var connection = _database.GetConnection();
					await connection.OpenAsync();
					var cmd = new MySqlCommand();
					cmd.Connection = connection;

					// For each field change, if it is a localised field and locale is not 1 then update the relevant localised field.
					// Otherwise, update the base field.

					StringBuilder str = new StringBuilder();
					str.Append("UPDATE ");
					str.Append(mainTableAs);
					str.Append(" SET ");

					var paramIndex = 0;

					var parameter = cmd.CreateParameter();
					parameter.ParameterName = "id";
					parameter.Value = entity.Id;
					cmd.Parameters.Add(parameter);

					// Get the locale code:
					string localeCode = null;
					var localeId = context.LocaleId;

					if (localeId > 1)
					{
						var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ? 
								ContentTypes.Locales[localeId - 1] : null);
						localeCode = locale.Code;
					}

					foreach (var field in changes)
					{
						if (paramIndex != 0)
						{
							str.Append(", ");
						}
						if (localeCode == null || field.LocalisedName == null)
						{
							str.Append(field.FullName);
						}
						else
						{
							str.Append(field.LocalisedName);
							str.Append(localeCode);
							str.Append('`');
						}
						str.Append("=@p");
						str.Append(paramIndex);

						parameter = cmd.CreateParameter();
						parameter.ParameterName = "p" + paramIndex;
						parameter.Value = field.TargetField.GetValue(entity);
						cmd.Parameters.Add(parameter);
						paramIndex++;
					}

					// Add one of the two static where clauses:
					if (prevEdit.HasValue)
					{
						str.Append(" WHERE Id=@id AND EditedUtc=@eUtc");
						parameter = cmd.CreateParameter();
						parameter.ParameterName = "eUtc";
						parameter.Value = prevEdit.Value;
						cmd.Parameters.Add(parameter);
					}
					else
					{
						str.Append(" WHERE Id=@id");
					}

					cmd.CommandText = str.ToString();

					// Run update:
					if (await cmd.ExecuteNonQueryAsync() > 0)
					{
						return entity;
					}

					// It failed.
					return null;
				});

				service.EventGroup.Load.AddEventListener(async (Context context, T item, ID id) => {

					if (item != null)
					{
						return item;
					}

					item = await _database.Select<T, ID>(context, selectQuery, service.InstanceType, id);
					return item;
				});

				service.EventGroup.List.AddEventListener(async (Context context, QueryPair<T, ID> queryPair) => {

					if (queryPair.Handled)
					{
						return queryPair;
					}

					queryPair.Handled = true;

					// "Raw" results are as-is from the database.
					// That means the fields are not automatically filled in with the default locale when they're empty.
					var raw = (queryPair.QueryA.DataOptions & DataOptions.RawFlag) == DataOptions.RawFlag;

					// Get the results from the database:
					queryPair.Total = await _database.GetResults(context, queryPair, queryPair.OnResult, queryPair.SrcA, queryPair.SrcB, service.InstanceType, raw ? listRawQuery : listQuery);

					return queryPair;
				});

				// We have a thing we'll potentially need to reconfigure.
				if (_database == null)
				{
					Log.Warn("databasediff", "The type '" + service.ServicedType.Name + "' did not have its database schema updated because the database service was not up in time.");
				}
				else
				{
					await HandleDatabaseType(service);
				}
			}

		}
			
		/// <summary>
		/// Checks the DB version to see if we can auto handle schemas.
		/// </summary>
		/// <returns></returns>
		private async Task<bool> TryCheckVersion()
		{
			if (VersionCheckResult.HasValue)
			{
				return VersionCheckResult.Value;
			}

			// Get MySQL version:
			var versionQuery = Query.List(typeof(DatabaseVersion), nameof(DatabaseVersion));
			versionQuery.SetRawQuery("SELECT VERSION() as Version");

			DatabaseVersion dbVersion = null;
			var tryAgain = true;

			// This is the first db query that happens - if the database is not yet available we'll keep retrying until it is.
			while (tryAgain)
			{
				tryAgain = false;

				try
				{

					dbVersion = await _database.Select<DatabaseVersion, uint>(null, versionQuery, typeof(DatabaseVersion), 0);

				}
				catch (MySqlException e)
				{
					if (e.Code == 0)
					{
						Log.Warn("databasediff", e, "Authentication or unable to contact mysql. Trying again in 5 seconds.");
						await Task.Delay(5000);
						tryAgain = true;
					}
				}
			}

			// Get DB version:
			VersionText = dbVersion.Version;
			var version = VersionText.ToLower().Trim();

			var versionPieces = version.Split('-');

			if (!Version.TryParse(versionPieces[0], out Version parsedVersion))
			{
				Log.Warn("databasediff", "DatabaseDiff module disabled due to unrecognised MySQL version text. It was: " + version);
				VersionCheckResult = false;
				return false;
			}

			string variant = "base";

			if (versionPieces.Length > 2)
			{
				// It's a variant, like MariaDB
				variant = versionPieces[1];
			}

			Version minVersion = null;

			if (variant == "base")
			{
				minVersion = new Version(5, 7);
			}
			else if (variant == "mariadb")
			{
				minVersion = new Version(10, 1);
			}

			// Which version we got?
			if (minVersion == null)
			{
				Log.Warn("databasediff", "DatabaseDiff module disabled. Unrecognised MySQL variant: " + version);
				VersionCheckResult = false;
				return false;
			}
			else if (parsedVersion < minVersion)
			{
				Log.Warn("databasediff", "DatabaseDiff module disabled. You're using a version of MySQL that is too old. It's version: " + version);
				VersionCheckResult = false;
				return false;
			}
			
			VersionCheckResult = true;
			return true;
		}

		/// <summary>
		/// Loads the complete DB schema.
		/// </summary>
		/// <returns></returns>
		private async Task<Schema> LoadSchema()
		{
			if (CurrentDbSchema != null)
			{
				return CurrentDbSchema;
			}

			var existingSchema = new MySQLSchema();
			CurrentDbSchema = existingSchema;
			_database.Schema = CurrentDbSchema;

			List<DatabaseColumnDefinition> columns;

			try
			{
				// Collect all the existing table meta:
				var listQuery = Query.List(typeof(MySQLDatabaseColumnDefinition), nameof(MySQLDatabaseColumnDefinition));
				listQuery.SetRawQuery(
					"SELECT `data_type` as DataType, " +
					"IF(INSTR(extra, 'auto_increment')>0, TRUE, FALSE) as IsAutoIncrement, " +
					"CAST(IF(`character_maximum_length` IS NULL, `numeric_scale`, `character_maximum_length`) as SIGNED) as MaxCharacters, " +
					"CAST(`numeric_precision` as SIGNED) as MaxCharacters2, " +
					"IF(INSTR(column_type, 'unsigned')>0, TRUE, FALSE) as IsUnsigned, " +
					"table_name as TableName, `column_name` as ColumnName, `is_nullable` = 'YES' as IsNullable " +
					"FROM information_schema.columns WHERE table_schema = DATABASE()"
				);

				columns = await _database.List<DatabaseColumnDefinition>(null, listQuery, typeof(MySQLDatabaseColumnDefinition));
			}
			catch(Exception e)
			{
				Log.Warn("databasediff", e, "DatabaseDiff module disabled due to a failure during query execution. The version (which is supported) was: " + VersionText);
				VersionCheckResult = false;
				return null;
			}

			// group them by table:
			existingSchema.Add(columns);
			
			return existingSchema;
		}

		/// <summary>
		/// Sets up the table(s) for the given type.
		/// </summary>
		/// <param name="service"></param>
		/// <returns></returns>
		private async Task HandleDatabaseType(AutoService service)
		{
			if (!await TryCheckVersion())
			{
				return;
			}

			var existingSchema = await LoadSchema();
			
			if(existingSchema == null)
			{
				return;
			}
			
			var type = service.InstanceType;

			// New schema for this type:
			var newSchema = new MySQLSchema();

			// Get all its fields (including any sub fields).
			var fieldMap = service.FieldMap;

			// Invoke an event which can e.g. add additional columns or whole tables.
			fieldMap = await Events.DatabaseDiffBeforeAdd.Dispatch(new Context(), fieldMap, type, newSchema);

			if (fieldMap == null)
			{
				// Event handlers don't want this type to update.
				return;
			}

			foreach (var field in fieldMap.Fields)
			{
				// Add to schema:
				newSchema.AddColumn(field);
			}

			var tableDiff = existingSchema.Diff(newSchema);
			var altersToRun = new StringBuilder();

			foreach (var table in tableDiff.Added)
			{
				Log.Info("databasediff", "Creating table " + table.TableName);
				altersToRun.Append(table.CreateTableSql());
				altersToRun.Append(';');
			}

			foreach (var tableDiffs in tableDiff.Changed)
			{
				if (service.IsMapping)
				{
					// Special case for older mapping tables.
					// They need to be converted from {SourceType}Id,{TargetType}Id   to   SourceId,TargetId.
					// This change allows them to handle self referencing and also simplifies the caching/ objects themselves.

					// Check if we're adding SourceId/TargetId:
					DatabaseColumnDefinition claimed = null;

					for (var i = tableDiffs.Added.Count - 1; i >= 0; i--)
					{
						var newColumn = tableDiffs.Added[i];

						if (newColumn.ColumnName == "SourceId")
						{
							// If old source existed, mark this as a rename instead.
							var srcCol = existingSchema.GetColumn(newColumn.TableName, service.MappingSourceType.Name + "Id");

							if (srcCol != null && srcCol != claimed)
							{
								claimed = srcCol;// this claim prevents one column mapping tables (where src==targ type) from trying to rename the col twice.

								tableDiffs.Changed.Add(new ChangedColumn()
								{
									FromColumn = srcCol,
									ToColumn = newColumn
								});
								tableDiffs.Added[i] = null;
							}
						}
						else if (newColumn.ColumnName == "TargetId")
						{
							// If old target existed, mark this as a rename instead.
							var targCol = existingSchema.GetColumn(newColumn.TableName, service.MappingTargetType.Name + "Id");

							if (targCol != null && targCol != claimed)
							{
								claimed = targCol; // this claim prevents one column mapping tables (where src==targ type) from trying to rename the col twice.

								tableDiffs.Changed.Add(new ChangedColumn()
								{
									FromColumn = targCol,
									ToColumn = newColumn
								});
								tableDiffs.Added[i] = null;
							}
						}
					}
				}


				// Handle added columns:
				foreach (var newColumn in tableDiffs.Added)
				{
					if (newColumn == null)
					{
						continue;
					}
					Log.Info("databasediff", "Adding column " + newColumn.TableName + "." + newColumn.ColumnName);
					altersToRun.Append(((MySQLDatabaseColumnDefinition)newColumn).AlterTableSql());
					altersToRun.Append(';');

					// Add to existingSchema object:
					existingSchema.Add(newColumn);
				}

				// Changed columns that can't be automatically upgraded must be handled via manually specified upgrade objects.
				foreach (var changedColumn in tableDiffs.Changed)
				{
					// We'll attempt an auto upgrade of anything changing meta values (i.e. anything that is not actually changing type).
					var from = (MySQLDatabaseColumnDefinition)changedColumn.FromColumn;
					var to = (MySQLDatabaseColumnDefinition)changedColumn.ToColumn;

					if (from.DataType == to.DataType)
					{
						// This will fail (expectedly) if the change would result in data loss.
						Log.Info("databasediff", "Attempting to alter column  " + to.TableName + "." + to.ColumnName + ".");

						altersToRun.Append(to.AlterTableSql(true, from.ColumnName));
						altersToRun.Append(';');
					}
					else
					{
						// (No support for those upgrade objects yet)
						Log.Info("databasediff", "Manual column change required: '" + to.AlterTableSql(true) + "'");
					}

					// Update in the existing schema by performing a remove and then a re-add:
					existingSchema.Remove(changedColumn.FromColumn);
					existingSchema.Add(changedColumn.ToColumn);
				}

				foreach (var removedColumn in tableDiffs.Removed)
				{
					var colDef = (MySQLDatabaseColumnDefinition)removedColumn;
					Log.Info("databasediff", "Obsolete or otherwise ignored column: " + colDef.TableName + "." + colDef.ColumnName + "");
				}

			}

			// Run now:
			var queryToRun = altersToRun.ToString();

			if (queryToRun.Length > 0)
			{
				try
				{
					await _database.Run(queryToRun);
				}
				catch (MySqlException e)
				{
					// Skipping all MySQL errors - the ones here are "it already exists" errors.
					Log.Info("databasediff", e, "Skipping a MySQL error during database diff.");
				}
				catch (Exception e)
				{
					Log.Info("databasediff", e, "Skipping a general error during database diff.");
				}

				await Events.DatabaseDiffAfterAdd.Dispatch(new Context(), tableDiff);
			}

		}

	}
}
