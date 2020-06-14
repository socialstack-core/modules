using Api.Contexts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Api.Database;
using System.Threading.Tasks;
using System.Linq;

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
		/// Additional param value resolvers if there are any.
		/// </summary>
		public List<FilterFieldEqualsValue> ParamValueResolvers;
		
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

			DefaultType = defaultType;

			if (fromRequest == null) {
				return;
			}

			FromRequest = fromRequest;
			
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

			var sort = fromRequest["sort"] as JObject;
			if (sort != null)
			{
				if (sort["field"] != null)
				{
					string field = sort["field"].ToString();

					if (sort["direction"] != null && sort["direction"].ToString() == "desc")
					{
						Sort(field, "desc");
					}
					else
					{
						Sort(field, "asc");
					}
				}
			}

			var where = fromRequest["where"];

			if (where == null) {
				return;
			}
			
			// There's 2 main ways you can format your where filter.
			// Either as a simple object, which will be combined with AND:
			/*
				"where": {
					"fieldName": "valueThatItMustBe",
					"otherField": {
						"startsWith": "valueItMustStartWith
					},
					"yetAnotherField": ["equals", "any", "of", "these"]
				}
			*/
			
			// Or it can be an array. If it's an array, then they are combined with OR. like this:
			
			/*
				"where": [
					{
						"fieldName": "valueThatItMustBe"
					},
					{
						"otherField": {
							"startsWith": "valueItMustStartWith
						}
					},
					{
						"yetAnotherField": ["equals", "any", "of", "these"]
					}
				]
			*/
			
			// The above is just an array of the previous where objects.
			
			// So, is it an array?
			
			var whereArray = where as JArray;
			
			if(whereArray != null){
				
				foreach(var entry in whereArray){

					var entryObj = entry as JObject;

					if (entryObj == null)
					{
						continue;
					}

					if (Nodes.Count > 0)
					{
						// These combine with Or unless told otherwise.
						if (entryObj["and"] != null)
						{
							And();
						}
						else
						{
							Or();
						}
					}

					// Handle it with brackets (only if count != 1)
					if (whereArray.Count == 1)
					{
						// We're actually just ignoring the array.
						// This check isn't needed, however it avoids generating unnecessary brackets in the 
						// query and allocating another filter object.
						HandleWhereObject(entryObj);
					}
					else
					{
						Brackets((Filter filter) =>
						{
							filter.HandleWhereObject(entryObj);
						});
					}
				}
				
			}else{
				
				var whereObject = where as JObject;
				
				if(whereObject != null){
					HandleWhereObject(whereObject);
				}
				
			}
		}

		/// <summary>
		/// Creates a new filter which is an AND combination of this one and the given one.
		/// </summary>
		/// <param name="b"></param>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public Filter Combine(FilterNode b, List<FilterFieldEqualsValue> nodes)
		{
			// Create a new chain and set it up immediately:
			var combined = Copy(false);

			// Construct the subchain so we get the node to actually add straight away:
			var a = Construct();

			// If either is null, set just the one on combined.
			if (a == null)
			{
				combined.Add(b);
			}
			else if (b == null)
			{
				combined.Add(a);
			}
			else
			{
				// Add it in:
				combined.Add(new FilterAnd()
				{
					Input0 = a,
					Input1 = b
				});
			}

			// Combine the args:
			if (ParamValueResolvers == null)
			{
				// Only need to consider nodes:
				combined.ParamValueResolvers = nodes;
			}
			else
			{
				if (nodes != null)
				{
					// Merge them:
					combined.ParamValueResolvers = nodes.Concat(ParamValueResolvers).ToList();
				}
				else
				{
					// Only need to consider ParamValueResolvers:
					combined.ParamValueResolvers = ParamValueResolvers;
				}
			}

			return combined;
		}

		/// <summary>
		/// Creates a new filter which is an AND combination of this one and the given one.
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public Filter Combine(Filter b)
		{
			// Create a new chain and set it up immediately:
			var combined = new Filter
			{
				Role = Role,
				DefaultType = DefaultType,
				FromRequest = FromRequest
			};

			// Construct the subchain so we get the node to actually add straight away:
			var a = Construct();

			// Add it in:
			combined.Add(new FilterAnd()
			{
				Input0 = a,
				Input1 = b.Construct()
			});

			return combined;
		}

		/// <summary>
		/// Adds a param value resolver.
		/// </summary>
		/// <param name="resolver"></param>
		public void AddParamValueResolver(FilterFieldEqualsValue resolver)
		{
			if (ParamValueResolvers == null)
			{
				ParamValueResolvers = new List<FilterFieldEqualsValue>();
			}
			ParamValueResolvers.Add(resolver);
		}

		/// <summary>
		/// Applies JSON defined where:{} filters.
		/// </summary>
		private void HandleWhereObject(JObject whereObject){
			
			foreach (var kvp in whereObject)
			{
				// Default type:
				var type = DefaultType;
				var fieldName = kvp.Key;

				if (fieldName == "and" || fieldName == "op")
				{
					// Special case for the and/ op fields.
					continue;
				}

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
					// It's also specifying the mode to use.
					// For example
					// MyField: {startsWith: 'Hello'}
					// MyField: {contains: 'Hello'}
					// MyField: {equals: 'Hello'}
					// MyField: {endsWith: 'Hello'}

					// For now the value can't be a set on anything except equals, i.e. MyField: {endsWith: ['Hello', 'World']} meaning either of Hello or World is not supported.

					var fieldObject = matchingWithValue as JObject;

					foreach (var fvp in fieldObject)
					{

						switch (fvp.Key)
						{
							case "startsWith":
								StartsWith(type, kvp.Key, fvp.Value.Value<string>());
							break;
							case "contains":
								Contains(type, kvp.Key, fvp.Value.Value<string>());
							break;
							case "endsWith":
								EndsWith(type, kvp.Key, fvp.Value.Value<string>());
							break;
							case "geq":
							case "greaterThanOrEqual":
								GreaterThanOrEqual(type, kvp.Key, fvp.Value.Value<long>());
							break;
							case "greaterThan":
								GreaterThan(type, kvp.Key, fvp.Value.Value<long>());
							break;
							case "lessThan":
								LessThan(type, kvp.Key, fvp.Value.Value<long>());
							break;
							case "leq":
							case "lessThanOrEqual":
								LessThanOrEqual(type, kvp.Key, fvp.Value.Value<long>());
							break;
							case "not":
								// Not equals:
								Not().Equals(type, kvp.Key, fvp.Value.ToObject<object>());
							break;
							case "equals":
								// Little bit pointless but we'll support it anyway. fieldName: {equals: x} is the same as just fieldName: x.
								Equals(type, kvp.Key, fvp.Value.ToObject<object>());
							break;
							default:
								throw new Exception(fvp.Key + " is not a suitable comparison operator in a filter. Try contains, startsWith, equals or endsWith.");
						}

					}

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
        /// Any sorts in this filter.
        /// </summary>
        public List<FilterSort> Sorts = null;

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
				Role.Grant(capabilityNames[i], rootNode.Copy(), this);
			}

			return Role;
		}

		/// <summary>
		/// If the previous chain resolves to true, then all the given capabilities will be granted.
		/// </summary>
		/// <param name="verbNames"></param>
		/// <returns></returns>
		public Role ThenGrantVerb(params string[] verbNames)
		{
			var rootNode = Construct();

			// Grant the given set of caps to the given role
			// Using *duplicates* of this grant chain.
			// We duplicate in case people start directly using the grant chain on a particular capability
			// after applying a bulk if to a bunch of them.
			for (var i = 0; i < verbNames.Length; i++)
			{
				Role.GrantVerb(rootNode.Copy(), this, verbNames[i]);
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

			if (ParamValueResolvers == null)
			{
				// Sub-filters must share the same set (otherwise adds get left out).
				ParamValueResolvers = new List<FilterFieldEqualsValue>();
			}

			var chain = new Filter
			{
				Role = Role,
				ParamValueResolvers = ParamValueResolvers,
				DefaultType = DefaultType,
				FromRequest = FromRequest
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
			if (node is FilterFieldEqualsValue)
			{
				AddParamValueResolver((FilterFieldEqualsValue)node);
			}
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
		/// Copies the nodes from this filter into the given one.
		/// </summary>
		/// <param name="intoFilter"></param>
		public void CopyNodes(Filter intoFilter)
		{
			foreach (var node in Nodes)
			{
				intoFilter.Add(node.Copy());
			}
		}
		
		/// <summary>
		/// Copies this filter.
		/// </summary>
		/// <returns></returns>
		public virtual Filter Copy(bool withNodes = true)
		{
			var filter = new Filter()
			{
				Role = Role,
				DefaultType = DefaultType,
				FromRequest = FromRequest
			};
			if (withNodes)
			{
				CopyNodes(filter);
			}
			
			// Shallow sort copy:
			filter.Sorts = Sorts;

			return filter;
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
			CurrentStackTop = 0;

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