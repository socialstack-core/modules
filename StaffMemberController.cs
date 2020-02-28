using Microsoft.AspNetCore.Mvc;

namespace Api.StaffMembers
{
    /// <summary>
    /// Handles Staff Member endpoints.
    /// </summary>

    [Route("v1/staffmember")]
	public partial class StaffMemberController : AutoController<StaffMember, StaffMemberAutoForm>
    {
    }
}