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
		/// The WHERE filter on this query.
		/// </summary>
		public Filter _where;

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
		protected void SetMainTable(Type type)
		{
			MainTable = type.TableName();
			MainTableAs = MainTable + " AS `" + type.Name + "`";
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
		/// Type should be a DatabaseRow derived type. The type of data that will be getting inserted.
		/// </summary>
		public static Query<U> Insert<U>()
		{

			var result = new Query<U>();
			result.Operation = INSERT;
			result.SetMainTable(typeof(U));

			// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
			// Note that these maps aren't shared between queries so the fields can be removed etc from them.
			result.Fields = new FieldMap(typeof(U));

			// Discover if the type has an explicit ID or not.
			// It will do if it has turned off auto-inc on the Id, which is done via setting the attrib on the class itself.
			// So, lets get that:
			bool explicitId = false;

			var metaAttribs = typeof(U).GetCustomAttributes(typeof(DatabaseFieldAttribute), true);

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

			return result;
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
		/// Start building a custom WHERE filter.
		/// </summary>
		/// <returns></returns>
		public Filter<T> Where()
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
		/// Remove all fields except the named ones.
		/// </summary>
		/// <param name="fieldNames"></param>
		public Query<T> RemoveAllBut(params string[] fieldNames)
		{
			Fields.RemoveAllBut(fieldNames);
			return this;
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
					for (var i = 0; i < Fields.Count; i++) {
						if (i != 0) {
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
					for (var i = 0; i < Fields.Count; i++) {
						if (i != 0) {
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
				case INSERT:
					// Single or multiple row insert.
					str.Append("INSERT INTO ");
					str.Append(MainTable);
					str.Append('(');

					// List of field names next!
					for (var i = 0; i < Fields.Count; i++) {
						if (i != 0) {
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

					if (_where != null || filter != null) {
						throw new Exception("Where isn't permitted on insert statements");
					}
					break;
			}
			
			if (filter != null)
			{
				filter.BuildFullQuery(str, paramOffset);
			}
			else if (_where != null)
			{
				_where.BuildFullQuery(str, paramOffset);
			}
			
			var result = str.ToString();

			if (!bulk && filter == null)
			{
				_query = result;
			}

			return result;
		}
	}
	
}