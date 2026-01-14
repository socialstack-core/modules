using Api.Startup;

namespace Api.Permissions{
	
	[ListAs("RolePermits", IsPrimary = false, Tab = "access")]
	[ListAs("RoleExclusions", IsPrimary = false, Tab = "access")]
	public partial class Role{}
	
}