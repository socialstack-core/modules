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
using Api.Users;
using Api.SocketServerLibrary;
using System.Reflection.Emit;
using System.Reflection;

namespace Api.BlockDatabase
{
	/// <summary>
	/// Connects the blockchain database to entity events and also checks if the schema needs an update.
	/// </summary>
	[EventListener]
	public class Init
	{
		
		/// <summary>
		/// Creates object differ method. Checks for field changes and writes them as transaction fields.
		/// The resulting func is (updateObject, originalObject, Writer) and it returns the # of fields that changed.
		/// </summary>
		public static Func<T, T, Writer, int> CreateObjectDiff<T, ID>(AutoService<T, ID> service)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// ()
			var dymMethod = new DynamicMethod("ObjectDiffer", typeof(int), new Type[] { typeof(T), typeof(T), typeof(Writer) }, true);
			var generator = dymMethod.GetILGenerator();

			var numberOfFields = generator.DeclareLocal(typeof(int)); // loc_0 = number of changed fields.

			var fieldInfo = service.GetContentFields();

			var writeCompressed = typeof(Writer).GetMethod(nameof(Writer.WriteInvertibleCompressed), new Type[] { typeof(ulong) });

			Console.WriteLine("ObjectDiff for " + typeof(T).Name);

			foreach (var field in fieldInfo.List)
			{
				if (field == null || field.Definition == null || 
					field.FieldInfo == null || field.Name == "Id" || 
					field.Name == "EditedUtc" || field.Name == "CreatedUtc")
				{
					// Id is never considered for diffs and EditedUtc/ CreatedUtc is derived exclusively from txn timestamps.
					continue;
				}

				var fieldId = field.Definition.Id;
				var noChange = generator.DefineLabel();

				// Need to consider nullables carefully:
				var nullableBase = Nullable.GetUnderlyingType(field.FieldType);

				if (nullableBase != null)
				{
					// Nullable field. Special case for comparisons.
					CompareNullables(generator, field.FieldInfo);

				}
				else
				{
					// Ceq comparison is fine:
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Ldfld, field.FieldInfo);
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldfld, field.FieldInfo);
					generator.Emit(OpCodes.Ceq); // 1 if the fields are the same
				}

