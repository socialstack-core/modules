using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Api.Permissions{
	
	/// <summary>
	/// A non-allocating mechanism for obtaining a list of things from a service.
	/// </summary>
	public struct QueryPair<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// Primary user provided query. Will be effectively AND-ed with QueryB.
		/// </summary>
		public Filter<T,ID> QueryA;
		/// <summary>
		/// Secondary query, pre-parsed. Originates from the permission system, and is not null.
		/// </summary>
		public Filter<T,ID> QueryB;
	}
	
	/// <summary>
	/// Fast filter metadata.
	/// </summary>
	public class FilterMeta<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		private string _query;
		/// <summary>
		/// The service this filter is for
		/// </summary>
		public AutoService<T, ID> Service;
		private bool _allowConstants;
		/// <summary>
		/// The constructed filter type to use.
		/// </summary>
		private Type _constructedType;
		
		/// <summary>
		/// The AST for this filter.
		/// </summary>
		public FilterAst<T, ID> Ast;

		/// <summary>
		/// True if the filter has a From(..) statement. It must have one only and can only be a child of an AND statement.
		/// </summary>
		public bool HasFrom;

		/// <summary>
		/// Creates filter metadata for the given query pair.
		/// </summary>
		public FilterMeta(AutoService<T, ID> service, string query, bool allowConstants = false){
			_query = query;
			Service = service;
			_allowConstants = allowConstants;
		}

		/// <summary>
		/// The original query string.
		/// </summary>
		public string Query => _query;

		/// <summary>
		/// Type info for the args.
		/// </summary>
		public List<ArgBinding> ArgTypes;

		/// <summary>
		/// Parses the queries and constructs the filters now.
		/// </summary>
		public void Construct()
		{
			var tree = FilterAst.Parse(Service, _query, _allowConstants, !_allowConstants);
			Ast = tree;

			if (tree == null)
			{
				// No actual filter. It's effectively just a base "list everything".
				return;
			}

			// Build the type:
			_constructedType = tree.ConstructType();

			if (tree.Root != null)
			{
				HasFrom = tree.Root.HasFrom();
			}

			ArgTypes = tree.Args;

			for (var i = 0; i < ArgTypes.Count; i++)
			{
				ArgTypes[i].ConstructedField = _constructedType.GetField("Arg_" + i);
			}
		}

		/// <summary>
		/// Get a pooled filter instance.
		/// </summary>
		/// <returns></returns>
		public Filter<T,ID> GetPooled()
		{
			Filter<T, ID> f;

			if (_constructedType == null)
			{
				// Just a base Filter<T, ID> - it has no args etc.
				f = new Filter<T, ID>();
				f.Empty = true;
			}
			else
			{
				f = Activator.CreateInstance(_constructedType) as Filter<T, ID>;
				f.Empty = false;
			}

			f.HasFrom = HasFrom;
			f.Pool = this;
			return f;
		}
		
	}

	/// <summary>
	/// A filter2. Use the concrete-type variant as much as possible.
	/// </summary>
	public class FilterBase
	{
		/// <summary>
		/// Results per page. If 0, there's no limitation.
		/// </summary>
		public int PageSize;
		/// <summary>
		/// 0 based starting offset (In number of records).
		/// </summary>
		public int Offset;

		/// <summary>
		/// True if the total # of results should be included. Results in potentially large scans.
		/// </summary>
		public bool IncludeTotal = false;

		/// <summary>
		/// The field to sort by. Must be a field (can't be a virtual or property).
		/// </summary>
		public ContentField SortField;
		/// <summary>
		/// True if this should sort ascending.
		/// </summary>
		public bool SortAscending = true;

		/// <summary>
		/// True if the filter has a "From" target. This is also set to true if arriving via includes.
		/// </summary>
		/// <returns></returns>
		public bool HasFrom;

		/// <summary>
		/// Binds the current arg using the given textual representation.
		/// </summary>
		/// <param name="str"></param>
		public virtual FilterBase BindUnknown(string str)
		{
			return this;
		}

		/// <summary>Builds an SQL Where query.</summary>
		/// <param name="cmd"></param>
		/// <param name="builder"></param>
		/// <param name="currentCollector"></param>
		/// <param name="context"></param>
		/// <param name="filterA"></param>
		/// <param name="localeCode">Optional localeCode used when a request is for e.g. French fields instead. 
		/// It would be e.g. "fr" and just matches whatever your Locale.Code is.</param>
		public virtual void BuildWhereQuery(MySqlCommand cmd, Writer builder, IDCollector currentCollector, string localeCode, Context context, FilterBase filterA)
		{
		}

		/// <summary>
		/// Gets the set of argument types for this filter. Can be null if there are none.
		/// </summary>
		/// <returns></returns>
		public virtual List<ArgBinding> GetArgTypes()
		{
			return null;
		}

		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// If a variable is outputted then a value reader is pushed in the given arg set.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="localeCode">Optional localeCode used when a request is for e.g. French fields instead. 
		/// It would be e.g. "fr" and just matches whatever your Locale.Code is.</param>
		public void BuildOrderLimitQuery(Writer builder, string localeCode)
		{
			if (SortField != null)
			{
				builder.WriteS(" ORDER BY ");
				builder.Write((byte)'`');
				builder.WriteS(SortField.FieldInfo.Name);

				if (SortField.Localised && localeCode != null)
				{
					builder.Write((byte)'_');
					builder.WriteS(localeCode);
				}

				builder.WriteS("` ");
				builder.WriteS(SortAscending ? "asc" : "desc");
			}

			if (PageSize != 0)
			{
				builder.WriteS(" LIMIT ");
				builder.WriteS(Offset);
				builder.Write((byte)',');
				builder.WriteS(PageSize);
			}
			else if (Offset != 0)
			{
				// Just being used as an offset.
				builder.WriteS(" LIMIT ");
				builder.WriteS(Offset);
			}
		}

		/// <summary>
		/// use this to paginate (or restrict result counts) for large filters.
		/// </summary>
		/// <param name="pageIndex">0 based page index.</param>
		/// <param name="pageSize">The amount of results per page, or 50 if not specified. 
		/// If you specifically set this to 0, pageIndex acts like an offset (i.e. 10 meaning skip the first 10 results).</param>
		public FilterBase SetPage(int pageIndex, int pageSize = 50)
		{
			Offset = pageSize == 0 ? pageIndex : pageIndex * pageSize;
			PageSize = pageSize;
			return this;
		}

		/// <summary>
		/// Sort this filter by the given field name from the filters default type.
		/// </summary>
		/// <param name="fieldName">Name of field to sort by.</param>
		/// <param name="ascending">True if should sort in ascending order (true is default).</param>
		public FilterBase Sort(string fieldName, bool ascending = true)
		{
			SortAscending = ascending;
			SortField = GetField(fieldName);
			return this;
		}

		/// <summary>
		/// Gets a field in the type by its textual name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected virtual ContentField GetField(string name)
		{
			return null;
		}

		/// <summary>
		/// Test if the given object passes this filter.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="value"></param>
		/// <param name="filterA">A filter which can hold state. Permission system filters can't hold state, so use the given one instead.</param>
		/// <returns></returns>
		public virtual bool Match(Context context, object value, FilterBase filterA)
		{
			// No filter - pass by default.
			return true;
		}

		/// <summary>
		/// Gets the query for this filter.
		/// </summary>
		/// <returns></returns>
		public virtual string GetQuery()
		{
			return "";
		}

	}

	/// <summary>
	/// Fast precompiled non-allocating filter engine.
	/// </summary>
	public class Filter<T,ID> : FilterBase
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// The pool that the object came from.
		/// </summary>
		public FilterMeta<T, ID> Pool;

		/// <summary>
		/// Current arg offset.
		/// </summary>
		protected int _arg = 0;

		/// <summary>
		/// True if every arg has been bound.
		/// </summary>
		/// <returns></returns>
		public bool FullyBound()
		{
			if (Pool == null || Pool.ArgTypes == null)
			{
				return _arg == 0;
			}
			return _arg == Pool.ArgTypes.Count;
		}

		/// <summary>
		/// Return back to pool.
		/// </summary>
		public void Release()
		{
			Reset();
		}

		/// <summary>
		/// True if this filter will always be true.
		/// </summary>
		public bool Empty;

		/// <summary>
		/// True if the given iterator has the given value in it
		/// </summary>
		/// <typeparam name="IT"></typeparam>
		/// <param name="value"></param>
		/// <param name="vals"></param>
		/// <returns></returns>
		public static bool HasAny<IT>(IT value, IEnumerable<IT> vals)
			where IT:IEquatable<IT>
		{
			foreach (var v in vals)
			{
				if (v.Equals(value))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// First IDcollector for filter A. Both chains are stored on filterA as it's user specific.
		/// </summary>
		public IDCollector FirstACollector;

		/// <summary>
		/// First IDcollector for filter B. Both chains are stored on filterA as it's user specific.
		/// </summary>
		public IDCollector FirstBCollector;

		/// <summary>
		/// Gets the set of argument types for this filter. Can be null if there are none.
		/// </summary>
		/// <returns></returns>
		public override List<ArgBinding> GetArgTypes()
		{
			return Pool.ArgTypes;
		}
		
		/// <summary>Builds an SQL Where query.</summary>
		/// <param name="cmd"></param>
		/// <param name="builder"></param>
		/// <param name="currentCollector"></param>
		/// <param name="context"></param>
		/// <param name="filterA"></param>
		/// <param name="localeCode">Optional localeCode used when a request is for e.g. French fields instead. 
		/// It would be e.g. "fr" and just matches whatever your Locale.Code is.</param>
		public override void BuildWhereQuery(MySqlCommand cmd, Writer builder, IDCollector currentCollector, string localeCode, Context context, FilterBase filterA)
		{
			if (Pool == null || Pool.Ast == null)
			{
				// There isn't one
				return;
			}
			
			// Don't cache this.
			// Both queryA and queryB might output the same text, however they will use different arg numbers, meaning only filterA can be cached (because it's first).
			// For simplicity, therefore, cache neither.

			// Only filterA is permitted to have args. This is also important for checking the state of any On(..) calls.
			var cc = currentCollector;
			Pool.Ast.ToSql(cmd, builder, ref cc, localeCode, (Filter<T, ID>)filterA, context);
		}
		
		/// <summary>
		/// Gets a field in the type by its textual name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected override ContentField GetField(string name)
		{
			if (Pool == null)
			{
				return null;
			}


			if (!Pool.Service.GetContentFields().NameMap.TryGetValue(name.ToLower(), out ContentField fld))
			{
				return null;
			}

			return fld;
		}

		/// <summary>
		/// Gets the query for this filter.
		/// </summary>
		/// <returns></returns>
		public override string GetQuery()
		{
			return Pool == null ? "" : Pool.Query;
		}

		/// <summary>
		/// Reset arg bind.
		/// </summary>
		public void Reset()
		{
			_arg = 0;
			Offset = 0;
			PageSize = 0;
			IncludeTotal = false;
			SortField = null;
			SortAscending = true;
		}

		/// <summary>
		/// Indicates a bind failure has happened.
		/// </summary>
		/// <param name="type"></param>
		public void Fail(Type type)
		{
			if (Pool == null)
			{
				throw new Exception("Argument #" + _arg + " of this filter is not a '" + type.Name + "'.");
			}

			var max = Pool.ArgTypes == null ? 0 : Pool.ArgTypes.Count;

			if (_arg >= max)
			{
				throw new Exception("Too many args being provided. This filter has " + max);
			}

			var arg = Pool.ArgTypes[_arg];

			throw new Exception("Argument #" + _arg + " must be a '" + arg.ArgType.Name + "', but you used Bind('" + type.Name + "') for it.");
		}

		/// <summary>
		/// Data options for this filter.
		/// </summary>
		public DataOptions DataOptions = DataOptions.Default;

		/// <summary>
		/// Execute this filter now, obtaining an allocated list of results. 
		/// Consider using the callback overload instead if you wish to avoid the list allocation.
		/// </summary>
		/// <returns></returns>
		public async ValueTask ListAll(Context context, Func<Context, T, int, object, object, ValueTask> cb, object srcA = null, object srcB = null)
		{
			await Pool.Service.GetResults(context, this, cb, srcA, srcB);
			Release();
		}

		/// <summary>
		/// Convenience function for getting a true if there are any results, or false if there were none.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<bool> Any(Context context)
		{
			var results = await ListAll(context);
			return results.Count > 0;
		}

		/// <summary>
		/// Convenience function for getting the first result, or null.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<T> First(Context context)
		{
			var results = await ListAll(context);
			return results.Count > 0 ? results[0] : null;
		}
		
		/// <summary>
		/// Convenience function for getting the last result, or null.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<T> Last(Context context)
		{
			var results = await ListAll(context);
			return results.Count > 0 ? results[results.Count - 1] : null;
		}
		
		/// <summary>
		/// Execute this filter now, obtaining an allocated list of results. 
		/// Consider using the callback overload instead if you wish to avoid the list allocation.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<List<T>> ListAll(Context context)
		{
			var results = new List<T>();
			await ListAll(context, (Context ctx, T val, int index, object src, object src2) =>
			{
				var list = src as List<T>;
				list.Add(val);
				return new ValueTask();
			}, results);
			return results;
		}
		
		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public Filter<T, ID> Bind(object v)
		{
			var t = v.GetType();

			if (Pool.ArgTypes == null || _arg >= Pool.ArgTypes.Count)
			{
				Fail(t);
				return this;
			}

			var argInfo = Pool.ArgTypes[_arg];

			if (!argInfo.ArgType.IsAssignableFrom(t))
			{
				Fail(t);
			}

			argInfo.ConstructedField.SetValue(this, v);
			_arg++;
			return this;
		}

		/// <summary>
		/// Binds the current arg using the given textual representation.
		/// </summary>
		/// <param name="str"></param>
		public override FilterBase BindUnknown(string str)
		{
			return BindFromString(str);
		}

		/// <summary>
		/// Binds the current arg using the given textual representation.
		/// </summary>
		/// <param name="str"></param>
		public virtual Filter<T, ID> BindFromString(string str)
		{
			Fail(typeof(string));
			return this;
		}
		
		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(string v)
		{
			Fail(typeof(string));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(double v)
		{
			Fail(typeof(double));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(float v)
		{
			Fail(typeof(float));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(decimal v)
		{
			Fail(typeof(decimal));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(DateTime v)
		{
			Fail(typeof(DateTime));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(bool v)
		{
			Fail(typeof(bool));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(ulong v)
		{
			Fail(typeof(ulong));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(long v)
		{
			Fail(typeof(long));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(uint v)
		{
			Fail(typeof(uint));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(int v)
		{
			Fail(typeof(int));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(ushort v)
		{
			Fail(typeof(ushort));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(short v)
		{
			Fail(typeof(short));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(byte v)
		{
			Fail(typeof(byte));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(sbyte v)
		{
			Fail(typeof(sbyte));
			return this;
		}

		// Nullables

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(double? v)
		{
			Fail(typeof(double?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(float? v)
		{
			Fail(typeof(float?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(decimal? v)
		{
			Fail(typeof(decimal?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(DateTime? v)
		{
			Fail(typeof(DateTime?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(bool? v)
		{
			Fail(typeof(bool?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(ulong? v)
		{
			Fail(typeof(ulong?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(long? v)
		{
			Fail(typeof(long?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(uint? v)
		{
			Fail(typeof(uint?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(int? v)
		{
			Fail(typeof(int?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(ushort? v)
		{
			Fail(typeof(ushort?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(short? v)
		{
			Fail(typeof(short?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(byte? v)
		{
			Fail(typeof(byte?));
			return this;
		}

		/// <summary>
		/// Binds given value to current argument.
		/// </summary>
		public virtual Filter<T, ID> Bind(sbyte? v)
		{
			Fail(typeof(sbyte?));
			return this;
		}

	}
	
}