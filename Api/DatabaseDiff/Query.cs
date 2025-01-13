using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Api.Permissions;
using Api.SocketServerLibrary;
using MySql.Data.MySqlClient;
using Api.Contexts;

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
		/// DELETE queries.
		/// </summary>
		protected const int DELETE = 4;

		/// <summary>
		/// INSERT .. SELECT queries.
		/// </summary>
		protected const int COPY = 6;

		/// <summary>
		/// The operation to run (insert, select etc).
		/// </summary>
		protected int Operation;

		/// <summary>
		/// True if this query gets the 'raw' object or not.
		/// The raw object is a localised version, exactly as-is from the database.
		/// </summary>
		public bool Raw;

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
		/// A custom WHERE .. part of this query.
		/// </summary>
		public string _where;

		/// <summary>
		/// Primary table name.
		/// </summary>
		protected string MainTable;
		
		/// <summary>
		/// Primary table name.
		/// </summary>
		protected string MainEntity;

		/// <summary>
		/// Primary table name with AS followed by the C# entity name.
		/// </summary>
		public string MainTableAs;

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
		/// <param name="entityName">The entity name.</param>
		public Query SetMainTable(string entityName)
		{
			var name = MySQLSchema.TableName(entityName);

			MainEntity = entityName;
			MainTable = name;
			MainTableAs = name + " AS `" + entityName + "`";
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
		/// <param name="entityName"></param>
		internal Query(string entityName) {
			SetMainTable(entityName);
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
		/// Builds the underlying query to run. QueryPair must be fully populated.
		/// </summary>
		public void ApplyQuery<T,ID>(Context context, MySqlCommand cmd, QueryPair<T,ID> qP, bool bulk = false, uint localeId = 0, string localeCode = null, bool includeCount = false)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var filterEmpty = qP.QueryA.Empty && qP.QueryB.Empty && qP.QueryA.PageSize == 0 && qP.QueryA.Offset == 0 && qP.QueryA.SortField == null;

			if (includeCount && (filterEmpty || Operation != SELECT))
			{
				throw new ArgumentException("You can only include the count on SELECT statements with a LIMIT.");
				// Otherwise just use the result count.
				// This check however exists to avoid a multitude of potential caching problems.
			}

			if (filterEmpty && !bulk)
			{
				if (localeId < 2)
				{
					if (_query != null)
					{
						cmd.CommandText = _query;
						return;
					}
				}
				else if (_localisedQuery != null && (localeId - 2) < _localisedQuery.Length && _localisedQuery[localeId - 2] != null)
				{
					// The query is similar 
					// except the fields typically have _localeCode on the end (e.g. _fr or _it).
					cmd.CommandText = _localisedQuery[localeId - 2];
					return;
				}
			}

			var str = Writer.GetPooled();
			str.Start(null);
			int fromLocation = 0;

			switch (Operation)
			{
				case SELECT:
					str.WriteS("SELECT ");
					// List of fields next!
					for (var i = 0; i < Fields.Count; i++)
					{
						var field = Fields[i];
						if (i != 0)
						{
							str.WriteS(", ");
						}
						if (localeCode == null || field.LocalisedName == null)
						{
							str.WriteS(field.FullName);
						}
						else if (Raw || field.IsPrice)
						{
							// Using this will result in a 'raw' object being returned.
							str.WriteS(field.LocalisedName);
							str.WriteS(localeCode);
							str.Write((byte)'`');
						}
						else
						{
							// Of the form if(FIELD_locale is null,FIELD,FIELD_locale)");
							str.WriteS("if(");
							str.WriteS(field.LocalisedName);
							str.WriteS(localeCode);
							str.WriteS("` is null,");
							str.WriteS(field.FullName);
							str.Write((byte)',');
							str.WriteS(field.LocalisedName);
							str.WriteS(localeCode);
							str.WriteS("`)");
						}
					}

					fromLocation = str.Length;
					str.WriteS(" FROM ");
					str.WriteS(MainTableAs);
					break;
				case DELETE:
					str.WriteS("DELETE `");
					str.WriteS(MainEntity);
					str.WriteS("` FROM ");
					str.WriteS(MainTableAs);

					if (bulk)
					{
						str.WriteS("WHERE Id ");
						var res = str.ToUTF8String();
						str.Release();
						cmd.CommandText = res;
						return;
					}
					break;
				case COPY:

					// Insert..Select query.
					str.WriteS("INSERT INTO ");
					str.WriteS(MySQLSchema.TableName(TransferMap.TargetEntityName));
					if (TransferMap.TargetTypeNameExtension != null)
					{
						str.WriteS(TransferMap.TargetTypeNameExtension);
					}
					str.Write((byte)'(');

					// List of field names next!
					for (var i = 0; i < TransferMap.Transfers.Count; i++)
					{
						if (i != 0)
						{
							str.WriteS(", ");
						}
						str.Write((byte)'`');
						str.WriteS(TransferMap.Transfers[i].To.Name);
						str.Write((byte)'`');
					}

					str.WriteS(") SELECT ");

					// List of fields next!
					for (var i = 0; i < TransferMap.Transfers.Count; i++)
					{
						var transfer = TransferMap.Transfers[i];

						if (i != 0)
						{
							str.WriteS(", ");
						}

						if (transfer.IsConstant)
						{
							if (transfer.Constant == null)
							{
								str.WriteS("NULL");
							}
							else if (transfer.Constant is string)
							{
								str.Write((byte)'"');
								str.WriteS(MySql.Data.MySqlClient.MySqlHelper.EscapeString((string)transfer.Constant));
								str.Write((byte)'"');
							}
							else
							{
								// True, false, int etc.
								str.WriteS(transfer.Constant.ToString());
							}
							str.WriteS(" AS `");
							str.WriteS(transfer.To.Name);
							str.Write((byte)'`');
						}
						else
						{
							str.Write((byte)'`');
							str.WriteS(transfer.From.Name);
							str.WriteS("` AS `");
							str.WriteS(transfer.To.Name);
							str.Write((byte)'`');
						}
					}

					str.WriteS(" FROM ");
					str.WriteS(MySQLSchema.TableName(TransferMap.SourceEntityName));
					str.WriteS(" AS `");
					str.WriteS(TransferMap.SourceEntityName);
					str.Write((byte)'`');
					if (TransferMap.SourceTypeNameExtension != null)
					{
						str.WriteS(TransferMap.SourceTypeNameExtension);
					}

					break;
				case INSERT:
					// Single or multiple row insert.
					str.WriteS("INSERT INTO ");
					str.WriteS(MainTable);
					str.Write((byte)'(');

					// List of field names next!
					for (var i = 0; i < Fields.Count; i++)
					{
						if (i != 0)
						{
							str.WriteS(", ");
						}
						str.Write('`');
						str.WriteS(Fields[i].Name);
						str.Write((byte)'`');
					}
					if (bulk)
					{
						str.WriteS(") VALUES ");
					}
					else
					{
						str.WriteS(") VALUES (");
						// And list the parameter set:
						for (var i = 0; i < Fields.Count; i++)
						{
							if (i != 0)
							{
								str.WriteS(", ");
							}
							str.Write((byte)'?');
						}
						str.Write((byte)')');
					}

					if (_where != null || !filterEmpty)
					{
						throw new Exception("Where isn't permitted on insert statements");
					}
					break;
			}

			string result;

			var firstCollector = qP.QueryA.FirstCollector;

			if (includeCount)
			{
				// SELECT .. FROM .. WHERE .. first:
				if (!filterEmpty)
				{
					if (!qP.QueryA.Empty || !qP.QueryB.Empty)
					{
						str.WriteS(" WHERE ");
					}

					if (!qP.QueryA.Empty)
					{
						qP.QueryA.BuildWhereQuery(cmd, str, firstCollector, localeCode, context, qP.QueryA);

						if (!qP.QueryB.Empty)
						{
							str.WriteS(" AND ");
						}
					}

					if (!qP.QueryB.Empty)
					{
						qP.QueryB.BuildWhereQuery(cmd, str, null, localeCode, context, qP.QueryA);
					}
				}

				// Bake to a string as we're mostly interested in the WHERE part:
				var whereSegment = str.ToUTF8String();

				// Clear the str builder and start constructing the next one:
				str.Reset(null);
				str.WriteS("SELECT COUNT(*) as Count ");
				str.WriteS(whereSegment.Substring(fromLocation));
				str.Write((byte)';');
				str.WriteS(whereSegment);

				// Limit/ sort only comes from QueryA:
				qP.QueryA.BuildOrderLimitQuery(str, localeCode);

				result = str.ToUTF8String();
				str.Release();
			}
			else
			{

				if (!filterEmpty)
				{
					if (!qP.QueryA.Empty || !qP.QueryB.Empty)
					{
						str.WriteS(" WHERE ");
					}

					if (!qP.QueryA.Empty)
					{
						qP.QueryA.BuildWhereQuery(cmd, str, firstCollector, localeCode, context, qP.QueryA);

						if (!qP.QueryB.Empty)
						{
							str.WriteS(" AND ");
						}
					}

					if (!qP.QueryB.Empty)
					{
						qP.QueryB.BuildWhereQuery(cmd, str, null, localeCode, context, qP.QueryA);
					}

					// Limit/ sort only comes from QueryA:
					qP.QueryA.BuildOrderLimitQuery(str, localeCode);
				}
				else if (_where != null)
				{
					str.WriteS(_where);
				}

				result = str.ToUTF8String();
				str.Release();
			}

			if (!bulk && filterEmpty)
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

			cmd.CommandText = result;
		}
		
		/// <summary>
		/// Builds the underlying query to run.
		/// </summary>
		public string GetQuery(bool bulk = false, uint localeId = 0, string localeCode = null, bool includeCount = false)
		{
			if (includeCount && Operation != SELECT)
			{
				throw new ArgumentException("You can only include the count on SELECT statements with a LIMIT.");
				// Otherwise just use the result count.
				// This check however exists to avoid a multitude of potential caching problems.
			}

			if (!bulk)
			{
				if (localeId < 2)
				{
					if (_query != null)
					{
						return _query;
					}
				}
				else if(_localisedQuery != null && (localeId-2) < _localisedQuery.Length && _localisedQuery[localeId - 2] != null)
				{
					// The query is similar 
					// except the fields typically have _localeCode on the end (e.g. _fr or _it).
					return _localisedQuery[localeId - 2];
				}
			}

			var str = new StringBuilder();
			// int paramOffset = 0;
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
						else if (Raw || field.IsPrice)
						{
							// Using this will result in a 'raw' object being returned.
							str.Append(field.LocalisedName);
							str.Append(localeCode);
							str.Append('`');
						}
						else
						{
							// Of the form if(FIELD_locale is null,FIELD,FIELD_locale)");
							str.Append("if(");
							str.Append(field.LocalisedName);
							str.Append(localeCode);
							str.Append("` is null,");
							str.Append(field.FullName);
							str.Append(',');
							str.Append(field.LocalisedName);
							str.Append(localeCode);
							str.Append("`)");
						}
					}

					fromLocation = str.Length;
					str.Append(" FROM ");
					str.Append(MainTableAs);
					break;
				case DELETE:
					str.Append("DELETE `");
					str.Append(MainEntity);
					str.Append("` FROM ");
					str.Append(MainTableAs);

					if (bulk)
					{
						str.Append("WHERE Id ");
						return str.ToString();
					}
					break;
				case COPY:

					// Insert..Select query.
					str.Append("INSERT INTO ");
					str.Append(MySQLSchema.TableName(TransferMap.TargetEntityName));
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
					str.Append(MySQLSchema.TableName(TransferMap.SourceEntityName));
					str.Append(" AS `");
					str.Append(TransferMap.SourceEntityName);
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

					if (_where != null)
					{
						throw new Exception("Where isn't permitted on insert statements");
					}
					break;
			}

			string result;
			
			if (_where != null)
			{
				str.Append(_where);
			}

			result = str.ToString();

			if (!bulk)
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
		public static Query Select(Type rowType, string tableName)
		{
			var result = List(rowType, tableName);
			result.Where("Id=@id");
			return result;
		}

		/// <summary>
		/// The same as select, only it doesn't use a where constraint.
		/// </summary>
		public static Query List(Type rowType, string entityName)
		{
			var result = new Query(entityName)
			{
				Operation = SELECT,

				// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
				// Note that these maps aren't shared between queries so the fields can be removed etc from them.
				Fields = new FieldMap(rowType, entityName)
			};

			return result;
		}

		/// <summary>
		/// Start building a custom WHERE filter.
		/// </summary>
		/// <returns></returns>
		public void Where(string where)
		{
			_where = " WHERE " + where;
		}

		/// <summary>
		/// Generates a insert into rowType.TableName() (field,field..) values(?,?,?..) query.
		/// rowType should be a DatabaseRow derived type. The type of data that will be getting inserted.
		/// </summary>
		public static Query Insert(Type rowType, string entityName, bool explicitId = false)
		{
			var result = new Query(entityName);
			SetupInsert(result, rowType, entityName, explicitId);
			return result;
		}

		private static void SetupInsert(Query result, Type mainType, string entityName, bool explicitId = false)
		{
			result.Operation = INSERT;
			result.SetMainTable(entityName);

			// Fields that we'll select are mapped ahead-of-time for rapid lookup speeds.
			// Note that these maps aren't shared between queries so the fields can be removed etc from them.
			result.Fields = new FieldMap(mainType, entityName);

			// Discover if the type has an explicit ID or not.
			// It will do if it has turned off auto-inc on the Id, which is done via setting the attrib on the class itself.
			// So, lets get that:
			var metaAttribs = mainType.GetCustomAttributes(typeof(DatabaseFieldAttribute), true);

			if (metaAttribs.Length > 0)
			{
				var classMeta = (DatabaseFieldAttribute)metaAttribs[0];

				if (classMeta != null && classMeta.AutoIncWasSet && !classMeta.AutoIncrement)
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
		/// Generates a delete from rowType.TableName() query.
		/// Defaults to using where Id=? unless you use a custom where override.
		/// Type should be a DatabaseRow derived type. The type of data that will be getting deleted.
		/// </summary>
		public static Query Delete(Type type, string entityName)
		{
			var result = new Query
			{
				Operation = DELETE
			};
			result.SetMainTable(entityName);

			// Default where:
			result.Where("Id=@id");
			return result;
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
		/// Remove all fields except the named ones.
		/// </summary>
		/// <param name="fieldNames"></param>
		public Query RemoveAllBut(params string[] fieldNames)
		{
			Fields.RemoveAllBut(fieldNames);
			return this;
		}
	}

}