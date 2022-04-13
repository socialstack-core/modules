using System;
using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using System.Threading.Tasks;
using Api.Translate;
using System.Collections.Generic;
using System.Text;
using Api.Permissions;
using Api.Database;

namespace Api.BlockDatabase
{
	/// <summary>
	/// Connects the blockchain database to entity events and also checks if the schema needs an update.
	/// </summary>
	[EventListener]
	public class Init
	{
		private BlockDatabaseService _database;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Init()
		{
			var setupHandlersMethod = GetType().GetMethod(nameof(SetupServiceHandlers));

			// Add handler for the initial locale list:
			Events.Locale.InitialList.AddEventListener(async (Context context, List<Locale> locales) => {

				if (_database == null)
				{
					_database = Services.Get<BlockDatabaseService>();
				}

				// Get the def:
				var definition = _database.GetDefinition(typeof(Locale), Lumity.BlockChains.ChainType.Public);

				if (definition == null)
				{
					locales = new List<Locale>();
				}
				else
				{
#warning todo!
					locales = new List<Locale>();  // await _database.List<Locale>(new Context(), definition, typeof(Locale));
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

			Events.Service.AfterCreate.AddEventListener(async (Context context, AutoService service) => {

				if (service == null || service.ServicedType == null)
				{
					return service;
				}

				if (_database == null)
				{
					_database = Services.Get<BlockDatabaseService>();
				}

				// If type derives from DatabaseRow, we have a thing we'll potentially need to reconfigure.
				if (ContentTypes.IsAssignableToGenericType(service.ServicedType, typeof(Content<>)))
				{
					if (_database == null)
					{
						Console.WriteLine("[WARN] The type '" + service.ServicedType.Name + "' did not have its database schema updated because the database service was not up in time.");
						return service;
					}

					await HandleDatabaseType(service);

					var servicedType = service.ServicedType;

					// Add data load events:
					var setupType = setupHandlersMethod.MakeGenericMethod(new Type[] {
						servicedType,
						service.IdType
					});

					setupType.Invoke(this, new object[] {
						service
					});
				}

				// Service can now attempt to load its cache:
				await service.SetupCacheIfNeeded();

				return service;
			}, 2);
			
		}

		/// <summary>
		/// Sets up for the given type with its event group.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="service"></param>
		public void SetupServiceHandlers<T, ID>(AutoService<T, ID> service)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var isDbStored = service.DataIsPersistent;
			var blockTableMeta = _database.GetTableMeta(service.InstanceType);

			if (service.GetCacheConfig() == null)
			{
				service.Cache();
			}
			
			service.EventGroup.Delete.AddEventListener(async (Context context, T result) => {

#warning todo deletion
				/*
				// Delete the entry:
				if (isDbStored)
				{
					await _database.RunWithId(context, deleteQuery, result.Id);
				}

				var cache = service.GetCacheForLocale(context == null ? 1 : context.LocaleId);

				if (cache != null)
				{
					cache.Remove(context, result.GetId());
				}
				*/

				return result;
			});

			service.EventGroup.Update.AddEventListener(async (Context context, T entity) => {
				
				var id = entity.Id;

				T raw = null;

				var locale = context == null ? 1 : context.LocaleId;
				var cache = service.GetCacheForLocale(locale);

				if (locale == 1)
				{
					raw = entity;
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
						primaryEntity = await service.Get(new Context(1, context.User, context.RoleId), id);
					}
					else
					{
						primaryEntity = service.GetCacheForLocale(1).Get(id);
					}

					service.PopulateRawEntityFromTarget(raw, entity, primaryEntity);
				}
				
				if (isDbStored)
				{
					_database.Write(entity, blockTableMeta);
				}

				if (cache != null)
				{
					cache.Add(context, entity, raw);

					if (locale == 1)
					{
						service.OnPrimaryEntityChanged(entity);
					}

				}
				
				return entity;
			});

			service.EventGroup.Create.AddEventListener(async (Context context, T entity) => {

				if (isDbStored)
				{
					if (!entity.GetId().Equals(default(ID)))
					{
						// Explicit ID has been provided.
						_database.Write(entity, blockTableMeta);
					}
					else
					{
						#warning todo omit ID field
						_database.Write(entity, blockTableMeta);
					}
				}

				return entity;
			});

			service.EventGroup.CreatePartial.AddEventListener((Context context, T raw) => {
				var cache = service.GetCacheForLocale(context == null ? 1 : context.LocaleId);

				if (cache != null)
				{
					T entity;

					if (context.LocaleId == 1)
					{
						// Primary locale. Entity == raw entity, and no transferring needs to happen. This is expected to always be the case for creations.
						entity = raw;
					}
					else
					{
						// Get the 'real' (not raw) entity from the cache. We'll copy the fields from the raw object to it.
						entity = new T();

						// Transfer fields from raw to entity, using the primary object as a source of blank fields:
						service.PopulateTargetEntityFromRaw(entity, raw, null);
					}

					cache.Add(context, entity, raw);
				}
				
				return new ValueTask<T>(raw);
			});

			service.EventGroup.Load.AddEventListener(async (Context context, T item, ID id) => {

				var cache = service.GetCacheForLocale(context == null ? 1 : context.LocaleId);

				if (cache != null)
				{
					item = cache.Get(id);
				}

				if (item == null && isDbStored)
				{
					item = _database.GetResult<T, ID>(context, id, service.InstanceType, blockTableMeta);
				}

				return item;
			});

			service.EventGroup.List.AddEventListener(async (Context context, QueryPair<T, ID> queryPair) => {

				// Do we have a cache?
				var cache = (queryPair.QueryA.DataOptions & DataOptions.CacheFlag) == DataOptions.CacheFlag ? service.GetCacheForLocale(context.LocaleId) : null;

				if (cache != null)
				{
					// Great - we're using the cache:
					queryPair.Total = await cache.GetResults(context, queryPair, queryPair.OnResult, queryPair.SrcA, queryPair.SrcB);
				}
				else if (isDbStored)
				{
					// "Raw" results are as-is from the database.
					// That means the fields are not automatically filled in with the default locale when they're empty.
					var raw = (queryPair.QueryA.DataOptions & DataOptions.RawFlag) == DataOptions.RawFlag;

					// Get the results from the database:
					queryPair.Total = _database.GetResults(context, queryPair, service.InstanceType, blockTableMeta);
				}

				return queryPair;
			});
		}
		
		/// <summary>
		/// Sets up the table(s) for the given type.
		/// </summary>
		/// <param name="service"></param>
		/// <returns></returns>
		private async Task HandleDatabaseType(AutoService service)
		{
			var type = service.InstanceType;

			// New schema for this type:
			var newSchema = new BlockSchema();

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
				newSchema.AddColumn(field, type);
			}

			// Next, match each column in the schema with fields in the chain.

			foreach (var kvp in newSchema.Tables)
			{
				// First, which chain?
				var tableGroup = kvp.Value.GetGroupName();

				if (!string.IsNullOrEmpty(tableGroup))
				{
					tableGroup = tableGroup.ToLower();
				}

				var chain = tableGroup == "host" ? 
					_database.GetChain(Lumity.BlockChains.ChainType.PublicHost) : 
					_database.GetChain(Lumity.BlockChains.ChainType.Public);

				// Get or define:
				var tableName = kvp.Value.TableName;

				var tableDef = chain.FindOrDefine(tableName, out bool wasTableDefined);

				if (wasTableDefined)
				{
					Console.WriteLine("Defined table '" + tableName + "'");
				}

				// Create db meta for the system type:
				BlockTableMeta tableMeta = null;

				foreach (var col in kvp.Value.Columns)
				{
					var column = col.Value as BlockDatabaseColumnDefinition;

					if (tableMeta == null)
					{
						tableMeta = _database.CreateTableMeta(tableDef, column.TableType);
					}

					// Get the field definition or create it:
					var fieldDef = chain.FindOrDefineField(column.ColumnName, column.DataType, out bool wasDefined);

					if (wasDefined)
					{
						Console.WriteLine("Defined field '" + column.ColumnName + "' used by type '" + column.TableName + "'");
					}

					tableMeta.AddField(column, fieldDef);
				}

				if (tableMeta != null)
				{
					tableMeta.Chain = chain;

					// Generate the writer for create calls now.
					tableMeta.Completed();
				}
			}

		}

	}
}
