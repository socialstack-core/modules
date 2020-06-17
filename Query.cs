using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Api.Permissions;

namespace Api.Database
{
	/// <summary>
	/// Extremely similar to WP Query. 
	/// Essentially an abstraction layer from the underlying database technology.
	/// Also functions as a place to cache any field mappings or other db handles for performance boosting.
	/// </summary>
	public class Query
	{
		/// <summary>
		/// INSERT queries.
		/// </summary>
		protected const int INSERT = 1;

		/// <summary>
		/// SELECT queries.
		/// </summary>
		protected const int SELECT = 2;

		/// <summary>
		/// UPDATE queries.
		/// </summary>
		protected const int UPDATE = 3;

		/// <summary>
		/// DELETE queries.
		/// </summary>
		protected const int DELETE = 4;

		/// <summary>
		/// REPLACE queries.
		/// </summary>
		protected const int REPLACE = 5;

		/// <summary>
		/// INSERT .. SELECT queries.
		/// </summary>
		protected const int COPY = 6;

		/// <summary>
		/// The operation to run (insert, select etc).
		/// </summary>
		protected int Operation;

		/// <summary>
		/// The field map for any fields that are being inserted/ updated etc.
		/// Either maps params to object fields or result row fields to object fields
		/// depending on what the query is being used for.
		/// </summary>
		public FieldMap Fields;

		/// <summary>
		/// Used when transferring fields between tables (typically a copy statement).
		/// </summary>
		public FieldTransferMap TransferMap;

		/// <summary>
		/// The WHERE filter on this query.
		/// </summary>
		public Filter _where;

		/// <summary>
		/// Primary table type.
		/// </summary>
		protected Type MainTableType;

		/// <summary>
		/// Primary table name.
		/// </summary>
		protected string MainTable;

		/// <summary>
		/// Primary table name.
		/// </summary>
		protected string MainTableAs;

		/// <summary>
		/// Id field to write out to.
		/// </summary>
		public FieldInfo IdField;

		/// <summary>The query to run (cached).</summary>
		protected string _query;

		/// <summary>
		/// Language specific variants of the cached query. Generated on demand.
		/// </summary>
		protected string[] _localisedQuery;

		/// <summary>
		/// Sets the MainTable and MainTableAs fields.
		/// </summary>
		/// <param name="type"></param>
		public Query SetMainTable(Type type)
		{
			return SetMainTable(type, type.TableName());
		}

		/// <summary>
		/// Sets the MainTable and MainTableAs fields.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name">The underlying table name.</param>
		public Query SetMainTable(Type type, string name)
		{
			MainTableType = type;
			MainTable = name;
			MainTableAs = name + " AS `" + type.Name + "`";
			return this;
		}
		
		/// <summary>
		/// True if this is an INSERT query.
		/// </summary>
		public bool IsInsert
		{
			get
			{
				return Operation == INSERT;
			}
		}

		/// <summary>
		/// True if this is an INSERT .. SELECT query.
		/// </summary>
		public bool IsCopy
		{
			get
			{
				return Operation == COPY;
			}
		}

		/// <summary>
		/// True if this is a SELECT query.
		/// </summary>
		public bool IsSelect
		{
			get
			{
				return Operation == SELECT;
			}
		}

		/// <summary>
		/// True if this is an UPDATE query.
		/// </summary>
		public bool IsUpdate
		{
			get
			{
				return Operation == UPDATE;
			}
		}

		/// <summary>
		/// True if this is a DELETE query.
		/// </summary>
		public bool IsDelete
		{
			get
			{
				return Operation == DELETE;
			}
		}

		/// <summary>
		/// Use Query.Select etc instead.
		/// </summary>
		internal Query() { }

		/// <summary>
		/// Use Query.Select etc instead.
		/// </summary>
		/// <param name="mainTableType"></param>
		internal Query(Type mainTableType) {
			SetMainTable(mainTableType);
		}

