using Api.Contexts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Api.Database;


namespace Api.Permissions
{
	/// <summary>
	/// Used to filter both permissions and e.g. database queries.
	/// </summary>
	public partial class Filter
	{
		private static Dictionary<string, bool> IgnoredWhereFields;

		/// <summary>
		/// Call this if you handle a where field in a custom way.
		/// </summary>
		/// <param name="fieldName"></param>
		public static void DeclareCustomWhereField(string fieldName)
		{
			if (IgnoredWhereFields == null)
			{
				IgnoredWhereFields = new Dictionary<string, bool>();
			}

			IgnoredWhereFields.Add(fieldName, true);
		}

		/// <summary>
		/// The underlying request info used to construct this filter.
		/// </summary>
		public JObject FromRequest;

		/// <summary>
		/// The default type of this filter. E.g. this is filtering Forum threads then this will be typeof(ForumThread).
		/// </summary>
		public Type DefaultType;

		/// <summary>
		/// Create a new filter with no restrictions by default.
		/// </summary>
		public Filter() { }

		/// <summary>
		/// Builds a filter safely from a generic JSON payload.
		/// </summary>
		/// <param name="fromRequest"></param>
		/// <param name="defaultType">The default database row type to use when referencing fields via where.
		/// For example, where Id 1 would mean DefaultType.Id = 1.</param>
		public Filter(JObject fromRequest, Type defaultType) {

			if (fromRequest == null) {
				return;
			}

			FromRequest = fromRequest;
			DefaultType = defaultType;
			
			// Handle universal pagination:
			var pageSizeJToken = fromRequest["pageSize"];
			int? pageSize = null;

			if (pageSizeJToken != null && pageSizeJToken.Type == JTokenType.Integer) {
				pageSize = pageSizeJToken.Value<int>();
			}

			var pageIndexJToken = fromRequest["pageIndex"];
			int? pageIndex = null;

			if (pageIndexJToken != null && pageSizeJToken.Type == JTokenType.Integer)
			{
				pageIndex = pageIndexJToken.Value<int>();
			}

			if (pageSize.HasValue)
			{
				SetPage(pageIndex.HasValue ? pageIndex.Value : 0, pageSize.Value);
			}
			else if (pageIndex.HasValue)
			{
				// Default page size used
				SetPage(pageIndex.Value);
			}
			var where = fromRequest["where"] as JObject;

			if (where == null) {
				return;
			}

			foreach (var kvp in where)
			{
				// Default type:
				var type = defaultType;
				var fieldName = kvp.Key;
				var dotIndex = fieldName.IndexOf('.');

				if (IgnoredWhereFields != null && IgnoredWhereFields.ContainsKey(fieldName))
				{
					continue;
				}

				if (fieldName.Contains('.'))
				{
					// Type.Field - split:
					var typeName = fieldName.Substring(0, dotIndex);

					// Field name is the rest:
					fieldName = fieldName.Substring(dotIndex + 1);

					type = ContentTypes.GetType(typeName);

					if (type == null)
					{
						throw new Exception("A content type called '" + typeName + "' doesn't exist.");
					}
				}

				var matchingWithValue = kvp.Value;

				if (Nodes.Count > 0)
				{
					// And by default:
					And();
				}

				if (matchingWithValue is JArray)
				{
					// Array of values to compare.

					Add(new FilterFieldEqualsSet(type, fieldName)
					{
						Values = ((JArray)matchingWithValue).ToObject<object[]>()
					});

				}
				else if (matchingWithValue is JObject)
				{
					// It's also specifying the comparitor to use.
					throw new NotImplementedException("Can't use objects as where field args yet.");
				}
				else
				{
					Equals(type, kvp.Key, kvp.Value.ToObject<object>());
				}
			}
			
		}

		/// <summary>
		/// The nodes in this filter.
		/// </summary>
		public List<FilterNode> Nodes = new List<FilterNode>();

		/// <summary>
		/// Any joins in this filter.
		/// </summary>
		public List<FilterJoin> Joins = null;

		/// <summary>
		/// Used only by Construct() methods. Its purpose is to essentially construct the params for the Or()/ And()/ Not() etc nodes.
		/// </summary>
		/// <returns></returns>
		public FilterNode PopConstructed()
		{
			var pop = Nodes[CurrentStackTop++];
			return pop.Construct(this);
		}
		
