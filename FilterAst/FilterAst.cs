using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Api.Permissions{

	/// <summary>
	/// Functions that can be used in a filter. They are invoked during compilation and can output type specific things.
	/// </summary>
	public static partial class FilterFunctions
	{
		/// <summary>
		/// The available methods
		/// </summary>
		private static ConcurrentDictionary<string, MethodInfo> _methods;

		/// <summary>
		/// Gets a filter function by its given lowercase name, or null if it wasn't found.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Func<MemberFilterTreeNode<T, ID>, FilterAst<T, ID>, FilterTreeNode<T, ID>> Get<T, ID>(string name)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>
		{
			if (_methods == null)
			{
				_methods = new ConcurrentDictionary<string, MethodInfo>();

				// Get public static methods:
				var set = typeof(FilterFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static);

				foreach (var method in set)
				{
					var mtdName = method.Name.ToLower();
					if (mtdName == "get")
					{
						continue;
					}
					_methods[mtdName] = method;
				}
			}

			if (!_methods.TryGetValue(name, out MethodInfo mtd))
			{
				return null;
			}

			mtd = mtd.MakeGenericMethod(typeof(T), typeof(ID));
			var result = mtd.CreateDelegate<Func<MemberFilterTreeNode<T, ID>, FilterAst<T, ID>, FilterTreeNode<T, ID>>>();
			return result;
		}

		/// <summary>
		/// True if the user filter has an On(..)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="node"></param>
		/// <param name="ast"></param>
		public static FilterTreeNode<T, ID> HasFrom<T, ID>(MemberFilterTreeNode<T, ID> node, FilterAst<T, ID> ast)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>
		{
			if (node.Args.Count != 0)
			{
				throw new PublicException("HasFrom in a filter call takes 0 arguments", "filter_invalid");
			}

			// Use specialised hasFrom node:
			return new HasFromFilterTreeNode<T,ID>();
		}
		
		/// <summary>
		/// True if either the object is a User and is a direct match, or if this user is the creator user.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="node"></param>
		/// <param name="ast"></param>
		public static FilterTreeNode<T, ID> IsSelf<T, ID>(MemberFilterTreeNode<T, ID> node, FilterAst<T, ID> ast)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
		{
			if (node.Args.Count != 0)
			{
				throw new PublicException("IsSelf in a filter call takes 0 arguments", "filter_invalid");
			}

			var op = new OpFilterTreeNode<T, ID>() {
				Operation = "="
			};

			// Context userId field:
			op.B = new MemberFilterTreeNode<T, ID>()
			{
				Name = "UserId",
				OnContext = true
			}.Resolve(ast);

			if (typeof(T) == typeof(Users.User))
			{
				// Id=context.UserId

				op.A = new MemberFilterTreeNode<T, ID>() {
					Name = "Id"
				}.Resolve(ast);
			}
			else
			{
				// If the object does not have a UserId field (e.g. it's just a regular Content<>), then return a const false.
				if (!typeof(T).IsAssignableFrom(typeof(UserCreatedContent<ID>)))
				{
					// A false:
					return new BoolFilterTreeNode<T, ID>();
				}

				// UserId=context.UserId
				op.A = new MemberFilterTreeNode<T, ID>()
				{
					Name = "UserId"
				}.Resolve(ast);
			}

			return op;
		}

		/// <summary>
		/// True if the contextual user is the creator, or there is a "UserPermits" mapping of {Thing}->{Context.User}
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="node"></param>
		/// <param name="ast"></param>
		public static FilterTreeNode<T, ID> HasUserPermit<T, ID>(MemberFilterTreeNode<T, ID> node, FilterAst<T, ID> ast)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>
		{
			if (node.Args.Count != 0)
			{
				throw new PublicException("HasUserPermit in a filter call takes 0 arguments", "filter_invalid");
			}

			// Get the mapping T->User called UserPermits:
			// var mappingSvc = ast.Service.GetMap<Users.User, uint>("UserPermits");

			return IsSelf(node, ast);
		}

		/// <summary>
		/// True if there is a "RolePermits" mapping of {Thing}->{Context.Role}
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="node"></param>
		/// <param name="ast"></param>
		public static FilterTreeNode<T, ID> HasRolePermit<T, ID>(MemberFilterTreeNode<T, ID> node, FilterAst<T, ID> ast)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>
		{
			if (node.Args.Count != 0)
			{
				throw new PublicException("HasRolePermit in a filter call takes 0 arguments", "filter_invalid");
			}

			// Get the mapping T->Role called RolePermits:
			// var mappingSvc = ast.Service.GetMap<Role, uint>("RolePermits");

			return IsSelf(node, ast);
		}

	}

	/// <summary>
	/// 
	/// </summary>
	public static class FilterAst
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="service"></param>
		/// <param name="q"></param>
		/// <param name="allowConstants"></param>
		/// <param name="allowArgs"></param>
		/// <returns></returns>
		public static FilterAst<T, ID> Parse<T, ID>(AutoService<T,ID> service, string q, bool allowConstants = true, bool allowArgs = true)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>
		{
			if (string.IsNullOrEmpty(q))
			{
				return null;
			}
			var result = new FilterAst<T, ID>(q);
			result.Service = service;
			result.AllowConstants = allowConstants;
			result.AllowArgs = allowArgs;
			result.Root = result.ParseAny(0);
			return result;
		}
	}

	/// <summary>
	/// Argument binding
	/// </summary>
	public class ArgBinding
	{
		/// <summary>
		/// Field type
		/// </summary>
		public Type ArgType;
		/// <summary>
		/// Underlying builder
		/// </summary>
		public FieldBuilder Builder;
		/// <summary>
		/// Constructed field on target type
		/// </summary>
		public FieldInfo ConstructedField;
		/// <summary>
		/// The Bind() method for args of this same type.
		/// </summary>
		public ILGenerator BindMethod;
		/// <summary>
		/// True if this binding was the one that created the bind method. It'll be up to it to add the Ret.
		/// </summary>
		public bool FirstMethodUser;
	}

	/// <summary>
	/// A tree of parsed filter nodes.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	public partial class FilterAst<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// Fields that will require ID collectors (if any - can be null).
		/// </summary>
		public List<ContentField> Collectors;

		/// <summary>
		/// Root node.
		/// </summary>
		public FilterTreeNode<T,ID> Root;
		/// <summary>
		/// True if constant values are permitted. They're disabled for frontend users to avoid 
		/// potentially vast quantities of apparent unique filters, heavily damaging our ability to optimise them.
		/// </summary>
		public bool AllowConstants;
		/// <summary>
		/// True if args ('?') are allowed in this filter.
		/// </summary>
		public bool AllowArgs;
		/// <summary>
		/// The query str
		/// </summary>
		public readonly string Query;
		/// <summary>
		/// Current index in the query
		/// </summary>
		public int Index;
		private int ArgIndex;
		/// <summary>
		/// The autoservice.
		/// </summary>
		public AutoService<T, ID> Service;
		/// <summary>
		/// The bound args, available after calling ConstructType.
		/// </summary>
		public List<ArgBinding> Args;

		/// <summary>
		/// Create an ast for the given query string.
		/// </summary>
		/// <param name="q"></param>
		public FilterAst(string q)
		{
			Query = q;
		}

		private TypeBuilder TypeBuilder;

		/// <summary>
		/// The BindFromString method
		/// </summary>
		private ILGenerator BindStringMethod;

		/// <summary>
		/// an array of just typeof(string)
		/// </summary>
		private static Type[] _strTypeInArray = new Type[] { typeof(string) };

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="argType"></param>
		/// <returns></returns>
		public ArgBinding DeclareArg(int index, Type argType)
		{
			var builder = TypeBuilder.DefineField("Arg_" + index, argType, System.Reflection.FieldAttributes.Public);

			var binding = new ArgBinding() { Builder = builder, ArgType = argType };

			for (var i = 0; i < Args.Count; i++)
			{
				var arg = Args[i];

				if (arg.ArgType == argType && arg.BindMethod != null)
				{
					binding.BindMethod = arg.BindMethod;
					break;
				}
			}

			Args.Add(binding);

			// If it's a valuetype or string, add a Bind method override plus also a string parse.
			var isString = argType == typeof(string);

			if (!argType.IsValueType && !isString)
			{
				// other objects, such as IEnumerables. These don't need special bind overrides.
				return binding;
			}

			var filt = typeof(Filter<T, ID>);
			var _argField = filt.GetField("_arg", BindingFlags.NonPublic | BindingFlags.Instance);

			if (BindStringMethod == null) {
				var bindMethod = TypeBuilder.DefineMethod("BindFromString", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, filt, _strTypeInArray);
				BindStringMethod = bindMethod.GetILGenerator();

				// Declare _arg as a local:
				BindStringMethod.DeclareLocal(typeof(int));
				BindStringMethod.Emit(OpCodes.Ldarg_0);
				BindStringMethod.Emit(OpCodes.Ldfld, _argField);
				BindStringMethod.Emit(OpCodes.Stloc_0);
			}

			if (binding.BindMethod == null)
			{
				// Create the bind method as well:
				binding.FirstMethodUser = true;
				var bindMethod = TypeBuilder.DefineMethod("Bind", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, filt, new Type[] { argType });
				binding.BindMethod = bindMethod.GetILGenerator();

				// Declare _arg as a local:
				binding.BindMethod.DeclareLocal(typeof(int));
				binding.BindMethod.Emit(OpCodes.Ldarg_0);
				binding.BindMethod.Emit(OpCodes.Ldfld, _argField);
				binding.BindMethod.Emit(OpCodes.Stloc_0);
			}

			// First, on the Bind method:

			// If(_arg == index) {
			//     Arg_INDEX = value;
			//     _arg++;
			// }

			var label = binding.BindMethod.DefineLabel();
			binding.BindMethod.Emit(OpCodes.Ldloc_0);
			binding.BindMethod.Emit(OpCodes.Ldc_I4, index);
			binding.BindMethod.Emit(OpCodes.Ceq);
			binding.BindMethod.Emit(OpCodes.Brfalse, label);
				// Set the value
				binding.BindMethod.Emit(OpCodes.Ldarg_0);
				binding.BindMethod.Emit(OpCodes.Ldarg_1);
				binding.BindMethod.Emit(OpCodes.Stfld, builder);
				// Increase _arg:
				binding.BindMethod.Emit(OpCodes.Ldarg_0);
				binding.BindMethod.Emit(OpCodes.Ldloc_0);
				binding.BindMethod.Emit(OpCodes.Ldc_I4_1);
				binding.BindMethod.Emit(OpCodes.Add);
				binding.BindMethod.Emit(OpCodes.Stfld, _argField);
			binding.BindMethod.Emit(OpCodes.Ldarg_0);
			binding.BindMethod.Emit(OpCodes.Ret);
			binding.BindMethod.MarkLabel(label);

			// Next, on the BindFromString method. These are only ever valuetypes, which should mean they have a Parse method that we can use.
			var parseMethod = isString ? null : argType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, _strTypeInArray, null);

			if (isString || parseMethod != null)
			{
				// We've got a parse method that can be used (or it just is a string anyway).

				label = BindStringMethod.DefineLabel();
				BindStringMethod.Emit(OpCodes.Ldloc_0);
				BindStringMethod.Emit(OpCodes.Ldc_I4, index);
				BindStringMethod.Emit(OpCodes.Ceq);
				BindStringMethod.Emit(OpCodes.Brfalse, label);
					// Set the value
					BindStringMethod.Emit(OpCodes.Ldarg_0);
					BindStringMethod.Emit(OpCodes.Ldarg_1);
					if (parseMethod != null)
					{
						// Convert now - the string is on the stack:
						BindStringMethod.Emit(OpCodes.Call, parseMethod);
						// Now a value suitable for the target field is on the stack.
					}
					BindStringMethod.Emit(OpCodes.Stfld, builder);
					// Increase _arg:
					BindStringMethod.Emit(OpCodes.Ldarg_0);
					BindStringMethod.Emit(OpCodes.Ldloc_0);
					BindStringMethod.Emit(OpCodes.Ldc_I4_1);
					BindStringMethod.Emit(OpCodes.Add);
					BindStringMethod.Emit(OpCodes.Stfld, _argField);
				BindStringMethod.Emit(OpCodes.Ldarg_0);
				BindStringMethod.Emit(OpCodes.Ret);
				BindStringMethod.MarkLabel(label);
			}

			return binding;
		}

		/// <summary>
		/// Skips whitespaces
		/// </summary>
		public void ConsumeWhitespace()
		{
			while (Index < Query.Length && char.IsWhiteSpace(Query[Index]))
			{
				Index++;
			}
		}

		/// <summary>
		/// Peeks the next char
		/// </summary>
		/// <returns></returns>
		public char Peek()
		{
			if (Index >= Query.Length)
			{
				return '\0';
			}

			return Query[Index];
		}

		/// <summary>
		/// 
		/// </summary>
		public List<ContentField> GetIndexable()
		{
			// - Applies to function calls too -
			// (e.g. HasUserPermit() which is an indexable function as it uses the permit map OR the UserId one).

			// "indexable" state must be collected through to the root of a filter.
			// For example {IndexableNode} and {IndexableNode} => the "and" node is indexable, and uses the first one (it doesn't need both), or whichever is a shorter list.
			// {IndexableNode} or {non indexable} => the "or" node is _not_ indexable.
			// {IndexableNode} or {IndexableNode} => the "or" node is indexable, and does use both.

			// When multiple indices are returned to the root, it's effectively a union of all set results.

			// If the root is indexable at all, then some big performance gains can occur whilst resolving, particularly for live loops.
			// -> On lookup, it can use the map(s) as an index (or a content index if it's specifically marked as a DatabaseIndex field). 
			//    If there is more than one, it must use an ID collector to obtain and then uniquify the target IDs.
			//    This behaviour finally permits optimised lookups for both tags and categories simultaneously.

			// -> Whenever something is updated/ created with a mapping ref, 
			//    e.g. a message is created and it's mapped to video 4, then an inter-server broadcast marked as "video 4" is sent out.
			//  Any mappable filters are tuned directly in to a lookup, meaning there's a "video 4" index key with a linked list of listeners in it. 
			//  As maps are very specific and data safe by design, no filter or permission check needs to happen. It can simply send to all listeners on that map.

			if (Root == null)
			{
				// No root!
				return null;
			}

			return Root.GetIndexable();
		}

		private static int counter = 1;

		/// <summary>
		/// True if there's any array args or Id collectors.
		/// </summary>
		public bool HasArrayNodes;

		/// <summary>
		/// 
		/// </summary>
		/// <returns>hasArrayNodes is true if there are any collector nodes or [?] args</returns>
		public Type ConstructType()
		{
			HasArrayNodes = false;
			Args = new List<ArgBinding>();
			AssemblyName assemblyName = new AssemblyName("GeneratedFilter_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

			// Create an inheriting type which reads the field as the key value:
			var filterT = typeof(Filter<T, ID>);
			TypeBuilder = moduleBuilder.DefineType("GeneratedFilter", TypeAttributes.Public, filterT);

			var matchMethod = TypeBuilder.DefineMethod(
				"Match",
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
				typeof(bool), new Type[] { typeof(Context), typeof(object), typeof(FilterBase) }
			);

			ILGenerator writerBody = matchMethod.GetILGenerator();

			if (Root == null)
			{
				// No filter - true:
				writerBody.Emit(OpCodes.Ldc_I4, 1);
			}
			else
			{
				Root.Emit(writerBody, this);
			}

			writerBody.Emit(OpCodes.Ret);

			var baseFail = filterT.GetMethod(nameof(Filter<T, ID>.Fail));

			for (var i = 0; i < Args.Count; i++)
			{
				// Close them off:
				var arg = Args[i];

				if (arg.FirstMethodUser)
				{
					// At the bottom of the bind method is a fail method which indicates current arg X is not of the current type.
					arg.BindMethod.Emit(OpCodes.Ldarg_0);
					arg.BindMethod.Emit(OpCodes.Ldtoken, arg.ArgType);
					arg.BindMethod.Emit(OpCodes.Call, baseFail);
					arg.BindMethod.Emit(OpCodes.Ldarg_0);
					arg.BindMethod.Emit(OpCodes.Ret);
				}
			}

			if (BindStringMethod != null)
			{
				// At the bottom of the bind string method is a fail method which indicates current arg X is not of the current type.
				BindStringMethod.Emit(OpCodes.Ldarg_0);
				BindStringMethod.Emit(OpCodes.Ldtoken, typeof(string));
				BindStringMethod.Emit(OpCodes.Call, baseFail);
				BindStringMethod.Emit(OpCodes.Ldarg_0);
				BindStringMethod.Emit(OpCodes.Ret);
			}

			// Bake the type:
			return TypeBuilder.CreateType();
		}

		/// <summary>
		/// True if there's more tokens
		/// </summary>
		/// <returns></returns>
		public bool More()
		{
			return Index < Query.Length;
		}

		/// <summary>
		/// Peeks the next char
		/// </summary>
		/// <returns></returns>
		public char Peek(int offset)
		{
			if ((Index + offset) >= Query.Length)
			{
				return '\0';
			}

			return Query[Index + offset];
		}

		/// <summary>
		/// Substring from given index to the current index (inclusive of the char at both start + index).
		/// </summary>
		/// <param name="start"></param>
		/// <returns></returns>
		public string SubstringFrom(int start)
		{
			return Query.Substring(start, Index - start);
		}

		/// <summary>
		/// Parse a token in the tree
		/// </summary>
		/// <returns></returns>
		public FilterTreeNode<T, ID> ParseAny(int groupState)
		{
			// Consume whitespace:
			ConsumeWhitespace();

			// Peek the token based on phase:
			var current = Peek();

			FilterTreeNode<T,ID> firstNode;

			// Entry - only a (, ! or a member are permitted.
			if (current == '(')
			{
				// Consume the bracket:
				Index++;
				ConsumeWhitespace();
				firstNode = ParseAny(2);

				if (Peek() != ')')
				{
					throw new PublicException(
						"Unexpected character in filter string. Expected a ) at position " + Index + " in " + Query,
						"filter_invalid"
					);
				}

				Index++;
				ConsumeWhitespace();
			}
			else if (current == '!')
			{
				// Consume the !:
				Index++;
				ConsumeWhitespace();
				current = Peek();

				if (current != '(')
				{
					throw new PublicException(
						"! can only be followed immediately by a bracket in this context " + Index +" in " + Query,
						"filter_invalid"
					);
				}

				// Read the bracket:
				Index++;
				ConsumeWhitespace();
				firstNode = ParseAny(2);
				if (Peek() != ')')
				{
					throw new PublicException(
						"Unexpected character in filter string. Expected a ) at position " + Index + " in " + Query,
						"filter_invalid"
					);
				}

				Index++;
				ConsumeWhitespace();

				firstNode = new OpFilterTreeNode<T, ID>() {
					A = firstNode,
					Operation = "not"
				};
			}
			else
			{
				// Can only be a member (either a field or a method call).
				if (!char.IsLetter(current))
				{
					throw new PublicException(
						"Unexpected character in filter string. Expected a letter but got '" + current + "' at position " + Index + " in " + Query,
						"filter_invalid"
					);
				}

				// Read member:
				var member = new MemberFilterTreeNode<T, ID>();
				firstNode = member.Parse(this); // Consumes whitespace internally
			}

			// If the next char is ) or , or just EOF, return member.
			current = Peek();

			if (groupState == 0)
			{
				if (!More())
				{
					return firstNode;
				}
			}
			else if (groupState == 1) // Inside args like (a,b)
			{
				if (!More() || current == ',' || current == ')')
				{
					return firstNode;
				}
			}
			else if (groupState == 2) // Inside brackets (logic statement)
			{
				if (!More() || current == ')')
				{
					return firstNode;
				}
			}

			// There's more. Next must be an opcode, which means we're returning an Op node.
			var node = ParseOp();
			node.A = firstNode;
			ConsumeWhitespace();

			// An any node is acceptable only if the opcode is logical:
			if (node.IsLogic())
			{
				// A and {ANY}
				var val = ParseAny(groupState);
				node.B = val;
			}
			else
			{
				// Can only be a value here
				var val = ParseValue();
				node.B = val;
			}

			ConsumeWhitespace();
			current = Peek();

			// Check if we're terminating:
			if (groupState == 0)
			{
				if (!More())
				{
					return node;
				}
			}
			else if (groupState == 1) // Inside args like (a,b)
			{
				if (!More() || current == ',' || current == ')')
				{
					return node;
				}
			}
			else if (groupState == 2) // Inside (logic staetment)
			{
				if (!More() || current == ')')
				{
					return node;
				}
			}

			// There's more - it can only be an opcode though:
			var chain = ParseOp();

			if (chain == null || !chain.IsLogic())
			{
				throw new PublicException("Can only chain logic opcodes. Encountered an invalid sequence at " + Index + " in " + Query, "filter_invalid");
			}

			ConsumeWhitespace();
			var child = ParseAny(groupState);
			chain.A = node;
			chain.B = child;
			return chain;
		}

		/// <summary>
		/// Parse an operation
		/// </summary>
		/// <returns></returns>
		public OpFilterTreeNode<T, ID> ParseOp()
		{
			// Operator such as != or "and" or "startsWith" or "endsWith" or "contains":

			// If this char is an ascii letter, read operator until whitespace.
			var current = Peek();

			var opStart = Index;
			
			if (char.IsLetter(current))
			{
				Index++;

				while (Index < Query.Length && !char.IsWhiteSpace(Query[Index]))
				{
					Index++;
				}

			}
			else if (current == '<' || current == '>')
			{
				Index++;
				if (Peek() == '=')
				{
					Index++;
				}

			}
			else if (current == '!')
			{
				Index++;
				if (Peek() != '=')
				{
					// == is invalid
					throw new PublicException("Expected != but only saw the !. Was encountered at index " + Index + " in " + Query, "filter_invalid");
				}
				Index++;
			}
			else if (current == '=')
			{
				Index++;
				if (Peek() == '=')
				{
					// == is invalid
					throw new PublicException("Don't use == in a filter query string. Was encountered at index " + Index + " in " + Query, "filter_invalid");
				}
			}

			// OpCode:
			var opCode = SubstringFrom(opStart).ToLower();

			return new OpFilterTreeNode<T, ID>() {
				Operation = opCode
			};
		}

		private static readonly string NoConstants = "Constant values aren't permitted in this filter, for both performance and to make security easier for you. You'll need to use ? and pass the value as an arg instead.";

		/// <summary>
		/// Parses a constant-like value.
		/// </summary>
		/// <returns></returns>
		public FilterTreeNode<T, ID> ParseValue(bool allowMember = false)
		{
			// Value - typically "?" but can be a const if const is permissable

			// Consume whitespace:
			ConsumeWhitespace();

			// Peek the token based on phase:
			var current = Peek();
			
			// Consume spaces:
			ConsumeWhitespace();

			if (current == '?')
			{
				// Arg
				if (!AllowArgs)
				{
					throw new PublicException(
						"Can't use an argument '?' in this filter. One was found at position " + Index + " in " + Query,
						"filter_invalid"
					);
				}

				Index++;
				return new ArgFilterTreeNode<T, ID>() { Id = ArgIndex++ };
			}

			if (current == '[' && Peek(1) == '?' && Peek(2) == ']')
			{
				// Arg
				if (!AllowArgs)
				{
					throw new PublicException(
						"Can't use an argument '[?]' in this filter. One was found at position " + Index + " in " + Query,
						"filter_invalid"
					);
				}

				Index+=3;
				HasArrayNodes = true;
				return new ArgFilterTreeNode<T, ID>() { Id = ArgIndex++, Array=true };
			}

			if (char.IsDigit(current) || current == '.')
			{
				// Either int/decimal
				var start = Index;
				var isDecimal = current == '.';
				Index++;

				while (More() && (char.IsDigit(Query[Index]) || Query[Index] =='.'))
				{
					if (Query[Index] == '.')
					{
						isDecimal = true;
					}
					Index++;
				}

				var str = Query.Substring(start, Index - start);

				if (!AllowConstants)
				{
					throw new PublicException(
						NoConstants + " Encountered '" + str + "' at position " + start + " in " + Query,
						"filter_invalid"
					);
				}

				if (isDecimal)
				{
					return new DecimalFilterTreeNode<T, ID>() { Value = Decimal.Parse(str) };
				}

				return new NumberFilterTreeNode<T, ID>() { Value = long.Parse(str) };
			}
			else if (current == 'T' || current == 't')
			{
				// "true"
				ExpectText("rue", "RUE");

				if (!AllowConstants)
				{
					throw new PublicException(
						NoConstants + " Encountered a use of true at position " + Index + " in " + Query,
						"filter_invalid"
					);
				}

				Index += 4;

				return new BoolFilterTreeNode<T, ID>() { Value = true };
			}
			else if (current == 'F' || current == 'f')
			{
				// "false"
				ExpectText("alse", "ALSE");
				
				if (!AllowConstants)
				{
					throw new PublicException(
						NoConstants + " Encountered a use of false at position " + Index + " in " + Query,
						"filter_invalid"
					);
				}

				Index += 5;

				return new BoolFilterTreeNode<T, ID>() { Value = false };
			}
			else if (current == 'N' || current == 'n')
			{
				// "null"
				ExpectText("ull", "ULL");

				if (!AllowConstants)
				{
					throw new PublicException(
						NoConstants + " Encountered a use of null at position " + Index + " in " + Query,
						"filter_invalid"
					);
				}
				
				Index += 4;
				return new NullFilterTreeNode<T, ID>();
			}
			else if (current == '"')
			{
				// a string. Only thing that requires escaping is "
				return ReadString('"');
			}
			else if (current == '\'')
			{
				// a string. Only thing that requires escaping is '
				return ReadString('\'');
			}

			if (allowMember)
			{
				// TODO: Handle fields inside a methodCall, e.g. MyFunc(Id)
				// Be careful for the above true/false/null though - fields that start with t/f/n will fail to come down here
			}

			throw new PublicException(
				"Unexpected character in filter string. Expected a value but got '" + current + "' at position " + Index + " in " + Query,
				"filter_invalid"
			);
		}

		private StringBuilder sb = new StringBuilder();

		private StringFilterTreeNode<T, ID> ReadString(char terminal)
		{
			// Read off the first quote:
			var start = Index;
			Index++;
			bool escaped = false;
			while (Index < Query.Length)
			{
				var cur = Query[Index];
				Index++;

				if (escaped)
				{
					escaped = false;
					sb.Append(cur);
				}
				else if (cur == '\\')
				{
					escaped = true;
				}
				else if (cur == terminal)
				{
					break;
				}
				else
				{
					sb.Append(cur);
				}
			}

			var result = sb.ToString();
			sb.Clear();

			if (!AllowConstants)
			{
				throw new PublicException(
					NoConstants + " Encountered the string '" + result + "' at position " + start + " in " + Query,
					"filter_invalid"
				);
			}

			return new StringFilterTreeNode<T, ID>() { Value = result };
		}

		private void ExpectText(string lc, string uc)
		{
			for (var i = 0; i < lc.Length; i++)
			{
				var current = Peek(i + 1);
				if (current != lc[i] && current != uc[i])
				{
					Fail(lc, Index + i + i);
				}
			}
		}

		private void Fail(string msg, int index)
		{
			throw new PublicException(
				"Unexpected character in filter string. Expected " + msg + " but got '" + Query[index] + "' at position " + index + " in " + Query,
				"filter_invalid"
			);
		}

		/// <summary>
		/// 
		/// </summary>
		public override string ToString()
		{
			var builder = new StringBuilder();
			if (Root != null)
			{
				Root.ToString(builder);
			}
			return builder.ToString();
		}
	}

	/// <summary>
	/// Base tree node
	/// </summary>
	public partial class FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{

		/// <summary>
		/// True if the node has an on statement.
		/// Most nodes return false - only and will accept one as a child.
		/// </summary>
		/// <returns></returns>
		public virtual bool HasFrom()
		{
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="ast"></param>
		public virtual void Emit(ILGenerator generator, FilterAst<T, ID> ast)
		{
			throw new NotImplementedException("Filter uses a " + GetType() + " node that can't be emitted: " + ast.ToString());
		}

		/// <summary>
		/// 
		/// </summary>
		public virtual void ToString(StringBuilder builder)
		{
		}
		
		/// <summary>
		/// If not null, this node is indexable (using the given indices).
		/// When more than one is returned, it's because it is a union of all the id's in those indices.
		/// </summary>
		/// <returns></returns>
		public virtual List<ContentField> GetIndexable()
		{
			return null;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class OpFilterTreeNode<T, ID> : FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// string.Equals static method
		/// </summary>
		private static MethodInfo _strEquals;

		/// <summary>
		/// string.StartsWith(str) static method
		/// </summary>
		private static MethodInfo _strStartsWith;

		/// <summary>
		/// string.EndsWith(str) static method
		/// </summary>
		private static MethodInfo _strEndsWith;

		/// <summary>
		/// string.Contains(str) method
		/// </summary>
		private static MethodInfo _strContains;

		/// <summary>
		/// Node a
		/// </summary>
		public FilterTreeNode<T, ID> A;
		
		/// <summary>
		/// Node b
		/// </summary>
		public FilterTreeNode<T, ID> B;

		/// <summary>
		/// The actual operation (lowercase)
		/// </summary>
		public string Operation; // <, >, =, "or", etc

		/// <summary>
		/// True if this is a logic operation
		/// </summary>
		public bool IsLogic()
		{
			return Operation == "and" || Operation == "&&" ||
				Operation == "or" || Operation == "||" ||
				Operation == "not";
		}

		/// <summary>
		/// True if the node has an on statement.
		/// Most nodes return false - only and will accept one as a child.
		/// </summary>
		/// <returns></returns>
		public override bool HasFrom()
		{
			if (Operation == "and" || Operation == "&&")
			{
				return A.HasFrom() || B.HasFrom();
			}

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="ast"></param>
		public override void Emit(ILGenerator generator, FilterAst<T, ID> ast)
		{
			if (Operation == "and" || Operation == "&&")
			{
				A.Emit(generator, ast);

				// If it was false, put a 0 on the stack. Don't evaluate B.
				var falseResult = generator.DefineLabel();
				var afterFalse = generator.DefineLabel();
				generator.Emit(OpCodes.Brfalse, falseResult);
				B.Emit(generator, ast);
				generator.Emit(OpCodes.Brfalse, falseResult);
				generator.Emit(OpCodes.Ldc_I4, 1);
				generator.Emit(OpCodes.Br, afterFalse);
				generator.MarkLabel(falseResult);
				generator.Emit(OpCodes.Ldc_I4, 0);
				generator.MarkLabel(afterFalse);
				return;
			}
			else if (Operation == "or" || Operation == "||")
			{
				var trueResult = generator.DefineLabel();
				var afterTrue = generator.DefineLabel();
				A.Emit(generator, ast);
				generator.Emit(OpCodes.Brtrue, trueResult);
				B.Emit(generator, ast);
				generator.Emit(OpCodes.Brtrue, trueResult);
				generator.Emit(OpCodes.Ldc_I4, 0);
				generator.Emit(OpCodes.Br, afterTrue);
				generator.MarkLabel(trueResult);
				generator.Emit(OpCodes.Ldc_I4, 1);
				generator.MarkLabel(afterTrue);
				return;
			}
			else if (Operation == "not")
			{
				A.Emit(generator, ast);
				generator.Emit(OpCodes.Not);
				return;
			}
			
			// Field=VALUE, FIELD>=VALUE etc.
			var member = A as MemberFilterTreeNode<T, ID>;
			
			if (member == null)
			{
				throw new Exception("Invalid filter - operation '" + Operation + "' must be performed as Member=Value, not Value=Member.");
			}

			// If it's a virtual field, it's a set type.
			if (member.Field.IsVirtual)
			{
				// Actual thing emitted is a collector pop + ID in set

				#warning todo - filtering based on a virtual field
				// close relative of FROM(..) as well.
				throw new Exception("Not supported yet!");
			}
			else
			{
				// It has an actual field. It can either be a context or a field on the target.

				Type fieldType;

				if (member.Field == null)
				{
					// Context field
					fieldType = member.ContextField.PrivateFieldInfo.FieldType;

					// Arg 1 is ctx:
					generator.Emit(OpCodes.Ldarg_1);
					generator.Emit(OpCodes.Callvirt, member.ContextField.Property.GetGetMethod());
				}
				else
				{
					// Field on object
					fieldType = member.Field.FieldInfo.FieldType;

					// Arg 2 is the object being checked (Arg 1 is ctx):
					generator.Emit(OpCodes.Ldarg_2);
					generator.Emit(OpCodes.Ldfld, member.Field.FieldInfo);
				}
				
				var value = B as ConstFilterTreeNode<T, ID>;
				var isEnumerable = false;

				if (value == null)
				{
					// Another member (e.g. CtxField = ObjectField)
					var member2 = B as MemberFilterTreeNode<T, ID>;

					if (member2.Field == null)
					{
						// Context field

						// Arg 1 is ctx:
						generator.Emit(OpCodes.Ldarg_1);
						generator.Emit(OpCodes.Callvirt, member2.ContextField.Property.GetGetMethod());
					}
					else
					{
						// Field on object
						
						// Arg 2 is the object being checked (Arg 1 is ctx):
						generator.Emit(OpCodes.Ldarg_2);
						generator.Emit(OpCodes.Ldfld, member2.Field.FieldInfo);
					}
				}
				else
				{
					// The field's type dictates what we'll read the value as.
					
					// Emit the value. This will vary a little depending on what fieldType is.
					var argNode = value as ArgFilterTreeNode<T, ID>;

					if (argNode == null)
					{
						// Constant. Cooerce the constant to being of the same type as the field.

						if (fieldType == typeof(string))
						{
							generator.Emit(OpCodes.Ldstr, value.AsString());
						}
						else if (fieldType == typeof(float))
						{
							generator.Emit(OpCodes.Ldc_R4, (float)value.AsDecimal());
						}
						else if (fieldType == typeof(double))
						{
							generator.Emit(OpCodes.Ldc_R8, (double)value.AsDecimal());
						}
						else if (fieldType == typeof(uint) || fieldType == typeof(int) || fieldType == typeof(short) ||
							fieldType == typeof(ushort) || fieldType == typeof(byte) || fieldType == typeof(sbyte))
						{
							generator.Emit(OpCodes.Ldc_I4, value.AsInt());
						}
						else if (fieldType == typeof(ulong) || fieldType == typeof(long))
						{
							generator.Emit(OpCodes.Ldc_I8, value.AsInt());
						}
						else if (fieldType == typeof(bool))
						{
							generator.Emit(OpCodes.Ldc_I4, value.AsBool() ? 1 : 0);
						}
					}
					else
					{
						// Create the arg now:
						isEnumerable = argNode.Array;
						var argField = ast.DeclareArg(argNode.Id, argNode.Array ? typeof(IEnumerable<>).MakeGenericType(fieldType) : fieldType);
						argNode.Binding = argField;
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Ldfld, argField.Builder);
					}
				}

				if (Operation == "=")
				{
					if (isEnumerable)
					{
						var baseMethod = typeof(Filter<T, ID>).GetMethod(nameof(Filter<T, ID>.HasAny));
						var hasAny = baseMethod.MakeGenericMethod(fieldType);
						// It's a static method so no "this" is needed. The two field values are also already on the stack.
						generator.Emit(OpCodes.Call, hasAny);
					}
					else if (fieldType == typeof(string))
					{
						// Must use .Equals as they're ref types:
						if (_strEquals == null)
						{
							_strEquals = typeof(string).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);
						}

						generator.Emit(OpCodes.Call, _strEquals);
					}
					else
					{
						generator.Emit(OpCodes.Ceq);
					}
				}
				else if (Operation == "!=")
				{
					if (fieldType == typeof(string))
					{
						// Must use .Equals as they're ref types:
						if (_strEquals == null)
						{
							_strEquals = typeof(string).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);
						}

						generator.Emit(OpCodes.Call, _strEquals);
					}
					else
					{
						generator.Emit(OpCodes.Ceq);
					}
					generator.Emit(OpCodes.Not);
				}
				else if (Operation == ">=")
				{
					// A>=B is the same as !A<B
					generator.Emit(OpCodes.Clt);
					generator.Emit(OpCodes.Not);
				}
				else if (Operation == "<=")
				{
					// A<=B is the same as !A>B
					generator.Emit(OpCodes.Cgt);
					generator.Emit(OpCodes.Not);
				}
				else if (Operation == ">")
				{
					generator.Emit(OpCodes.Cgt);
				}
				else if (Operation == "<")
				{
					generator.Emit(OpCodes.Clt);
				}
				else if (Operation == "contains")
				{
					if (_strContains == null)
					{
						_strContains = typeof(string).GetMethod("Contains", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);
					}

					generator.Emit(OpCodes.Call, _strContains);
				}
				else if (Operation == "startswith")
				{
					if (_strStartsWith == null)
					{
						_strStartsWith = typeof(string).GetMethod("StartsWith", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);
					}

					generator.Emit(OpCodes.Call, _strStartsWith);
				}
				else if (Operation == "endswith")
				{
					if (_strEndsWith == null)
					{
						_strEndsWith = typeof(string).GetMethod("EndsWith", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(string) }, null);
					}

					generator.Emit(OpCodes.Call, _strEndsWith);
				}
				else
				{
					throw new Exception("Opcode not implemented yet: " + Operation);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void ToString(StringBuilder builder)
		{
			if (Operation == "not")
			{
				builder.Append("!(");
				A.ToString(builder);
				builder.Append(')');
				return;
			}

			builder.Append('(');
			A.ToString(builder);
			builder.Append(' ');
			builder.Append(Operation);
			builder.Append(' ');
			B.ToString(builder);
			builder.Append(')');
		}

		/// <summary>
		/// If not null, this node is indexable (using the given indices).
		/// When more than one is returned, it's because it is a union of all the id's in those indices.
		/// </summary>
		/// <returns></returns>
		public override List<ContentField> GetIndexable()
		{
			if (Operation == "and" || Operation == "&&")
			{
				// Logical AND is indexable if either one is.
				var a = A.GetIndexable();
				var b = B.GetIndexable();

				if (a == null && b == null)
				{
					return null;
				}

				if (a != null)
				{
					// If they both are, it uses the "shortest".
					// Note that the shortest is not necessarily the length of the list - it can also be whichever one is 
					if (b != null)
					{
						if (a.Count == b.Count)
						{
							// If they're both the same length, pick one based on them being a ListAs field or not.
							// For example, "Id=4 and Tags={An_Array}". The Id=4 one wins, 
							// because it is more specific and therefore a better index to use.

							var aLists = 0;
							var bLists = 0;
							for (var x = 0; x < a.Count; x++)
							{
								if (a[x].IsVirtual && a[x].VirtualInfo.IsList)
								{
									aLists++;
								}
								if (b[x].IsVirtual && b[x].VirtualInfo.IsList)
								{
									bLists++;
								}
							}

							if (aLists == bLists)
							{
								return a;
							}

							// Fewer list entries wins
							return aLists < bLists ? a : b;
						}

						return a.Count < b.Count ? a : b;
					}

					// a only
					return a;
				}

				// b only
				return b;
			}
			else if (Operation == "or" || Operation == "||")
			{
				// "has these tags OR has these categories" for example.
				// Must be a union of both.
				var a = A.GetIndexable();
				var b = B.GetIndexable();

				if (a == null && b == null)
				{
					// Neither anyway
					return null;
				}

				if (a != null)
				{
					if (b != null)
					{
						var set = new List<ContentField>();

						// Merge:
						set.AddRange(a);
						set.AddRange(b);
						return set;
					}

					// a only
					return a;
				}

				// b only
				return b;
			}

			return null;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class MemberFilterTreeNode<T, ID> : FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// The member name
		/// </summary>
		public string Name;

		/// <summary>
		/// The field to use.
		/// </summary>
		public ContentField Field;

		/// <summary>
		/// The field to use (if it's a context one).
		/// </summary>
		public ContextFieldInfo ContextField;

		/// <summary>
		/// True if this is a method call.
		/// </summary>
		public bool IsMethod
		{
			get {
				return Args != null;
			}
		}

		/// <summary>
		/// If method call, the args
		/// </summary>
		public List<FilterTreeNode<T, ID>> Args;

		/// <summary>
		/// True if this is a context field.
		/// </summary>
		public bool OnContext;


		/// <summary>
		/// 
		/// </summary>
		public override void ToString(StringBuilder builder)
		{
			builder.Append(Name);
			if(Args != null)
			{
				builder.Append('(');
				for (var i = 0; i < Args.Count; i++)
				{
					if (i != 0)
					{
						builder.Append(", ");
					}
					Args[i].ToString(builder);
				}
				builder.Append(')');
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public FilterTreeNode<T,ID> Parse(FilterAst<T,ID> ast)
		{
			var start = ast.Index;

			// Consume first letter:
			ast.Index++;

			// Keep reading letters and numbers until either a ( or whitespace.
			while (ast.More())
			{
				var current = ast.Peek();

				if (char.IsHighSurrogate(current))
				{
					ast.Index++;
					if (char.IsLowSurrogate(ast.Peek()))
					{
						ast.Index++;
					}
				}
				else if (char.IsLetterOrDigit(current))
				{
					ast.Index++;
				}
				else if (current == '(')
				{
					Args = new List<FilterTreeNode<T, ID>>();
					break;
				}
				else
				{
					break;
				}
			}

			// Name is start->index:
			Name = ast.SubstringFrom(start);
			
			if (Args != null)
			{
				// Skip the opening bracket:
				ast.Index++;
				ast.ConsumeWhitespace();

				while (ast.Peek() != ')')
				{
					Args.Add(ast.ParseValue(true));
					ast.ConsumeWhitespace();
					var current = ast.Peek();
					if (!ast.More() || (current != ',' && current != ')'))
					{
						// Fail scenario
						throw new PublicException("Incomplete method call in filter at index " + ast.Index, "filter_invalid");
					}

					if (current == ',')
					{
						ast.Index++;
						ast.ConsumeWhitespace();
					}
				}

				// Consume the closing bracket:
				ast.Index++;
			}

			var resolved = Resolve(ast);

			// Consume whitespace:
			ast.ConsumeWhitespace();

			return resolved;
		}

		/// <summary>
		/// Resolves the field/ method from the name
		/// </summary>
		/// <param name="ast"></param>
		public FilterTreeNode<T, ID> Resolve(FilterAst<T, ID> ast)
		{
			// It's a field which must exist on typeof(T). It can be a virtual list field too.
			// If it is a virtual list field, OR it _has_ a virtual field tied to it, then this field is "mappable".
			var lcName = Name.ToLower();

			if (Args != null)
			{
				var func = FilterFunctions.Get<T, ID>(lcName);

				if (func == null)
				{
					throw new PublicException("Couldn't find filter function '" + Name + "'.", "filter_invalid");
				}

				return func(this, ast);
			}

			if (OnContext)
			{
				if (!ContextFields.Fields.TryGetValue(lcName, out ContextFieldInfo ctxField))
				{
					// Not a valid ctx field!
					throw new PublicException("Couldn't find filter field '" + Name + "' on context.", "filter_invalid");
				}

				ContextField = ctxField;
				return this;
			}

			var fields = ast.Service.GetContentFields();
			if (!fields.TryGetValue(lcName, out ContentField field))
			{
				if (!ContentFields._globalVirtualFields.TryGetValue(lcName, out field))
				{
					throw new PublicException("Couldn't find filter field '" + Name + "' on this type, or a global virtual field by the same name.", "filter_invalid");
				}
				else
				{
					if (ast.Collectors == null)
					{
						ast.Collectors = new List<ContentField>();
					}

					ast.Collectors.Add(field);

					// This field will use an ID collector:
					ast.HasArrayNodes = true;
				}
			}
			else if (field.PropertyInfo != null)
			{
				// Can't use a filter on properties, as it isn't compatible with a remote database
				throw new Exception("Can't use " + Name + " in a filter. Only fields are compatible, not properties.");
			}

			// Field is the field to use.
			Field = field;

			return this;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class HasFromFilterTreeNode<T, ID> : FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{

		/// <summary>
		/// 
		/// </summary>
		public override void ToString(StringBuilder builder)
		{
			builder.Append("HasFrom()");
		}

		/// <summary>
		/// The FilterBase.HasFrom field.
		/// </summary>
		private static FieldInfo _fromField;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="ast"></param>
		public override void Emit(ILGenerator generator, FilterAst<T, ID> ast)
		{
			if (_fromField == null)
			{
				_fromField = typeof(FilterBase).GetField(nameof(FilterBase.HasFrom));
			}

			// Arg 3 is the filter that holds state, such as the "From" status:
			generator.Emit(OpCodes.Ldarg_3);
			generator.Emit(OpCodes.Ldfld, _fromField);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class ConstFilterTreeNode<T, ID> : FilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{

		/// <summary>
		/// 
		/// </summary>
		public virtual long AsInt()
		{
			return 0;
		}

		/// <summary>
		/// 
		/// </summary>
		public virtual string AsString()
		{
			return "";
		}

		/// <summary>
		/// 
		/// </summary>
		public virtual decimal AsDecimal()
		{
			return 0;
		}

		/// <summary>
		/// 
		/// </summary>
		public virtual bool AsBool()
		{
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void ToString(StringBuilder builder)
		{
			builder.Append(AsString());
		}

	}

	/// <summary>
	/// 
	/// </summary>
	public partial class StringFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// 
		/// </summary>
		public string Value;

		/// <summary>
		/// 
		/// </summary>
		public override long AsInt()
		{
			return long.Parse(Value);
		}

		/// <summary>
		/// 
		/// </summary>
		public override string AsString()
		{
			return Value;
		}

		/// <summary>
		/// 
		/// </summary>
		public override decimal AsDecimal()
		{
			return decimal.Parse(Value);
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool AsBool()
		{
			return !string.IsNullOrEmpty(Value);
		}

		/// <summary>
		/// 
		/// </summary>
		public override void ToString(StringBuilder builder)
		{
			builder.Append('"');
			builder.Append(Value.Replace("\"", "\\\""));
			builder.Append('"');
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="ast"></param>
		public override void Emit(ILGenerator generator, FilterAst<T, ID> ast)
		{
			// str
			generator.Emit(OpCodes.Ldstr, Value);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class NumberFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// 
		/// </summary>
		public long Value;

		/// <summary>
		/// 
		/// </summary>
		public override long AsInt()
		{
			return Value;
		}

		/// <summary>
		/// 
		/// </summary>
		public override string AsString()
		{
			return Value.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		public override decimal AsDecimal()
		{
			return Value;
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool AsBool()
		{
			return Value!=0;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class DecimalFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// 
		/// </summary>
		public decimal Value;

		/// <summary>
		/// 
		/// </summary>
		public override long AsInt()
		{
			return (long)Value;
		}

		/// <summary>
		/// 
		/// </summary>
		public override string AsString()
		{
			return Value.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		public override decimal AsDecimal()
		{
			return Value;
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool AsBool()
		{
			return Value != 0;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class BoolFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// 
		/// </summary>
		public bool Value;

		/// <summary>
		/// 
		/// </summary>
		public override long AsInt()
		{
			return Value ? 1 : 0;
		}

		/// <summary>
		/// 
		/// </summary>
		public override string AsString()
		{
			return Value ? "1" : "0";
		}

		/// <summary>
		/// 
		/// </summary>
		public override decimal AsDecimal()
		{
			return Value ? 1 : 0;
		}

		/// <summary>
		/// 
		/// </summary>
		public override bool AsBool()
		{
			return Value;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void ToString(StringBuilder builder)
		{
			builder.Append(Value ? "true" : "false");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="ast"></param>
		public override void Emit(ILGenerator generator, FilterAst<T, ID> ast)
		{
			// A 1 or 0 depending on the val:
			generator.Emit(Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class NullFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// 
		/// </summary>
		public override string AsString()
		{
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void ToString(StringBuilder builder)
		{
			builder.Append("null");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="ast"></param>
		public override void Emit(ILGenerator generator, FilterAst<T, ID> ast)
		{
			// null:
			generator.Emit(OpCodes.Ldnull);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public partial class ArgFilterTreeNode<T, ID> : ConstFilterTreeNode<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>
	{
		/// <summary>
		/// Assigned arg ID
		/// </summary>
		public int Id;

		/// <summary>
		/// True if the user wants to provide a set
		/// </summary>
		public bool Array;

		/// <summary>
		/// The bound value.
		/// </summary>
		public ArgBinding Binding;

		/// <summary>
		/// 
		/// </summary>
		public override void ToString(StringBuilder builder)
		{
			builder.Append(Array ? "[" + Id + "]" : "{" + Id + "}");
		}
	}
}