		/// <summary>
		/// Removes a field by its case-sensitive name.
		/// Returns it if it was removed.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Field RemoveField(string name)
		{
			return Fields.Remove(name);
		}

		/// <summary>
		/// Adds a new field to this query.
		/// </summary>
		/// <returns></returns>
		public void AddField(Field field)
		{
			Fields.Add(field);
		}
		
		/// <summary>
		/// Finds a field with the given name. Null if not found.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Field GetField(string name)
		{
			return Fields.Find(name);
		}

		/// <summary>
		/// Builds the underlying query to run.
		/// </summary>
		public string GetQuery(Filter filter = null, bool bulk = false, int localeId = 0, string localeCode = null, bool includeCount = false)
		{
			if (includeCount && (filter == null || Operation != SELECT))
			{
				throw new ArgumentException("You can only include the count on SELECT statements with a LIMIT.");
				// Otherwise just use the result count.
				// This check however exists to avoid a multitude of potential caching problems.
			}

			if (filter == null && !bulk)
			{
				if (localeId < 2)
				{
					if (_query != null)
					{
						return _query;
					}
				}
				else if(_localisedQuery != null && _localisedQuery[localeId - 2] != null)
				{
					// The query is similar 
					// except the fields typically have _localeCode on the end (e.g. _fr or _it).
					return _localisedQuery[localeId - 2];
				}
			}

			var str = new StringBuilder();
			int paramOffset = 0;
			int fromLocation = 0;

			switch (Operation)
			{
				case SELECT:
					str.Append("SELECT ");
					// List of fields next!
					for (var i = 0; i < Fields.Count; i++)
					{
						var field = Fields[i];
						if (i != 0)
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
					}

					fromLocation = str.Length;
					str.Append(" FROM ");
					str.Append(MainTableAs);
					break;
				case DELETE:
					str.Append("DELETE `");
					str.Append(MainTableType.Name);
					str.Append("` FROM ");
					str.Append(MainTableAs);

					if (bulk)
					{
						str.Append("WHERE Id ");
						return str.ToString();
					}
					break;
				case UPDATE:
					str.Append("UPDATE ");
					str.Append(MainTableAs);
					str.Append(" SET ");

					// List of parametric fields next!
					for (var i = 0; i < Fields.Count; i++)
					{
						var field = Fields[i];
						if (i != 0)
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
						str.Append(i);
					}

					paramOffset = Fields.Count;

					break;
				case REPLACE:
					// Single or multiple row replace.
					str.Append("REPLACE INTO ");
					str.Append(MainTable);
					str.Append('(');

					// List of field names next!
					for (var i = 0; i < Fields.Count; i++)
					{
						if (i != 0)
						{
							str.Append(", ");
						}
						str.Append('`');
						str.Append(Fields[i].Name);
						str.Append('`');
					}
					if (bulk)
					{
						str.Append(") VALUES ");
					}
					else
					{
						str.Append(") VALUES (");
						// And list the parameter set:
						for (var i = 0; i < Fields.Count; i++)
						{
							if (i != 0)
							{
								str.Append(", ");
							}
							str.Append('?');
						}
						str.Append(")");
					}

					if (_where != null || filter != null)
					{
						throw new Exception("Where isn't permitted on replace statements");
					}
					break;
				case COPY:

					// Insert..Select query.
					str.Append("INSERT INTO ");
					str.Append(TransferMap.TargetType.TableName());
					if (TransferMap.TargetTypeNameExtension != null)
					{
						str.Append(TransferMap.TargetTypeNameExtension);
					}
					str.Append('(');

					// List of field names next!
					for (var i = 0; i < TransferMap.Transfers.Count; i++)
					{
						if (i != 0)
						{
							str.Append(", ");
						}
						str.Append('`');
						str.Append(TransferMap.Transfers[i].To.Name);
						str.Append('`');
					}

					str.Append(") SELECT ");

					// List of fields next!
					for (var i = 0; i < TransferMap.Transfers.Count; i++)
					{
						var transfer = TransferMap.Transfers[i];
						
						if (i != 0)
						{
							str.Append(", ");
						}
						
						if(transfer.IsConstant)
						{
							if(transfer.Constant == null){
								str.Append("NULL");
							}else if(transfer.Constant is string){
								str.Append('"');
								str.Append(MySql.Data.MySqlClient.MySqlHelper.EscapeString((string)transfer.Constant));
								str.Append('"');
							}else{
								// True, false, int etc.
								str.Append(transfer.Constant.ToString());
							}
							str.Append(" AS `");
							str.Append(transfer.To.Name);
							str.Append('`');
						}
						else
						{
							str.Append('`');
							str.Append(transfer.From.Name);
							str.Append("` AS `");
							str.Append(transfer.To.Name);
							str.Append('`');
						}
					}

					str.Append(" FROM ");
					str.Append(TransferMap.SourceType.TableName());
					str.Append(" AS `");
					str.Append(TransferMap.SourceType.Name);
					str.Append('`');
					if (TransferMap.SourceTypeNameExtension != null)
					{
						str.Append(TransferMap.SourceTypeNameExtension);
					}

					break;
				case INSERT:
					// Single or multiple row insert.
					str.Append("INSERT INTO ");
					str.Append(MainTable);
					str.Append('(');

					// List of field names next!
					for (var i = 0; i < Fields.Count; i++)
					{
						if (i != 0)
						{
							str.Append(", ");
						}
						str.Append('`');
						str.Append(Fields[i].Name);
						str.Append('`');
					}
					if (bulk)
					{
						str.Append(") VALUES ");
					}
					else
					{
						str.Append(") VALUES (");
						// And list the parameter set:
						for (var i = 0; i < Fields.Count; i++)
						{
							if (i != 0)
							{
								str.Append(", ");
							}
							str.Append('?');
						}
						str.Append(")");
					}

					if (_where != null || filter != null)
					{
						throw new Exception("Where isn't permitted on insert statements");
					}
					break;
			}

			string result;

			if (includeCount)
			{
				// SELECT .. FROM .. WHERE .. first:
				filter.BuildWhereQuery(str, paramOffset, localeCode);

				// Bake to a string as we're mostly interested in the WHERE part:
				var whereSegment = str.ToString();

				// Clear the str builder and start constructing the next one:
				str.Clear();
				str.Append("SELECT COUNT(*) as Count ");
				str.Append(whereSegment.Substring(fromLocation));
				str.Append(';');
				str.Append(whereSegment);
				filter.BuildOrderLimitQuery(str, paramOffset, localeCode);
				result = str.ToString();
			}
			else
			{

				if (filter != null)
				{
					filter.BuildFullQuery(str, paramOffset, localeCode);
				}
				else if (_where != null)
				{
					_where.BuildFullQuery(str, paramOffset, localeCode);
				}

				result = str.ToString();
			}

			if (!bulk && filter == null)
			{
				if (localeId < 2)
				{
					_query = result;
				}
				else
				{
					// Cache it in the localised set of strings:
					var minSize = localeId + 5;
					if (_localisedQuery == null)
					{
						_localisedQuery = new string[minSize];
					}
					else if (minSize > _localisedQuery.Length)
					{
						// Resize it:
						var newLQ = new string[minSize];
						Array.Copy(_localisedQuery, 0, newLQ, 0, _localisedQuery.Length);
						_localisedQuery = newLQ;
					}

					_localisedQuery[localeId - 2] = result;
				}
			}

			return result;
		}

