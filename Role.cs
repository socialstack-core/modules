using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Api.AutoForms;
using Api.Contexts;
using Api.Database;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json;

namespace Api.Permissions
{
    /// <summary>
    /// A role which defines a set of capabilities to a user who is granted this particular role.
    /// </summary>
    public partial class Role : VersionedContent<uint>
	{
		/// <summary>
		/// The nice name of the role, usually in the site default language.
		/// </summary>
        public string Name;
		/// <summary>
		///  The role key - usually the lowercase, underscores instead of spaces variant of the first set name.
		///  This shouldn't change after it has been set.
		/// </summary>
        public string Key;
		
		/// <summary>
		/// True if this role can view the admin panel.
		/// </summary>
		public bool CanViewAdmin;

		/// <summary>
		/// The raw grant rules for this user role.
		/// </summary>
		[Module("Admin/PermissionGrid")]
		[Data("editor", "true")]
		public string GrantRuleJson;

		/// <summary>
		/// A role that this one inherits from. If non-zero, a GrantTheSameAs grant is applied.
		/// </summary>
		public uint InheritedRoleId;

		/// <summary>
		/// The raw grant rules, sorted by priority (weakest first). Evaluated against only when new capabilities are added.
		/// </summary>
		[JsonIgnore]
		public List<RoleGrantRule> GrantRules { get; } = new List<RoleGrantRule>();

		/// <summary>
		/// Indexed by capability InternalId.
		/// </summary>
		private FilterBase[] CapabilityLookup { get; set; } = Array.Empty<FilterBase>();

		/// <summary>
		/// Grants the given capabilities unconditionally.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
		public Role Grant(params string[] capabilityNames)
		{
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Single,
				Patterns = capabilityNames
			});

