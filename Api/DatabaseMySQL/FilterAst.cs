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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
		{
			if (Root != null)
			{
				Root.ToSql(cmd, writer, localeCode, filter, context);
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
		/// <param name="context"></param>
		/// <param name="filterA"></param>
		/// <param name="localeCode">Optional localeCode used when a request is for e.g. French fields instead. 
		/// It would be e.g. "fr" and just matches whatever your Locale.Code is.</param>
		public override void BuildWhereQuery(MySqlCommand cmd, Writer builder, string localeCode, Context context, FilterBase filterA)
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
			Pool.Ast.ToSql(cmd, builder, localeCode, (Filter<T, ID>)filterA, context);
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
		/// <param name="context"></param>
		/// <param name="filterA"></param>
		/// <param name="localeCode">Optional localeCode used when a request is for e.g. French fields instead. 
		/// It would be e.g. "fr" and just matches whatever your Locale.Code is.</param>
		public virtual void BuildWhereQuery(MySqlCommand cmd, Writer builder, string localeCode, Context context, FilterBase filterA)
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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public virtual void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
		{
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
		/// Steps through this tree, building an SQL-format where query. Very similar to how it actually starts out.
		/// Note that if it encounters an array node, it will immediately resolve the value using values stored in the given filter instance.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="writer"></param>
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
		{
			if (Operation == "not")
			{
				writer.WriteS(" not (");
				A.ToSql(cmd, writer, localeCode, filter, context);
				writer.Write((byte)')');
				return;
			}

			if (Operation == "and" || Operation == "&&" || Operation == "or" || Operation == "||")
			{
				writer.Write((byte)'(');
				A.ToSql(cmd, writer, localeCode, filter, context);
				writer.Write((byte)' ');
				writer.WriteS(Operation);
				writer.Write((byte)' ');
				B.ToSql(cmd, writer, localeCode, filter, context);
				writer.Write((byte)')');
			}
			else if (Operation == "=")
			{
				// Special case if RHS is either null or an array.
				A.ToSql(cmd, writer, localeCode, filter, context);

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
						B.ToSql(cmd, writer, localeCode, filter, context);
					}
					else
					{
						// Handle null special case:
						var val = filter.Arguments[arg.Binding.Index].BoxedValue;

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
					B.ToSql(cmd, writer, localeCode, filter, context);
				}
			}
			else if (Operation == "!=")
			{
				// Special case if RHS is either null or an array.
				A.ToSql(cmd, writer, localeCode, filter, context);

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
						B.ToSql(cmd, writer, localeCode, filter, context);
					}
					else
					{
						// Handle null special case:
						var val = filter.Arguments[arg.Binding.Index].BoxedValue;

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
					B.ToSql(cmd, writer, localeCode, filter, context);
				}
			}
			else if (Operation == ">=" || Operation == ">" || Operation == "<" || Operation == "<=")
			{
				// Field op
				A.ToSql(cmd, writer, localeCode, filter, context);
				writer.WriteS(Operation);
				B.ToSql(cmd, writer, localeCode, filter, context);
			}
			else if (Operation == "startswith")
			{
				// Starts with. Like has equal performance to INSTR, 
				// but like gains the lead considerably when the column is indexed, so like it is.
				A.ToSql(cmd, writer, localeCode, filter, context);
				writer.WriteS(" like concat(");
				B.ToSql(cmd, writer, localeCode, filter, context);
				writer.WriteS(", '%')");
			}
			else if (Operation == "endswith")
			{
				// Ends with. Can only perform a like here:
				A.ToSql(cmd, writer, localeCode, filter, context);
				writer.WriteS(" like concat('%', ");
				B.ToSql(cmd, writer, localeCode, filter, context);
				writer.WriteS(")");
			}
			else if (Operation == "contains")
			{
				// Contains. Uses INSTR to avoid % in args as much as possible.
				writer.WriteS("instr(");
				A.ToSql(cmd, writer, localeCode, filter, context);
				writer.Write((byte)',');
				B.ToSql(cmd, writer, localeCode, filter, context);
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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
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
				return;
			}

			var field = Field;
			
			if (field.VirtualInfo != null)
			{
				if (field.VirtualInfo.IsList)
				{
					#warning this will cause MySQL to quietly return no results when used
					writer.WriteASCII("`Mappings`->>'$.");
					writer.WriteS(field.Name);
					writer.Write((byte)'\'');
					return;
				}
				else
				{
					// Invalid
					field = null;
				}
			}
			
			if (field != null && field.FieldInfo != null)
			{
				// Regular field.

				if (field.Localised && localeCode != null)
				{
					writer.WriteASCII("if(`");
					writer.WriteS(field.FieldInfo.Name);
					writer.WriteASCII("`->>'$.");
					writer.WriteS(localeCode);
					writer.WriteASCII("' is null,`");
					writer.WriteS(field.FieldInfo.Name);
					writer.WriteASCII("`->>'$.en',`");
					writer.WriteS(field.FieldInfo.Name);
					writer.WriteASCII("`->>'$.");
					writer.WriteS(localeCode);
					writer.WriteASCII("')");
				}
				else
				{
					writer.Write((byte)'`');
					writer.WriteS(field.FieldInfo.Name);
					writer.Write((byte)'`');
				}
			}
			else
			{
				// (it's a property actually - properties are unusable in filters as they don't exist in the data store.
				// We don't call it a property as it leaks a little bit of internal structural information).
				throw new PublicException("Filter syntax error: Cannot use the field '" + Field.Name + "' in a filter.", "filter/invalid_field_usage");
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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
		{
			bool isIncluded = (filter.ContextFlags & ContextFlags.IsIncluded) == ContextFlags.IsIncluded;

			if (isIncluded)
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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
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
		/// <param name="localeCode"></param>
		/// <param name="filter"></param>
		/// <param name="context"></param>
		public override void ToSql(MySqlCommand cmd, Writer writer, string localeCode, Filter<T, ID> filter, Context context)
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

				var val = filter.Arguments[Binding.Index].BoxedValue;

				writer.WriteASCII("IN(");

				if (!_generator.Generate(writer, val))
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
				var val = filter.Arguments[Binding.Index].BoxedValue;
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