		/// <summary>
		/// Generates an insert into {target} {target fields from the map} select {mapped fields} from {source} query.
		/// </summary>
		public static Query Copy(FieldTransferMap map)
		{
			var result = new Query
			{
				Operation = COPY,
				TransferMap = map
			};
			return result;
		}

		/// <summary>
		/// Generates a select * from rowType.TableName() query.
		/// Defaults to using where Id=? unless you use a custom where override.
		/// </summary>
		public static Query Select(Type rowType)
		{
			var result = List(rowType);
			result.Where().EqualsArg(rowType, "Id", 0);
			return result;
		}

		/// <summary>
		/// Generates a select * from rowType.TableName() query.
		/// Defaults to using where Id=? unless you use a custom where override.
		/// </summary>
		public static Query<U> Select<U>()
		{
			var result = List<U>();
			result.Where().EqualsArg("Id", 0);
			return result;
		}

		/// <summary>
		/// The same as select, only it doesn't use a where constraint.
		/// </summary>
		public static Query List(Type rowType)
		{
			var result = new Query(rowType)
			{
				Operation = SELECT,

				// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
				// Note that these maps aren't shared between queries so the fields can be removed etc from them.
				Fields = new FieldMap(rowType)
			};

			return result;
		}

		/// <summary>
		/// Start building a custom WHERE filter.
		/// </summary>
		/// <returns></returns>
		public Filter Where()
		{
			var result = new Filter
			{
				DefaultType = MainTableType
			};
			_where = result;
			return result;
		}

