using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.PasswordResetRequests
{
    /// <summary>Handles passwordResetRequest endpoints.</summary>
    [Route("v1/passwordResetRequest")]
	public partial class PasswordResetRequestController : AutoController<PasswordResetRequest>
    {
		
		/// <summary>
		/// Admin link generation.
		/// </summary>
		[HttpGet("{id}/generate")]
		public async Task<object> Generate(int id)
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}
			
			// must be admin/ super admin. Nobody else can do this for very clear security reasons.
			if(context.Role != Roles.SuperAdmin && context.Role != Roles.Admin)
			{
				return null;
			}
			
			// Create token:
			var prr = await _service.Create(context, new PasswordResetRequest(){
				UserId = id
			});
			
			if(prr == null){
				return null;
			}
			
			return new {
				token = prr.Token,
				url = "/password/reset/" + prr.Token
			};
		}
		
    }
}