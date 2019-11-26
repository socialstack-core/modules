using System;
using System.Text;
using Api.Contexts;


namespace Api.Permissions
{
	/*
	 * This filter is experimental. Generating the query results in an awkward API as it 
	 * needs to be able to map filters through to filters on the actual target entity type, however, the usage for
	 * regular permission grants is easy to read and notably useful.
	 * 
	/// <summary>
	/// A filter method which grants if the named capability is granted.
	/// </summary>
	public class FilterIfAlsoGranted : FilterNode
	{
		/// <summary>
		/// The role to check.
		/// </summary>
		public Role Role;
		/// <summary>
		/// The capability that we're looking for.
		/// </summary>
		public Capability Capability;
		/// <summary>
		/// Maps given args to the args expected by the capability.
		/// </summary>
		public Func<object[], object[]> ArgMapper;


		/// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override bool IsGranted(Capability cap, LoginToken token, object[] extraArgs)
		{
			if(ArgMapper == null){
				// Pass the args as-is:
				return Role.IsGranted(Capability, token, extraArgs);
			}
			
			// Otherwise map the args:
			return Role.IsGranted(Capability, token, ArgMapper(extraArgs));
		}

		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// If a variable is outputted then a value reader is pushed in the given arg set.
		/// </summary>
		/// <param name="builder"></param>
		public override void BuildQuery(StringBuilder builder)
		{
			Role.BuildQuery(Capability, builder);
		}
		
		/// <summary>
		/// Copies this filter node.
		/// </summary>
		/// <returns>A deep copy of the node.</returns>
		public override FilterNode Copy()
		{
			return new FilterIfAlsoGranted()
			{
				Role = Role,
				Capability = Capability,
				ArgMapper = ArgMapper
			};
		}
	}

	public partial class Filter
	{

		/// <summary>
		/// Used to grant a capability if some other capability is granted. Use like this:
		/// MyRole.GrantIf("cap_to_grant", MyRole.IsAlsoGranted("cap_to_check_if_granted")).
		/// Optionally provide a mapper which maps the args for the first capability to the args of the second one. 
		/// If not provided, the args are passed through as-is.
		/// </summary>
		public Filter IsGranted(string capabilityName, Func<object[], object[]> argMapper = null)
		{
			// First get the capability:
			Capability cap;
			if (!Capabilities.All.TryGetValue(capabilityName.ToLower(), out cap))
			{
				// If you ended up here, please check to that you're instancing a capability with the given name 
				// (it's probably a typo in your grant call).
				throw new Exception("Role '" + Role.Name + "' checks if '" + capabilityName + "' is also granted but that capability wasn't found.");
			}

			Add(new FilterIfAlsoGranted()
			{
				Role = Role,
				ArgMapper = argMapper,
				Capability = cap
			});

			return this;
		}

	}
	*/
}