		/// <summary>
		/// The same as select, only it doesn't use a where constraint.
		/// </summary>
		public static Query<U> List<U>()
		{
			var result = new Query<U>
			{
				Operation = SELECT
			};
			result.SetMainTable(typeof(U));

			// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
			// Note that these maps aren't shared between queries so the fields can be removed etc from them.
			result.Fields = new FieldMap(typeof(U));

			return result;
		}

		/// <summary>
		/// Generates a replace into rowType.TableName() (field,field..) values(?,?,?..) query.
		/// Type should be a DatabaseRow derived type. The type of data that will be getting inserted.
		/// </summary>
		public static Query Replace(Type rowType)
		{
			var result = new Query(rowType)
			{
				Operation = REPLACE,

				// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
				// Note that these maps aren't shared between queries so the fields can be removed etc from them.
				Fields = new FieldMap(rowType)
			};
			return result;
		}

		/// <summary>
		/// Generates a replace into rowType.TableName() (field,field..) values(?,?,?..) query.
		/// Type should be a DatabaseRow derived type. The type of data that will be getting inserted.
		/// </summary>
		public static Query<U> Replace<U>()
		{
			var result = new Query<U>
			{
				Operation = REPLACE
			};
			result.SetMainTable(typeof(U));

			// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
			// Note that these maps aren't shared between queries so the fields can be removed etc from them.
			result.Fields = new FieldMap(typeof(U));
			return result;
		}

		/// <summary>
		/// Generates a insert into rowType.TableName() (field,field..) values(?,?,?..) query.
		/// rowType should be a DatabaseRow derived type. The type of data that will be getting inserted.
		/// </summary>
		public static Query Insert(Type rowType)
		{
			var result = new Query(rowType);
			SetupInsert(result, rowType);
			return result;
		}

		/// <summary>
		/// Generates a insert into rowType.TableName() (field,field..) values(?,?,?..) query.
		/// Type should be a DatabaseRow derived type. The type of data that will be getting inserted.
		/// </summary>
		public static Query<U> Insert<U>()
		{
			var result = new Query<U>();
			SetupInsert(result, typeof(U));
			return result;
		}

		private static void SetupInsert(Query result, Type mainType)
		{
			result.Operation = INSERT;
			result.SetMainTable(mainType);

			// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
			// Note that these maps aren't shared between queries so the fields can be removed etc from them.
			result.Fields = new FieldMap(mainType);

			// Discover if the type has an explicit ID or not.
			// It will do if it has turned off auto-inc on the Id, which is done via setting the attrib on the class itself.
			// So, lets get that:
			bool explicitId = false;

			var metaAttribs = mainType.GetCustomAttributes(typeof(DatabaseFieldAttribute), true);

			if (metaAttribs.Length > 0)
			{
				var classMeta = (DatabaseFieldAttribute)metaAttribs[0];

				if (classMeta != null && !classMeta.AutoIncrement)
				{
					// It's explicitly turning off auto-inc.
					// This class will require an explicit ID during inserts.
					explicitId = true;
				}

			}

			if (!explicitId)
			{
				// Remove "Id" field:
				var idField = result.RemoveField("Id");

				if (idField != null)
				{
					result.IdField = idField.TargetField;
				}
			}

		}