			return this;
		}

		/// <summary>
		/// Grants the given features unconditionally. Any capability that uses this feature will be granted.
		/// For example, GrantFeature("Load") will permit User Load, ForumReply Load etc.
		/// </summary>
		/// <param name="features"></param>
		/// <returns></returns>
		public Role GrantFeature(params string[] features)
		{
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Feature,
				Patterns = features
			});

			return this;
		}

		/*
		/// <summary>
		/// Adds a PermittedContent requirement to the given capability.
		/// </summary>
		/// <param name="cap"></param>
		/// <param name="contentType"></param>
		/// <param name="contentTypeId">The content type ID of the permit. This means a content type can use permits from another type.
		/// For example, permits on a private chat can be used directly by private chat *messages*.</param>
		/// <param name="field">The field on ContentType that will be checked for a permit.</param>
		public void AddHasPermit(Capability cap, Type contentType, int contentTypeId, string field = "Id")
		{
			ResizeIfRequired(cap.InternalId);
			
			var filter = CapabilityFilterLookup[cap.InternalId];
			if (filter == null)
			{
				filter = new Filter(contentType)
				{
					Role = this
				};
				CapabilityFilterLookup[cap.InternalId] = filter;
			}

			if (filter.HasContent)
			{
				filter.And();
			}

			// Must have a PermittedContent obj for the contextual user.
			// This means for each permit on the content object being filtered, 
			// check if at least one belongs to the contextual user.
			filter.Equals(field, async (Context context) => {

				if (_permittedContents == null)
				{
					userContentTypeId = ContentTypes.GetId(typeof(User));
					_permittedContents = Services.Get<PermittedContentService>();
				}

				// All permits for this user for the given type of content.
				var permits = await _permittedContents.List(context,
					new Filter<PermittedContent>()
						.Equals("PermittedContentId", context.UserId)
						.And()
						.Equals("PermittedContentTypeId", userContentTypeId)
						.And()
						.Equals("ContentTypeId", contentTypeId)
				);

				if (permits == null || permits.Count == 0)
				{
					// User is not permitted to any of this content. Just return a 0:
					return 0;
				}

				// User is permitted to something of this type.
				// Can be hundreds of results, so at this point build a dictionary.
				// When the cache is running objects through the Equals node of the filter, 
				// it knows to perform a lookup in <int, bool> dictionaries.
				var results = new ContentIdLookup()
				{
					ContentTypeId = contentTypeId
				};

				foreach (var permit in permits)
				{
					results.Add(permit.ContentId);
				}

				return results;
			});

			CapabilityLookup[cap.InternalId] = filter.Construct(true);
		}

		
		/// <summary>
		/// Extends a cap filter with a role restriction. Note that this happens after e.g. GrantTheSameAs calls, because it's always role specific.
		/// </summary>
		/// <param name="cap"></param>
		/// <param name="contentType"></param>
		/// <param name="fieldName"></param>
		public void AddRoleRestrictionToFilter(Capability cap, Type contentType, string fieldName)
		{
			ResizeIfRequired(cap.InternalId);

			var filter = CapabilityFilterLookup[cap.InternalId];
			if (filter == null)
			{
				filter = new Filter(contentType) {
					Role = this
				};
				CapabilityFilterLookup[cap.InternalId] = filter;
			}

			if (filter.HasContent)
			{
				filter.And();
			}

			filter.Equals(contentType, fieldName, true);
			CapabilityLookup[cap.InternalId] = filter.Construct(true);
		}
		
		/// <summary>
		/// The content type ID of User. ContentTypes.GetId(typeof(User));
		/// </summary>
		private static int userContentTypeId;
		
		/// <summary>
		/// The permitted content service.
		/// </summary>
		private static PermittedContentService _permittedContents;
		
		*/

		/// <summary>
		/// Start conditional grants. For example, theRole.If("IsSelf()").ThenGrant("user_update") - 
		/// this means if the current user is the user being edited then the permission is granted.
		/// </summary>
		/// <returns></returns>
		public RoleIfResolver If(string filterQuery)
		{
			return new RoleIfResolver() { Role = this, FilterQuery = filterQuery };
		}
		
		/// <summary>
		/// Revokes the named capabilities. Often used when merging or copying from roles.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
        public Role Revoke(params string[] capabilityNames)
        {
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Revoke | RoleGrantRuleType.Single,
				Patterns = capabilityNames
			});
			
			return this;
        }

		/// <summary>
		/// Grants every capability. You can revoke certain ones afterwards.
		/// </summary>
		/// <returns></returns>
		public Role GrantEverything()
        {
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.All
			});
			
			return this;
        }

		/// <summary>
		/// Adds the given rule to the grant set.
		/// </summary>
		/// <param name="rule"></param>
		public void AddRule(RoleGrantRule rule)
		{
			// If it has patterns, make sure they're lowercase:
			if (rule.Patterns != null)
			{
				for (var i = 0; i < rule.Patterns.Length; i++)
				{
					rule.Patterns[i] = rule.Patterns[i].ToLower();
				}
			}

			// Add rules in the order that they were requested.
			GrantRules.Add(rule);
		}

		/// <summary>
		/// Revokes all caps which are for the given feature.
		/// </summary>
		/// <param name="features"></param>
		/// <returns></returns>
		public Role RevokeFeature(params string[] features)
		{
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Revoke | RoleGrantRuleType.Feature,
				Patterns = features
			});

			return this;
		}

		/// <summary>
		/// Grants the same perms as the given role. 
		/// If no other rules apply, the given role will be used.
		/// </summary>
		/// <param name="copyFrom"></param>
		/// <returns></returns>
		public Role GrantTheSameAs(Role copyFrom)
        {
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Role,
				SameAsRole = copyFrom
			});
            return this;
        }
		
		/// <summary>Gets the raw grant rule for the given capability. This is readonly. If it's null, it's not granted.</summary>
		public FilterBase GetGrantRule(Capability capability)
		{
			CheckForNewCapabilities();
			return CapabilityLookup[capability.InternalId];
		}

		/// <summary>
		/// Is the given capability granted to this role?
		/// </summary>
		/// <param name="capability">The capability to check for. This is required.</param>
		/// <param name="context">The requesting context.</param>
		/// <param name="extraArg">
		/// E.g. the Forum object to check if access is granted for.
		/// </param>
		/// <returns></returns>
		public bool IsGranted(Capability capability, Context context, object extraArg)
		{
			CheckForNewCapabilities();
			var handler = CapabilityLookup[capability.InternalId];

            if (handler == null)
            {
				return false;
			}

            // Ask the handler:
            return handler.Match(context, extraArg, handler);
        }

		private void CheckForNewCapabilities()
		{
			var max = Capability.MaxCapId;
			if (CapabilityLookup != null && CapabilityLookup.Length == max)
			{
				return;
			}

			// Resize the buffers:
			var capLookup = CapabilityLookup;
			Array.Resize(ref capLookup, max);
			CapabilityLookup = capLookup;

			// For each capability, make sure it's added:
			foreach (var capability in Capabilities.GetAllCurrent())
			{
				if (CapabilityLookup[capability.InternalId] == null)
				{
					// Add (or re-add):
					AddCapability(capability);
				}
			}
		}

		/// <summary>
		/// Adds the given new capability to the role
		/// </summary>
		/// <param name="capability"></param>
		private void AddCapability(Capability capability)
		{
			// Clear:
			CapabilityLookup[capability.InternalId] = null;

			var rule = GetActiveRule(capability);

			if (rule == null || (rule.RuleType & RoleGrantRuleType.Revoke) == RoleGrantRuleType.Revoke)
			{
				// Deny
				return;
			}

			// Otherwise we're adding it.
			var filter = capability.Service.GetGeneralFilterFor(rule.FilterQuery, true);
			CapabilityLookup[capability.InternalId] = filter;
		}

		/// <summary>
		/// Gets the active grant rule on this role for the given capability. 
		/// Null if there isn't one and the grant is the base default (false).
		/// </summary>
		/// <param name="capability"></param>
		/// <returns></returns>
		public RoleGrantRule GetActiveRule(Capability capability)
		{
			// Now evaluate each rule in this role (they're in add order, i.e. rules added first are overriden by rules added later).
			for(var i=GrantRules.Count - 1; i>=0;i--)
			{
				var rule = GrantRules[i];

				// Do the patterns in the rule match this capability?
				rule = rule.AsAppliedTo(capability);
				if (rule == null)
				{
					continue;
				}

				return rule;
			}

			return null;
		}

	}

	/// <summary>
	/// Resolves If() statements on roles.
	/// </summary>
	public class RoleIfResolver
	{
		/// <summary>
		/// The role that this is for.
		/// </summary>
		public Role Role;

		/// <summary>
		/// The underlying filter query.
		/// </summary>
		public string FilterQuery;


		/// <summary>
		/// If the previous chain resolves to true, then all the given capabilities will be granted.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
		public Role ThenGrant(params string[] capabilityNames)
		{
			Role.AddRule(
				new RoleGrantRule()
				{
					FilterQuery = FilterQuery,
					Patterns = capabilityNames,
					RuleType = RoleGrantRuleType.Single,
				}
			);
			return Role;
		}

		/// <summary>
		/// If the previous chain resolves to true, then all the given capabilities will be granted.
		/// </summary>
		/// <param name="features"></param>
		/// <returns></returns>
		public Role ThenGrantFeature(params string[] features)
		{
			Role.AddRule(
				new RoleGrantRule()
				{
					FilterQuery = FilterQuery,
					Patterns = features,
					RuleType = RoleGrantRuleType.Feature,
				}
			);
			return Role;
		}

	}

	/// <summary>
	/// Used to define a method which returns true/ false depending on if a capability should be granted.
	/// </summary>
	/// <param name="capability"></param>
	/// <param name="token"></param>
	/// <param name="extraObjectsToCheck"></param>
	/// <returns></returns>
	public delegate bool IsGrantedHandler(Capability capability, Context token, object[] extraObjectsToCheck);

	/// <summary>
	/// A raw role grant rule. Capabilities are tested against these rules when a cap or role is added.
	/// </summary>
	public class RoleGrantRule
	{
		/// <summary>
		/// The base type of this grant rule, defining the scope of the patterns. Rules are sorted by the numeric version of this.
		/// </summary>
		public RoleGrantRuleType RuleType;

		/// <summary>
		/// A filter query string.
		/// </summary>
		public string FilterQuery;

		/// <summary>
		/// The raw pattern set inside this rule. Evaluating against these is relatively slow, 
		/// however, it is only evaluated once when a capability is seen for the first time.
		/// </summary>
		public string[] Patterns;

		/// <summary>
		/// Grant the same as this named role. Defines a base inheritence rule, which is the weakest of all rules.
		/// </summary>
		public Role SameAsRole;

		/// <summary>
		/// Returns a rule as applied to the given capability. Essentially if this rule does not apply, it returns null.
		/// It can inherit from other roles, thus the actual rule returned is not necessarily this one.
		/// </summary>
		/// <param name="cap"></param>
		/// <returns></returns>
		public RoleGrantRule AsAppliedTo(Capability cap)
		{
			if ((RuleType & RoleGrantRuleType.Role) == RoleGrantRuleType.Role)
			{
				var baseRule = SameAsRole.GetActiveRule(cap);

				if (baseRule != null)
				{
					return baseRule.AsAppliedTo(cap);
				}

				// No other options valid. Return here.
				return null;
			}

			if ((RuleType & RoleGrantRuleType.All) == RoleGrantRuleType.All)
			{
				return this;
			}

			if ((RuleType & RoleGrantRuleType.Feature) == RoleGrantRuleType.Feature)
			{
				foreach (var pattern in Patterns)
				{
					if (cap.Feature == pattern)
					{
						return this;
					}
				}
			}

			if ((RuleType & RoleGrantRuleType.Single) == RoleGrantRuleType.Single)
			{
				foreach (var pattern in Patterns)
				{
					if (cap.Name == pattern)
					{
						return this;
					}
				}
			}

			return null;
		}
	}

	/// <summary>
	/// The type of a role grant rule.
	/// </summary>
	public enum RoleGrantRuleType : int
	{
		/// <summary>
		/// Grant the same as the given role. Revoke is not valid with this.
		/// </summary> 
		Role = 4,
		/// <summary>
		/// All capabilities will be affected by the rule.
		/// </summary>
		All = 8,
		/// <summary>
		/// Any capability declaring a particular feature will be affected by the rule.
		/// </summary>
		Feature = 16,
		/// <summary>
		/// A single, specific capability is affected by the rule.
		/// </summary>
		Single = 32,
		/// <summary>
		/// Revoke can be combined with any of the other options. Declares that this rule clears the current grant, if there is one.
		/// For example, revoke all is resolved before revoke single is, but after a permit all.
		/// </summary>
		Revoke = 64,
		/// <summary>
		/// Inspired by how CSS selectivity works.
		/// In this case an important rule is one which is defined by the admin panel, and overrides any of the rules that originate from code.
		/// </summary>
		Important = 128
	}

}