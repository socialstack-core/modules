using System.Linq;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles productAttributeValue endpoints.</summary>
    [Route("v1/productAttributeValue")]
	public partial class ProductAttributeValueController : AutoController<ProductAttributeValue>
    {
		/// <summary>
		/// This endpoint has been added in order to
		/// perform a cleanup operation on orphaned
		/// values. Its access is limited strictly
		/// to Admin &amp; Developer users.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="httpContext"></param>
		/// <returns></returns>
		[HttpGet("cleanup")]
        public async ValueTask<OrphanedAttributeCleanupResponse?> DeleteOrphanedAttributeValues(Context context, HttpContext httpContext)
        {
            if (context.Role != Roles.Admin && context.Role != Roles.Developer)
            {
                // 404's 
                return null;
            }
            
            // load the required services in
            var attributeValueSvc = Services.Get<ProductAttributeValueService>();
            var attributeSvc      = Services.Get<ProductAttributeService>();
            
            // load all attribute values
            var allAttributeValues = await attributeValueSvc.Where().ListAll(context);
            // load all attributes
            var allAttributes = await attributeSvc.Where().ListAll(context);

            // create the response value.
            var response = new OrphanedAttributeCleanupResponse
            {
                // displays the total attribute count
                AllAttributesCount = allAttributes.Count,
                // displays the value count
                AllValuesCount = allAttributeValues.Count,
                // displays the amount cleaned up.
                CleanedUpValuesCount = 0
            };

            foreach (var value in allAttributeValues)
            {
                if (allAttributes.All(attribute => attribute.Id != value.ProductAttributeId))
                {
                    await attributeValueSvc.Delete(context, value);
                    response.CleanedUpValuesCount++;
                }
            }

            return response;
        }
    }
    /// <summary>
    /// The return structure for the cleanup operation above
    /// </summary>
    public struct OrphanedAttributeCleanupResponse
    {
        /// <summary>
        /// Total value count
        /// </summary>
        public int AllValuesCount { get; set; }
        
        /// <summary>
        /// Total attribute count
        /// </summary>
        public int AllAttributesCount { get; set; }
        
        /// <summary>
        /// Total cleaned up count
        /// </summary>
        public int CleanedUpValuesCount { get; set; }
    }
}