		/// <summary>
		/// Set when a filter is given to the Capability.IsGranted method.
		/// This is the login token in use by the current runtime only filter.
		/// </summary>
		public Context LoginToken
		{
			get; set;
		}

		private FilterNode _constructed;
		private bool TopUsed = false;
		private FilterNode StackTop = null;
		private int CurrentStackTop = 0;
		/// <summary>
		/// Results per page. If 0, there's no limitation.
		/// </summary>
		public int PageSize;
		/// <summary>
		/// 0 based page index.
		/// </summary>
		public int PageIndex;

		/// <summary>
		/// The role that this filter is for if the filter is used by the permission system.
		/// </summary>
		public Role Role;

		/// <summary>
		/// True if this filter has any nodes.
		/// </summary>
		public bool HasContent
		{
			get
			{
				return Nodes.Count > 0;
			}
		}
		
		/// <summary>
		/// use this to paginate (or restrict result counts) for large filters.
		/// </summary>
		/// <param name="pageIndex">0 based page index.</param>
		/// <param name="pageSize">The amount of results per page, or 50 if not specified. 
		/// If you specifically set this to 0, pageIndex acts like an offset (i.e. 10 meaning skip the first 10 results).</param>
		public void SetPage(int pageIndex, int pageSize = 50)
		{
			PageIndex = pageIndex;
			PageSize = pageSize;
		}

		/// <summary>
		/// If the previous chain resolves to true, then all the given capabilities will be granted.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
		public Role ThenGrant(params string[] capabilityNames)
		{
			var rootNode = Construct();

			// Grant the given set of caps to the given role
			// Using *duplicates* of this grant chain.
			// We duplicate in case people start directly using the grant chain on a particular capability
			// after applying a bulk if to a bunch of them.
			for (var i = 0; i < capabilityNames.Length; i++)
			{
				Role.Grant(capabilityNames[i], rootNode.Copy());
			}

			return Role;
		}

		/// <summary>
		/// Effectively adds the given node either as an X AND node or just the node as-is, depending on what is currently in the filter.
		/// Note: Do not use this on persisted filters. This should only be used on short lived filter objects as it intentionally does a shallow add and construct.
		/// </summary>
		/// <param name="node"></param>
		public void AndAppendConstructed(FilterNode node)
		{
			if (Nodes.Count == 0)
			{
				// Just the node itself:
				_constructed = node;
			}
			else
			{
				// Must add both the AND and the node itself as the constructed node:
				var first = Construct();
				_constructed = new FilterAnd()
				{
					Input0 = first,
					Input1 = node
				};
			}
		}

		/// <summary>
		/// Used to create a sub-block filter.
		/// Usage is e.g. .Brackets((Filter filter) => {
		///		filter.Equals(..).Or().Equals(..);
		/// });
		/// </summary>
		/// <param name="subfilter"></param>
		/// <returns></returns>
		public Filter Brackets(Action<Filter> subfilter)
		{
			// Create a new chain and set it up immediately:
			var chain = new Filter
			{
				Role = Role
			};
			subfilter(chain);

			// Construct the subchain so we get the node to actually add straight away:
			var nodeToAdd = chain.Construct();

			// Add it in:
			return Add(nodeToAdd);
		}

		/// <summary>
		/// Adds a new node to the filter.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private Filter Add(FilterNode node)
		{
			Nodes.Add(node);
			return this;
		}

		/// <summary>
		/// Gets the filter node currently on the top of the stack.
		/// </summary>
		/// <returns></returns>
		public FilterNode GetTopOfStack()
		{
			TopUsed = true;
			return StackTop;
		}

		/// <summary>
		/// "Constructs" the chain. Doesn't actually allocate anything - this just makes sure Or()/ And() etc Inputs are set up.
		/// </summary>
		/// <returns></returns>
		public FilterNode Construct()
		{
			if (_constructed != null)
			{
				return _constructed;
			}

			StackTop = null;

			while (CurrentStackTop < Nodes.Count)
			{
				TopUsed = false;
				var next = PopConstructed();

				if (StackTop != null && !TopUsed)
				{
					// There's something on the top of the stack which wasn't used by the node we just constructed.
					// This indicates we're ignoring some chunk of the filter and it's therefore invalid.
					throw new Exception(
						"Invalid filter if usage. You're probably missing either Or() or And() calls in your filter sequence here."
					);
				}

				StackTop = next;
			}

			_constructed = StackTop;
			return StackTop;
		}
		
	}

}