				generator.Emit(OpCodes.Brtrue_S, noChange); // Skip doing things if the values were the same.
				{
					// This happens when the field values are different - a change is detected and needs to be written out to the txn.

					// Write field ID before:
					generator.Emit(OpCodes.Ldarg_2);
					generator.Emit(OpCodes.Ldc_I4, (int)fieldId);
					generator.Emit(OpCodes.Conv_U8);
					generator.Emit(OpCodes.Call, writeCompressed);

					// Write the field value to the writer, Ldarg_2:
					generator.Emit(OpCodes.Ldarg_0);
					generator.Emit(OpCodes.Ldarg_2);
					generator.Emit(OpCodes.Call, field.FieldWriterMethodInfo);

					// Write field ID after:
					generator.Emit(OpCodes.Ldarg_2);
					generator.Emit(OpCodes.Ldc_I4, (int)fieldId);
					generator.Emit(OpCodes.Conv_U8);
					generator.Emit(OpCodes.Call, writeCompressed);

					// numberOfFields = numberOfFields+1;
					generator.Emit(OpCodes.Ldloc, numberOfFields);
					generator.Emit(OpCodes.Ldc_I4_1);
					generator.Emit(OpCodes.Add);
					generator.Emit(OpCodes.Stloc, numberOfFields);
				}
				generator.MarkLabel(noChange);
			}

			// Return number of fields written.
			generator.Emit(OpCodes.Ldloc, numberOfFields);
			generator.Emit(OpCodes.Ret);

			return dymMethod.CreateDelegate<Func<T, T, Writer, int>>();
		}

		private static void CompareNullables(ILGenerator generator, FieldInfo fieldInfo)
		{
			var fieldType = fieldInfo.FieldType;
			var hasValueProperty = fieldType.GetProperty("HasValue").GetGetMethod();
			var valueOrDefaultProperty = fieldType.GetMethod("GetValueOrDefault", Array.Empty<Type>());
			
			// Load both addresses:
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldflda, fieldInfo);
			generator.Emit(OpCodes.Call, hasValueProperty);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldflda, fieldInfo);
			generator.Emit(OpCodes.Call, hasValueProperty);

			// Comparing their HasValue states first:
			generator.Emit(OpCodes.Ceq);

			// Values next:
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldflda, fieldInfo);
			generator.Emit(OpCodes.Call, valueOrDefaultProperty);

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldflda, fieldInfo);
			generator.Emit(OpCodes.Call, valueOrDefaultProperty);
			generator.Emit(OpCodes.Ceq);

			// If both HasValue == HasValue, and the Value matches, do nothing.
			generator.Emit(OpCodes.And);
		}

		/// <summary>
		/// Creates field writer method. Writes the fields of an object as transaction format fields.
		/// The resulting func is (objectToWrite, Writer) and it returns the # of fields that it wrote out (note that it's actually always a constant here).
		/// </summary>
		public static Func<T, Writer, int> CreateFieldWriter<T, ID>(AutoService<T, ID> service)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var dymMethod = new DynamicMethod("FieldWriter", typeof(int), new Type[] { typeof(T), typeof(Writer) }, true);
			var generator = dymMethod.GetILGenerator();

			var fieldInfo = service.GetContentFields();

			var writeCompressed = typeof(Writer).GetMethod(nameof(Writer.WriteInvertibleCompressed), new Type[] { typeof(ulong) });

			var fieldCount = 0;

			foreach (var field in fieldInfo.List)
			{
				if (field == null || field.Definition == null || 
					field.FieldInfo == null || field.Name == "Id" || 
					field.Name == "EditedUtc" || field.Name == "CreatedUtc")
				{
					// Id is never considered for diffs and EditedUtc/ CreatedUtc is derived exclusively from txn timestamps.
					continue;
				}

				fieldCount++;

				var fieldId = (int)field.Definition.Id;

				// Write field ID before:
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldc_I4, fieldId);
				generator.Emit(OpCodes.Conv_U8);
				generator.Emit(OpCodes.Call, writeCompressed);

				// Write the field value to the writer, Ldarg_1:
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Call, field.FieldWriterMethodInfo);

				// Write field ID after:
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldc_I4, fieldId);
				generator.Emit(OpCodes.Conv_U8);
				generator.Emit(OpCodes.Call, writeCompressed);
			}

			// Return number of fields written.
			generator.Emit(OpCodes.Ldc_I4, fieldCount);
			generator.Emit(OpCodes.Ret);

			return dymMethod.CreateDelegate<Func<T, Writer, int>>();
		}

		/// <summary>
		/// Sets up for the given type with its event group.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="database"></param>
		/// <param name="service"></param>
		public static async ValueTask SetupServiceHandlers<T, ID>(BlockDatabaseService database, AutoService<T, ID> service)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			await HandleDatabaseType(database, service);

			// Chain and definition is now set on service.


			var chain = service.Chain;
			var definition = service.Definition;

			if (service.GetCacheConfig() == null)
			{
				service.Cache();
			}
			
			service.EventGroup.Delete.AddEventListener(async (Context context, T entity) => {

				var result = await database.WriteArchived(service.ReverseId(entity.Id), definition, chain);

				if (!result.Valid)
				{
					return null;
				}
				
				return entity;
			});

			Func<T, T, Writer, int> _diff = null;
			Func<T, Writer, int> _createWriter = null;

			service.EventGroup.Update.AddEventListener(async (Context context, T entity, T originalEntity) => {
				
				var id = entity.Id;

				if (_diff == null)
				{
					// This objectDiff function omits Id and EditedUtc.
					_diff = CreateObjectDiff(service);
				}

				// Get the entity ID (reversed from ID to a ulong):
				var entityId = service.ReverseId(entity.Id);

				var result = await database.WriteDiff(entity, originalEntity, _diff, definition, chain, entityId);

				// Cache updates happen in response to the transaction occuring.

				if (!result.Valid)
				{
					return null;
				}

				return entity;
			});

			service.EventGroup.Create.AddEventListener(async (Context context, T entity) => {
				if (_createWriter == null)
				{
					_createWriter = CreateFieldWriter(service);
				}

				// Get ID as a ulong:
				var entityId = service.ReverseId(entity.Id);

				// If an explicit ID is provided, entityId is non-zero and it is written as well.
				var result = await database.Write(entity, _createWriter, definition, chain, entityId);

				if (!result.Valid)
				{
					return null;
				}

				return result.RelevantObject as T;
			});

			/*
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
			*/

			service.EventGroup.Load.AddEventListener((Context context, T item, ID id) => {

				var cache = service.GetCacheForLocale(context == null ? 1 : context.LocaleId);

				if (cache != null)
				{
					item = cache.Get(id);
				}

				return new ValueTask<T>(item);
			});

			service.EventGroup.List.AddEventListener(async (Context context, QueryPair<T, ID> queryPair) => {

				// Do we have a cache?
				var cache = (queryPair.QueryA.DataOptions & DataOptions.CacheFlag) == DataOptions.CacheFlag ? service.GetCacheForLocale(context.LocaleId) : null;

				if (cache != null)
				{
					// Great - we're using the cache:
					queryPair.Total = await cache.GetResults(context, queryPair, queryPair.OnResult, queryPair.SrcA, queryPair.SrcB);
				}

				return queryPair;
			});
		}
		
		/// <summary>
		/// Sets up the table(s) for the given type.
		/// </summary>
		/// <param name="database"></param>
		/// <param name="service"></param>
		/// <returns></returns>
		private static async Task HandleDatabaseType<T,ID>(BlockDatabaseService database, AutoService<T,ID> service)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var type = service.InstanceType;

			// Get all its fields (including any sub fields).
			var contentFields = service.GetContentFields();
			
			// Note: Blockchain engine does not currently trigger the DatabaseDiffBeforeAdd event to modify the schema.
			// That's because of the static way it generates field readers/ writers etc - it requires FieldInfo in order to load fields as efficiently as possible.

			// Next, match each column in the schema with fields in the chain.

			// First, which chain? Derive this from the attributes on the type.
			var dbFieldInfos = type.GetCustomAttributes<DatabaseFieldAttribute>();
			string tableGroup = null;

			if (dbFieldInfos != null)
			{
				foreach (var dbFieldInfo in dbFieldInfos)
				{
					if (dbFieldInfo.Group != null)
					{
						tableGroup = dbFieldInfo.Group;
						break;
					}
				}
			}

			if (!string.IsNullOrEmpty(tableGroup))
			{
				tableGroup = tableGroup.ToLower();
			}

			var chain = database.GetChain(Lumity.BlockChains.ChainType.Public);

			service.Chain = chain;

			// Table name is simply that of the entity:
			var tableName = service.EntityName;

			var tableDef = chain.FindDefinition(tableName);

			if (tableDef == null)
			{
				tableDef = await chain.Define(tableName);

				Console.WriteLine("Defined table '" + tableName + "'");
			}

			service.Definition = tableDef;

			// Ensure each field is defined:
			foreach (var col in contentFields.List)
			{
				if (col.FieldInfo == null || col.DataType == null || col.Definition != null)
				{
					// Either a non-chain stored field or it was defined during the cache load process.
					continue;
				}

				// Get the field definition or create it:
				var fieldDef = chain.FindField(col.Name, col.DataType);

				if (fieldDef == null)
				{
					fieldDef = await chain.DefineField(col.Name, col.DataType);
					Console.WriteLine("Defined field '" + col.Name + "' used by type '" + tableName + "'");
				}

				col.Definition = fieldDef;
			}

		}

	}
}
