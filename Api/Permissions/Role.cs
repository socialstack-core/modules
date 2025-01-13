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
	[ListAs("composition", Explicit = true)]
	[ImplicitFor("composition", typeof(Role))]
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
		/// True if this role is composed from other roles. The child roles are in the composite set.
		/// </summary>
		public bool IsComposite;

		/// <summary>
		/// Admin dashboard JSON. Only sent to roles which can view the admin panel.
		/// </summary>
		public string AdminDashboardJson;

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
		public List<RoleGrantRule> GrantRules {
			get {
				return _grantRules;
			}
		}

		private List<RoleGrantRule> _grantRules = new List<RoleGrantRule>();

		/// <summary>
		/// Indexed by capability InternalId.
		/// </summary>
		private FilterBase[] CapabilityLookup { get; set; } = Array.Empty<FilterBase>();

		/// <summary>
		/// Grants the given capabilities unconditionally.
		/// </summary>
		/// <param name="capabilityNames"></param>
		/// <returns></returns>
		public Role GrantImportant(params string[] capabilityNames)
		{
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Single | RoleGrantRuleType.Important,
				Patterns = capabilityNames
			});

			return this;
		}

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
        public Role RevokeImportant(params string[] capabilityNames)
        {
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Revoke | RoleGrantRuleType.Single | RoleGrantRuleType.Important,
				Patterns = capabilityNames
			});
			
			return this;
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
		/// Revokes every capability. You can grant certain ones afterwards.
		/// </summary>
		/// <returns></returns>
		public Role RevokeEverything()
		{
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Revoke | RoleGrantRuleType.All
			});

			return this;
		}
		
		/// <summary>
		/// Grants every capability. You can revoke certain ones afterwards.
		/// </summary>
		/// <returns></returns>
		public Role GrantEverythingImportant()
        {
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.All | RoleGrantRuleType.Important
			});
			
			return this;
        }

		/// <summary>
		/// Revokes every capability. You can grant certain ones afterwards.
		/// </summary>
		/// <returns></returns>
		public Role RevokeEverythingImportant()
		{
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Revoke | RoleGrantRuleType.All | RoleGrantRuleType.Important
			});

			return this;
		}

		/// <summary>
		/// Adds the given rule to the grant set.
		/// </summary>
		/// <param name="rule"></param>
		/// <param name="addToStart"></param>
		public void AddRule(RoleGrantRule rule, bool addToStart = false)
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
			if(addToStart)
			{
				GrantRules.Insert(0, rule);
			}else{
				GrantRules.Add(rule);
			}
		}

		/// <summary>
		/// Removes all important marked rules from the role.
		/// </summary>
		public void ClearImportantRules()
		{
			// Capability lookup will need regeneration:
			CapabilityLookup = Array.Empty<FilterBase>();

			var ruleSet = new List<RoleGrantRule>();

			for (var i = 0; i < GrantRules.Count; i++)
			{
				var rule = GrantRules[i];
				if ((rule.RuleType & RoleGrantRuleType.Important) != RoleGrantRuleType.Important)
				{
					ruleSet.Add(rule);
				}
			}

			_grantRules = ruleSet;
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
		
		/// <summary>
		/// Grants the same perms as the given role. 
		/// If no other rules apply, the given role will be used.
		/// </summary>
		/// <param name="copyFrom"></param>
		/// <param name="addToStart"></param>
		/// <returns></returns>
		public Role GrantTheSameAsImportant(Role copyFrom, bool addToStart = false)
        {
			AddRule(new RoleGrantRule()
			{
				RuleType = RoleGrantRuleType.Role | RoleGrantRuleType.Important,
				SameAsRole = copyFrom
			}, addToStart);
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
		/// <param name="isIncluded">True if we're currently evaluating from within an included context.</param>
		/// <returns></returns>
		public async ValueTask<bool> IsGranted(Capability capability, Context context, object extraArg, bool isIncluded)
		{
			CheckForNewCapabilities();
			var handler = CapabilityLookup[capability.InternalId];

			if (handler == null)
            {
				return false;
			}

			// Ensure perm filter is prepped:
			if (handler.RequiresSetup)
			{
				await handler.Setup();
			}

			// Ask the handler:
			return handler.Match(context, extraArg, isIncluded);
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
		public Role ThenGrantImportant(params string[] capabilityNames)
		{
			Role.AddRule(
				new RoleGrantRule()
				{
					FilterQuery = FilterQuery,
					Patterns = capabilityNames,
					RuleType = RoleGrantRuleType.Single | RoleGrantRuleType.Important,
				}
			);
			return Role;
		}
		
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