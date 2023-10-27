using Api.Configuration;
using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Api.Permissions{

	/// <summary>
	/// A tree of parsed filter nodes.
	/// </summary>
	public partial class FilterAst<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			if (Root != null)
			{
				Root.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
			}
		}
	}
	
	/// <summary>
	/// Fast precompiled non-allocating filter engine.
	/// </summary>
	public partial class Filter<T,ID> : FilterBase
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		
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
		
	}
	
	/// <summary>
	/// A filter. Use the concrete-type variant as much as possible.
	/// </summary>
	public partial class FilterBase
	{
		
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
		
	}
	
	/// <summary>
	/// Base tree node
	/// </summary>
	public partial class FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public virtual void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
		}
	}

	public partial class MappingFilterTreeNode<T, ID> : FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Mapping table name.
		/// </summary>
		private string MappingTableName;

		/// <summary>
		/// Gets just the table name.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="target"></param>
		/// <param name="listAs"></param>
		/// <returns></returns>
		private static string GetMappingTableName(Type src, Type target, string listAs)
		{
			var srcTypeName = src.Name;
			var targetTypeName = target.Name;
			var typeName = srcTypeName + "_" + targetTypeName + "_map_" + listAs;
			var name = AppSettings.DatabaseTablePrefix + typeName.ToLower();

			if (name.Length > 64)
			{
				name = name.Substring(0, 64);
			}

			return name;
		}

		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			if (MappingTableName == null)
			{
				// Only thing that actually matters with regards to source/ target direction is just the table name:
				MappingTableName = SourceMapping ?
					GetMappingTableName(OtherService.ServicedType, typeof(T), MapName) :
					GetMappingTableName(typeof(T), OtherService.ServicedType, MapName);
			}

			if (TargetField != null)
			{
				// The same as just Field=x.

				writer.Write((byte)'`');
				writer.WriteS(TargetField.FieldInfo.Name);

				if (TargetField.Localised && localeCode != null)
				{
					writer.Write((byte)'_');
					writer.WriteS(localeCode);
				}

				writer.Write((byte)'`');

				var idNode = Id as ArgFilterTreeNode<T, ID>;

				// Read the ID:
				var val = idNode.Binding.ConstructedField.GetValue(filter);

				writer.WriteS("=");
				OutputArg(cmd, writer, val);
			}
			else
			{
				writer.WriteASCII(" EXISTS (SELECT Id from ");
				writer.WriteASCII(MappingTableName);
				writer.WriteASCII(" WHERE ");

				var thisTypeName = typeof(T).Name;

				if (SourceMapping)
				{
					// The given thing is the *target*. I.e. the value stored in Id == source Id.
					writer.WriteASCII("TargetId=`");
					writer.WriteASCII(thisTypeName);
					writer.WriteASCII("`.`Id` and SourceId"); // source
				}
				else
				{
					// The given thing is the source. I.e. the value stored in Id == source Id.
					writer.WriteASCII("SourceId=`");
					writer.WriteASCII(thisTypeName);
					writer.WriteASCII("`.`Id` and TargetId"); // target
				}

				writer.Write((byte)'=');
				Id.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.WriteASCII(")");
			}
		}

		/// <summary>
		/// Attempts to output an arg for a particular value. If the value is null, this returns false. You must use "is null" syntax instead.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		private void OutputArg(MySqlCommand cmd, Writer writer, object val)
		{
			var name = "@a" + cmd.Parameters.Count;
			var parameter = cmd.CreateParameter();
			parameter.ParameterName = name;
			parameter.Value = val;
			writer.WriteASCII(name);
			cmd.Parameters.Add(parameter);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class OpFilterTreeNode<T, ID> : FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// A generator for making IN(..) strings for arrays.
		/// </summary>
		private InStringGenerator _generator;

		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			if (Operation == "not")
			{
				writer.WriteS(" not (");
				A.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.Write((byte)')');
				return;
			}

			if (A is MemberFilterTreeNode<T, ID> member && member.Collect)
			{
				// Use up a collector:
				var collector = collectors;
				collectors = collector.NextCollector;

				// Output as an Id IN(..) statement.
				writer.WriteS("Id IN(");

				if (_generator == null)
				{
					var collectorType = collector.GetType();

					if (!collectorType.IsGenericType)
					{
						// This happens most of the time because collectors are generated objects.
						// Their base type is the generic collector type that we're interested in:
						collectorType = collectorType.BaseType;
					}

					_generator = InStringGenerator.Get(collectorType);

					if (_generator == null)
					{
						// Can't use this type as an enumerable array
						throw new Exception("Attempted to use a field value that isn't supported for an array argument in a filter. It was a collector.");
					}
				}

				if (!_generator.GenerateFromCollector(writer, collector))
				{
					// It didn't output anything. As we've already written out Id In(, avoid a failure by outputting a 0:
					writer.WriteS(0);
				}
				writer.Write((byte)')');

				return;
			}

			if (Operation == "and" || Operation == "&&" || Operation == "or" || Operation == "||")
			{
				writer.Write((byte)'(');
				A.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.Write((byte)' ');
				writer.WriteS(Operation);
				writer.Write((byte)' ');
				B.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.Write((byte)')');
			}
			else if (Operation == "=")
			{
				// Special case if RHS is either null or an array.
				A.ToSql(cmd, writer, ref collectors, localeCode, filter, context);

				if (B is NullFilterTreeNode<T, ID>)
				{
					// Null has a special "is null" syntax:
					writer.WriteASCII(" IS NULL");
				}
				else if (B is ArgFilterTreeNode<T, ID> arg)
				{
					if (arg.Array)
					{
						// It'll output an IN(..) statement. Don't include the =.
						writer.Write((byte)' ');
						B.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
					}
					else
					{
						// Handle null special case:
						var val = arg.Binding.ConstructedField.GetValue(filter);

						if (val == null)
						{
							writer.WriteASCII(" IS NULL");
						}
						else
						{
							writer.WriteS(Operation);
							OutputArg(cmd, writer, val);
						}
					}
				}
				else
				{
					// Regular field op
					writer.WriteS(Operation);
					B.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				}
			}
			else if (Operation == "!=")
			{
				// Special case if RHS is either null or an array.
				A.ToSql(cmd, writer, ref collectors, localeCode, filter, context);

				if (B is NullFilterTreeNode<T, ID>)
				{
					// Null has a special "is not null" syntax:
					writer.WriteASCII(" IS NOT NULL");
				}
				else if (B is ArgFilterTreeNode<T, ID> arg)
				{
					if (arg.Array)
					{
						// It'll output an IN(..) statement. we need a NOT infront:
						writer.WriteASCII(" NOT ");
						B.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
					}
					else
					{
						// Handle null special case:
						var val = arg.Binding.ConstructedField.GetValue(filter);

						if (val == null)
						{
							writer.WriteASCII(" IS NOT NULL");
						}
						else
						{
							writer.WriteS(Operation);
							OutputArg(cmd, writer, val);
						}
					}
				}
				else
				{
					// Regular field op
					writer.WriteS(Operation);
					B.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				}
			}
			else if (Operation == ">=" || Operation == ">" || Operation == "<" || Operation == "<=")
			{
				// Field op
				A.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.WriteS(Operation);
				B.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
			}
			else if (Operation == "startswith")
			{
				// Starts with. Like has equal performance to INSTR, 
				// but like gains the lead considerably when the column is indexed, so like it is.
				A.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.WriteS(" like concat(");
				B.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.WriteS(", '%')");
			}
			else if (Operation == "endswith")
			{
				// Ends with. Can only perform a like here:
				A.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.WriteS(" like concat('%', ");
				B.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.WriteS(")");
			}
			else if (Operation == "contains")
			{
				// Contains. Uses INSTR to avoid % in args as much as possible.
				writer.WriteS("instr(");
				A.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.Write((byte)',');
				B.ToSql(cmd, writer, ref collectors, localeCode, filter, context);
				writer.WriteS(")!=0");
			}
			else
			{
				throw new Exception("Not supported via MySQL yet: " + Operation);
			}
		}

		/// <summary>
		/// Attempts to output an arg for a particular value. If the value is null, this returns false. You must use "is null" syntax instead.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		private void OutputArg(MySqlCommand cmd, Writer writer, object val)
		{
			var name = "@a" + cmd.Parameters.Count;
			var parameter = cmd.CreateParameter();
			parameter.ParameterName = name;
			parameter.Value = val;
			writer.WriteASCII(name);
			cmd.Parameters.Add(parameter);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class MemberFilterTreeNode<T, ID> : FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			if (OnContext)
			{
				// Uses an arg.
				var name = "@a" + cmd.Parameters.Count;
				var parameter = cmd.CreateParameter();
				parameter.ParameterName = name;
				parameter.Value = ContextField.PrivateFieldInfo.GetValue(context);
				writer.WriteASCII(name);
				cmd.Parameters.Add(parameter);
			}
			else
			{
				// Regular field.
				
				if (Field.Localised && localeCode != null)
				{
					writer.WriteASCII("if(`");
					writer.WriteS(Field.FieldInfo.Name);
					writer.Write((byte)'_');
					writer.WriteS(localeCode);
					writer.WriteASCII("` is null,`");
					writer.WriteS(Field.FieldInfo.Name);
					writer.WriteASCII("`,`");
					writer.WriteS(Field.FieldInfo.Name);
					writer.Write((byte)'_');
					writer.WriteS(localeCode);
					writer.WriteASCII("`)");
				}
				else
				{
					writer.Write((byte)'`');
					writer.WriteS(Field.FieldInfo.Name);
					writer.Write((byte)'`');
				}
			}
		}
	}
	
	/// <summary>
	/// 
	/// </summary>
	public partial class IsIncludedFilterTreeNode<T, ID> : FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			if (filter.IsIncluded)
			{
				writer.WriteS("true");
			}
			else
			{
				writer.WriteS("false");
			}
		}

	}
	
	/// <summary>
	/// 
	/// </summary>
	public partial class StringFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			if (Value == null)
			{
				writer.WriteS("null");
			}
			else
			{
				writer.WriteEscaped(Value);
			}
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class NumberFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			writer.WriteS(Value);
		}

	}

	/// <summary>
	/// 
	/// </summary>
	public partial class DecimalFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			writer.WriteASCII(Value.ToString());
		}

	}

	/// <summary>
	/// 
	/// </summary>
	public partial class BoolFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			writer.WriteASCII(Value ? "true" : "false");
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class NullFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			writer.WriteASCII("null");
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class ArgFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// A generator for making IN(..) strings for arrays.
		/// </summary>
		private InStringGenerator _generator;

		/// <summary>
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="collectors"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, ref IDCollector collectors, string localeCode, Filter<T, ID> filter, Context context)
		{
			if (Array)
			{
				if (_generator == null)
				{
					_generator = InStringGenerator.Get(Binding.ArgType);

					if (_generator == null)
					{
						// Can't use this type as an enumerable array
						throw new Exception("Attempted to use a field value that isn't supported for an array argument in a filter. It was a " + Binding.ArgType.Name);
					}
				}

				writer.WriteASCII("IN(");
				if (!_generator.Generate(writer, Binding.ConstructedField.GetValue(filter)))
				{
					// It didn't output anything. As we've already written out IN(, avoid a syntax failure by outputting effectively IN(0):
					writer.WriteS(0);
				}
				writer.Write((byte)')');
			}
			else
			{
				// output an arg. This occurs for args used by e.g. contains or startsWith, 
				// where use of a null makes no sense and would (expectedly) return no results.
				var val = Binding.ConstructedField.GetValue(filter);
				var name = "@a" + cmd.Parameters.Count;
				var parameter = cmd.CreateParameter();
				parameter.ParameterName = name;
				parameter.Value = val;
				writer.WriteASCII(name);
				cmd.Parameters.Add(parameter);
			}
		}
	}
}
