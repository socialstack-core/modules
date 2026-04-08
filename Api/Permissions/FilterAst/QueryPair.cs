using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Permissions
{

	/// <summary>
	/// A non-allocating mechanism for obtaining a list of things from a service.
	/// </summary>
	public struct QueryPair<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Primary user provided query. Will be effectively AND-ed with QueryB.
		/// </summary>
		public Filter<T,ID> QueryA;

		/// <summary>
		/// Secondary query, pre-parsed. Originates from the permission system, and is not null.
		/// </summary>
		public Filter<T,ID> QueryB;

		/// <summary>
		/// Total result count, when available.
		/// </summary>
		public int Total;

		/// <summary>
		/// True if anything has handled the request.
		/// </summary>
		public bool Handled;

		/// <summary>
		/// Callback when the queries get a result
		/// </summary>
		public Func<Context, T, int, object, object, ValueTask> OnResult;

		/// <summary>
		/// Source a object.
		/// </summary>
		public object SrcA;

		/// <summary>
		/// Source b object.
		/// </summary>
		public object SrcB;
	}
	
	/// <summary>
	/// Fast filter metadata.
	/// </summary>
	public class FilterMeta<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		private string _query;
		/// <summary>
		/// The service this filter is for
		/// </summary>
		public AutoService<T, ID> Service;
		private bool _allowConstants;
		/// <summary>
		/// The constructed match delegate to use.
		/// </summary>
		public Func<FilterBase, Context, object, ContextFlags, bool> MatchDelegate;
		
		/// <summary>
		/// The AST for this filter.
		/// </summary>
		public FilterAst<T, ID> Ast;

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
				MatchDelegate = (FilterBase b, Context ctx, object val, ContextFlags flags) => true;
				return;
			}

			// Build the type:
			tree.Construct();
			ArgTypes = tree.Args;
			MatchDelegate = tree.MatchDelegate;
		}

		/// <summary>
		/// Get a pooled filter instance.
		/// </summary>
		/// <returns></returns>
		public Filter<T,ID> GetPooled()
		{
			Filter<T, ID> f = new Filter<T, ID>();

			if (Ast == null)
			{
				f.Empty = true;
			}
			else if(ArgTypes != null && ArgTypes.Count > 0)
			{
				// Instance the arg set now:
				f.Arguments = new FilterArg[ArgTypes.Count];

				for (var i = 0; i < ArgTypes.Count; i++)
				{
					f.Arguments[i] = (FilterArg)Activator.CreateInstance(ArgTypes[i].FilterArgGenericType);
				}
			}

			f.ContextFlags = ContextFlags.None;
			f.Pool = this;
			return f;
		}
		
	}

	/// <summary>
	/// A filter. Use the concrete-type variant as much as possible.
	/// </summary>
	public partial class FilterBase
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
		/// Errors when a null is given for a non-nullable field.
		/// </summary>
		public void NullCheck(string s)
		{
			if (s == null)
			{
				throw new PublicException("Attempted to use a null as an arg for a non-nullable field. Did you mean to use something else?", "filter_invalid");
			}
		}

		/// <summary>
		/// Return to pool it came from
		/// </summary>
		public virtual void Release()
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
					builder.WriteASCII("`.`");
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
		/// <param name="flags">Indicates which context the match is taking place in, such as if it is within an inclusion context.</param>
		/// <returns></returns>
		public virtual bool Match(Context context, object value, ContextFlags flags)
		{
			throw new NotImplementedException();
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
	/// The base class for args for a filter. You will always have the generic instance actually.
	/// </summary>
	public class FilterArg
	{

		/// <summary>
		/// The value type of the arg.
		/// </summary>
		public virtual Type ArgType => null;

		/// <summary>
		/// The boxed value of this arg.
		/// </summary>
		public virtual object BoxedValue => null;

		/// <summary>
		/// Clears the value of the arg to whatever its default value is.
		/// </summary>
		public virtual void SetDefault()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Set the value of the arg to the given boxed value. You must identify that 
		/// the value is settable before doing this.
		/// </summary>
		public virtual void InternalSetBoxedValue(object v)
		{
			throw new NotImplementedException();
		}

	}

	/// <summary>
	/// A specific argument value holder. Instanced once per filter instance: typically pooled.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FilterArg<T> : FilterArg
	{

		/// <summary>
		/// The value type of the arg.
		/// </summary>
		public override Type ArgType => typeof(T);

		/// <summary>
		/// The value of this arg.
		/// </summary>
		public T Value;

		/// <summary>
		/// The boxed value of this arg.
		/// </summary>
		public override object BoxedValue => Value;


		/// <summary>
		/// Clears the value of the arg to whatever its default value is.
		/// </summary>
		public override void SetDefault()
		{
			Value = default;
		}
		
		/// <summary>
		/// Set the value of the arg to the given boxed value. You must identify that 
		/// the value is settable before doing this.
		/// </summary>
		public override void InternalSetBoxedValue(object v)
		{
			Value = (T)v;
		}

	}

	/// <summary>
	/// Fast precompiled non-allocating filter engine.
	/// </summary>
	public partial class Filter<T,ID> : FilterBase
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// True if we're in an inclusion context.
		/// </summary>
		public ContextFlags ContextFlags;
		/// <summary>
		/// The pool that the object came from.
		/// </summary>
		public FilterMeta<T, ID> Pool;

		/// <summary>
		/// Current arg offset.
		/// </summary>
		protected int _arg = 0;

		/// <summary>
		/// The filter arg set. The array of objects is preallocated once and then reused by the pool system.
		/// </summary>
		public FilterArg[] Arguments;

		/// <summary>
		/// Test if the given object passes this filter.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="value"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public override bool Match(Context context, object value, ContextFlags flags)
		{
			return Pool.MatchDelegate(this, context, value, flags);
		}

		/// <summary>
		/// Bind an argument value to this filter.
		/// </summary>
		/// <typeparam name="VALUE_TYPE"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="PublicException"></exception>
		public Filter<T, ID> Bind<VALUE_TYPE>(VALUE_TYPE value)
		{
			if (typeof(VALUE_TYPE) == typeof(object))
			{
				return BindObject(value);
			}

			if (typeof(VALUE_TYPE) == typeof(JToken))
			{
				return BindJToken((JToken)(object)value);
			}

			var max = Pool.ArgTypes == null ? 0 : Pool.ArgTypes.Count;

			if (_arg >= max)
			{
				throw new PublicException("Too many args being provided. This filter has " + max, "filter_invalid");
			}

			var arg = Arguments[_arg];
			var argType = arg.ArgType;

			if (
				argType == typeof(VALUE_TYPE)
			)
			{
				var specificArg = (FilterArg<VALUE_TYPE>)arg;
				specificArg.Value = value;
				_arg++;
				return this;
			}
			else if (argType.IsAssignableFrom(typeof(VALUE_TYPE)) ||
				Nullable.GetUnderlyingType(argType) == typeof(VALUE_TYPE) ||
				Nullable.GetUnderlyingType(typeof(VALUE_TYPE)) == argType)
			{
				// It's typically a reference type in this scenario
				// anyway (e.g. List being set on an IEnumerable field).
				arg.InternalSetBoxedValue(value);
				_arg++;
				return this;
			}

			// Attempt a coersion bind:
			if (TryCoerseBind(value, typeof(VALUE_TYPE), arg))
			{
				_arg++;
				return this;
			}

			// For now though:
			Fail(typeof(VALUE_TYPE));

			return this;
		}

		private bool TryCoerseBind(object value, Type valueType, FilterArg target)
		{
			if (value == null)
			{
				target.SetDefault();
				return true;
			}

			// The only array coersion that happens is any sort of ulong iterator involving ones.
			if (typeof(IEnumerable<ulong>).IsAssignableFrom(valueType) && target.ArgType == typeof(IEnumerable<uint>))
			{
				target.InternalSetBoxedValue(
					((IEnumerable<ulong>)value).Select(n => (uint)n)
				);
				return true;
			}

			if (target.ArgType == typeof(IEnumerable<ulong>))
			{
				if (typeof(IEnumerable<int>).IsAssignableFrom(valueType))
				{
					target.InternalSetBoxedValue(
						((IEnumerable<int>)value).Select(n => (ulong)n)
					);
					return true;
				}

				if (typeof(IEnumerable<uint>).IsAssignableFrom(valueType))
				{
					target.InternalSetBoxedValue(
						((IEnumerable<uint>)value).Select(n => (ulong)n)
					);
					return true;
				}

				if (typeof(IEnumerable<short>).IsAssignableFrom(valueType))
				{
					target.InternalSetBoxedValue(
						((IEnumerable<short>)value).Select(n => (ulong)n)
					);
					return true;
				}

				if (typeof(IEnumerable<ushort>).IsAssignableFrom(valueType))
				{
					target.InternalSetBoxedValue(
						((IEnumerable<ushort>)value).Select(n => (ulong)n)
					);
					return true;
				}

				if (typeof(IEnumerable<long>).IsAssignableFrom(valueType))
				{
					target.InternalSetBoxedValue(
						((IEnumerable<long>)value).Select(n => (ulong)n)
					);
					return true;
				}
			}

			// We know the src is not null so if it is a nullable type we can 
			// first pop it out of that nullable wrapper.
			var baseSrcNull = Nullable.GetUnderlyingType(valueType);

			if (baseSrcNull != null)
			{
				valueType = baseSrcNull;
				value = Convert.ChangeType(value, baseSrcNull);
			}

			var baseTargetNull = Nullable.GetUnderlyingType(target.ArgType);
			var argType = baseTargetNull == null ? target.ArgType : baseTargetNull;

			try
			{
				var converted = Convert.ChangeType(value, argType);

				if (converted == null)
				{
					return false;
				}

				target.InternalSetBoxedValue(converted);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Binding from a raw textual value.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public Filter<T, ID> BindJToken(JToken token)
		{
			if (token == null)
			{
				return BindObject(null);
			}

			var array = token as JArray;

			if (array != null)
			{
				var argInfo = Pool.ArgTypes[_arg];

				// Is it an array arg?
				var arrOf = argInfo.ArrayOf;

				if (arrOf == null)
				{
					throw new PublicException(
						"You provided an array to a non-array argument. To specify an array argument in a filter, use [?]", 
						"filter_invalid"
					);
				}

				if (argInfo.ArrayOf == typeof(string))
				{
					Bind(ToStringIterator(array));
				}
				else if (argInfo.ArrayOf == typeof(uint))
				{
					Bind(ToLongIterator(array).Select(lon => (uint)lon));
				}
				else if (argInfo.ArrayOf == typeof(uint?))
				{
					Bind(ToLongIterator(array).Select(lon => (uint?)lon));
				}
				else if (argInfo.ArrayOf == typeof(long))
				{
					Bind(ToLongIterator(array));
				}
				else if (argInfo.ArrayOf == typeof(long?))
				{
					Bind(ToLongIterator(array).Select(lon => (long?)lon));
				}
				else if (argInfo.ArrayOf == typeof(ushort))
				{
					Bind(ToLongIterator(array).Select(lon => (ushort)lon));
				}
				else if (argInfo.ArrayOf == typeof(ushort?))
				{
					Bind(ToLongIterator(array).Select(lon => (ushort?)lon));
				}
				else
				{
					Fail(typeof(JArray));
				}
			}
			else
			{
				JValue value = token as JValue;

				if (value == null)
				{
					throw new PublicException(
						"Arg #" + (_arg + 1) + " in the args set is invalid - it can't be an object, only a string or numeric/ bool value.",
						"filter_invalid"
					);
				}

				// The underlying JSON token is textual, so we'll use a general use bind from string method.
				if (value.Type == JTokenType.Date)
				{
					var date = value.Value as DateTime?;

					// The target value could be a nullable date, in which case we'd need to use Bind(DateTime?)
					if (NextBindType == typeof(DateTime?))
					{
						Bind(date);
					}
					else
					{
						Bind(date.Value);
					}
				}
				else if (value.Type == JTokenType.Boolean)
				{
					var boolVal = value.Value as bool?;

					// The target value could be a nullable bool, in which case we'd need to use Bind(bool?)
					if (NextBindType == typeof(bool?))
					{
						Bind(boolVal);
					}
					else
					{
						Bind(boolVal.Value);
					}
				}
				else if (value.Type == JTokenType.Null)
				{
					BindObject(null);
				}
				else if (value.Type == JTokenType.String)
				{
					var str = value.Value<string>();

					if (str == null)
					{
						BindObject(null);
					}
					else
					{
						var argType = NextBindType;

						if (argType == typeof(DateTime) || argType == typeof(DateTime?))
						{
							// Special case for dateTime:
							if (!FilterAst.TryParseDate(str, out DateTime date))
							{
								throw new PublicException("Invalid date format provided '" + str + "'", "date/invalid");
							}

							Bind(date);
						}
						else
						{
							// Other general internal coersion occurs.
							Bind(str);
						}
					}
				}
				else if (value.Type == JTokenType.Integer)
				{
					// Internal coersion occurs
					Bind(value.Value<long>());
				}
				else if (value.Type == JTokenType.Float)
				{
					// Internal coersion occurs
					Bind(value.Value<double>());
				}
			}

			return this;
		}

		/// <summary>
		/// Treats a JArray like a long iterator.
		/// </summary>
		/// <param name="jArray"></param>
		/// <returns></returns>
		private IEnumerable<long> ToLongIterator(JArray jArray)
		{
			return jArray.Select(token => {

				if (token == null)
				{
					return 0;
				}

				switch (token.Type)
				{
					case JTokenType.Null:
						return 0;
					case JTokenType.String:
						if (long.TryParse(token.Value<string>(), out long ui))
						{
							return ui;
						}

						return 0;
					case JTokenType.Boolean:
						return token.Value<bool>() ? (long)1 : 0;
					case JTokenType.Float:
						return (long)token.Value<double>();
					case JTokenType.Integer:
						return token.Value<long>();
					default:
						return 0;
				}
			});
		}

		/// <summary>
		/// Treats a JArray like a string iterator.
		/// </summary>
		/// <param name="jArray"></param>
		/// <returns></returns>
		private IEnumerable<string> ToStringIterator(JArray jArray)
		{
			return jArray.Select(token => {

				if (token == null)
				{
					return (string)null;
				}

				switch (token.Type)
				{
					case JTokenType.Null:
						return (string)null;
					case JTokenType.String:
						return token.Value<string>();
					case JTokenType.Boolean:
						return token.Value<bool>() ? "true" : "false";
					case JTokenType.Float:
						return token.Value<double>().ToString();
					case JTokenType.Integer:
						return token.Value<long>().ToString();
					default:
						return (string)null;
				}
			});
		}

		/// <summary>
		/// If you don't know the type you're binding, use this. 
		/// You can safely use the generic Bind and it will also just fall through here first.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="PublicException"></exception>
		public Filter<T, ID> BindObject(object value)
		{
			var jToken = value as JToken;
			if (jToken != null)
			{
				return BindJToken(jToken);
			}
			
			var max = Pool.ArgTypes == null ? 0 : Pool.ArgTypes.Count;

			if (_arg >= max)
			{
				throw new PublicException("Too many args being provided. This filter has " + max, "filter_invalid");
			}

			var arg = Arguments[_arg];
			var argType = arg.ArgType;

			if (value == null)
			{
				arg.SetDefault();
				_arg++;
				return this;
			}

			var valType = value.GetType();

			if (
				argType == valType || 
				argType.IsAssignableFrom(valType) ||
				Nullable.GetUnderlyingType(argType) == valType ||
				Nullable.GetUnderlyingType(valType) == argType
			)
			{
				arg.InternalSetBoxedValue(value);
				_arg++;
				return this;
			}

			// Attempt a coersion bind:
			if (TryCoerseBind(value, valType, arg))
			{
				_arg++;
				return this;
			}

			// For now though:
			Fail(valType);

			return this;
		}

		/// <summary>
		/// The type of the next arg to bind. Null if there are no more args.
		/// </summary>
		public Type NextBindType {
			get {
				if (_arg >= Pool.ArgTypes.Count)
				{
					return null;
				}
				return Pool.ArgTypes[_arg].ArgType;
			}
		}

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
		public override void Release()
		{
			Reset();
		}

		/// <summary>
		/// True if this filter will always be true.
		/// </summary>
		public bool Empty;

		/// <summary>
		/// Gets the set of argument types for this filter. Can be null if there are none.
		/// </summary>
		/// <returns></returns>
		public override List<ArgBinding> GetArgTypes()
		{
			return Pool.ArgTypes;
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
				throw new PublicException("Argument #" + _arg + " of this filter is not a '" + type.Name + "'.", "filter_invalid");
			}

			var max = Pool.ArgTypes == null ? 0 : Pool.ArgTypes.Count;

			if (_arg >= max)
			{
				throw new PublicException("Too many args being provided. This filter has " + max, "filter_invalid");
			}

			var arg = Pool.ArgTypes[_arg];

			throw new PublicException(
				"Argument #" + _arg + " must be a '" + NiceTypeName(arg.ArgType) + "', but you used Bind('" + NiceTypeName(type) + "') for it.",
				"filter_invalid"
			);
		}

		private string NiceTypeName(Type type)
		{

			var nullableBaseType = Nullable.GetUnderlyingType(type);

			if (nullableBaseType != null)
			{
				return NiceTypeName(nullableBaseType) + "?";
			}

			if (type.IsGenericType)
			{
				var args = type.GetGenericArguments();

				var name = type.Name.Split('`')[0] + "<";

				for (var i = 0; i < args.Length; i++)
				{
					if (i != 0)
					{
						name += ", ";
					}
					name += NiceTypeName(args[i]);
				}

				return name + ">";
			}

			return type.Name;
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
			PageSize = 1;
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
		/// Execute this filter now, obtaining a count using an allocated delegate.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<int> Count(Context context)
		{
			var count = 0;
			await ListAll(context, (Context ctx, T val, int index, object src, object src2) =>
			{
				count++;
				return new ValueTask();
			}, null);
			return count;
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
		/// A convenience variant of SetPage which returns a stronger typed filter.
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public Filter<T, ID> Page(int offset, int size = 50)
		{
			SetPage(offset, size);
			return this;
		}

	}
	
}