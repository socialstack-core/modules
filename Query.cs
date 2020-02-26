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
		public string GetQuery(Filter filter = null, bool bulk = false)
		{
			if (filter == null && !bulk && _query != null)
			{
				return _query;
			}

			var str = new StringBuilder();
			int paramOffset = 0;

			switch (Operation)
			{
				case SELECT:
					str.Append("SELECT ");
					// List of fields next!
					for (var i = 0; i < Fields.Count; i++)
					{
						if (i != 0)
						{
							str.Append(", ");
						}
						str.Append(Fields[i].FullName);
					}

					str.Append(" FROM ");
					str.Append(MainTableAs);
					break;
				case DELETE:
					str.Append("DELETE FROM ");
					str.Append(MainTable);

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
						if (i != 0)
						{
							str.Append(", ");
						}
						str.Append(Fields[i].FullName);
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
						if (i != 0)
						{
							str.Append(", ");
						}
						str.Append('`');
						str.Append(TransferMap.Transfers[i].From.Name);
						str.Append("` AS `");
						str.Append(TransferMap.Transfers[i].To.Name);
						str.Append('`');
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

			if (filter != null)
			{
				filter.BuildFullQuery(str, paramOffset, Operation == DELETE);
			}
			else if (_where != null)
			{
				_where.BuildFullQuery(str, paramOffset, Operation == DELETE);
			}

			var result = str.ToString();

			if (!bulk && filter == null)
			{
				_query = result;
			}

			return result;
		}

		/// <summary>
		/// Generates an insert into {target} {target fields from the map} select {mapped fields} from {source} query.
		/// </summary>
		public static Query Copy(FieldTransferMap map)
		{
			var result = new Query();
			result.Operation = COPY;
			result.TransferMap = map;
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
			var result = new Query(rowType);
			result.Operation = SELECT;

			// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
			// Note that these maps aren't shared between queries so the fields can be removed etc from them.
			result.Fields = new FieldMap(rowType);

			return result;
		}

		/// <summary>
		/// Start building a custom WHERE filter.
		/// </summary>
		/// <returns></returns>
		public Filter Where()
		{
			var result = new Filter();
			result.DefaultType = MainTableType;
			_where = result;
			return result;
		}

		/// <summary>
		/// The same as select, only it doesn't use a where constraint.
		/// </summary>
		public static Query<U> List<U>()
		{
			var result = new Query<U>();
			result.Operation = SELECT;
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
			var result = new Query(rowType);
			result.Operation = REPLACE;

			// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
			// Note that these maps aren't shared between queries so the fields can be removed etc from them.
			result.Fields = new FieldMap(rowType);
			return result;
		}

		/// <summary>
		/// Generates a replace into rowType.TableName() (field,field..) values(?,?,?..) query.
		/// Type should be a DatabaseRow derived type. The type of data that will be getting inserted.
		/// </summary>
		public static Query<U> Replace<U>()
		{
			var result = new Query<U>();
			result.Operation = REPLACE;
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
			var result = new Query<U>();
			result.Operation = UPDATE;
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
			var result = new Query<U>();
			result.Operation = DELETE;
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