		/// <summary>
		/// Generates an update rowType.TableName() set field=?, field=?.. where.. query.
		/// Type should be a DatabaseRow derived type. The type of data that will be getting updated.
		/// </summary>
		public static Query<U> Update<U>()
		{
			var result = new Query<U>
			{
				Operation = UPDATE
			};
			result.SetMainTable(typeof(U));

			// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
			// Note that these maps aren't shared between queries so the fields can be removed etc from them.
			result.Fields = new FieldMap(typeof(U));

			// Remove "Id" field:
			result.RemoveField("Id");

			// Default where:
			result.Where().EqualsArg("Id", 0);
			return result;
		}

		/// <summary>
		/// Generates a delete from rowType.TableName() query.
		/// Defaults to using where Id=? unless you use a custom where override.
		/// Type should be a DatabaseRow derived type. The type of data that will be getting deleted.
		/// </summary>
		public static Query<U> Delete<U>()
		{
			var result = new Query<U>
			{
				Operation = DELETE
			};
			result.SetMainTable(typeof(U));

			// Default where:
			result.Where().EqualsArg("Id", 0);
			return result;
		}

	}

	/// <summary>
	/// Extremely similar to WP Query. 
	/// Essentially an abstraction layer from the underlying database technology.
	/// Also functions as a place to cache any field mappings or other db handles for performance boosting.
	/// </summary>
	public class Query<T> : Query
	{

		/// <summary>
		/// Sets the MainTable and MainTableAs fields.
		/// </summary>
		public Query SetMainTable()
		{
			return SetMainTable(typeof(T), typeof(T).TableName());
		}

		/// <summary>
		/// Sets the MainTable and MainTableAs fields.
		/// </summary>
		/// <param name="name">The underlying table name.</param>
		public Query<T> SetMainTable(string name)
		{
			MainTableType = typeof(T);
			MainTable = name;
			MainTableAs = name + " AS `" + typeof(T).Name + "`";
			return this;
		}
		
		/// <summary>
		/// Start building a custom WHERE filter.
		/// </summary>
		/// <returns></returns>
		public new Filter<T> Where()
		{
			var result = new Filter<T>();
			_where = result;
			return result;
		}

		/// <summary>
		/// Adds a "WHERE fieldName IS NULL".
		/// </summary>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		public Query<T> WhereNull(string fieldName)
		{
			Where().Equals(fieldName, null);
			return this;
		}

		/// <summary>
		/// Sets a raw query to run. Avoid using this when possible.
		/// </summary>
		/// <param name="qry"></param>
		public void SetRawQuery(string qry)
		{
			_query = qry;
		}

		/// <summary>
		/// Adds a "WHERE fieldName IS NOT NULL".
		/// </summary>
		public Query<T> WhereNotNull(string fieldName)
		{
			Where().Not().Equals(fieldName, null);
			return this;
		}
		
		/// <summary>
		/// Adds a "WHERE fieldName=valueOrArg".
		/// </summary>
		public Query<T> WhereEquals(string fieldName, string valueOrArg)
		{
			Where().Equals(fieldName, valueOrArg);
			return this;
		}
		
		/// <summary>
		/// Remove all fields except the named ones.
		/// </summary>
		/// <param name="fieldNames"></param>
		public Query<T> RemoveAllBut(params string[] fieldNames)
		{
			Fields.RemoveAllBut(fieldNames);
			return this;
		}
		
	}
	
}