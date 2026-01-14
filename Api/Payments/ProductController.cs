using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;

namespace Api.Payments
{
    /// <summary>Handles product endpoints.</summary>
    [Route("v1/product")]
	public partial class ProductController : AutoController<Product>
    {
        /// <summary>
        /// Manually triggers the permalink sync.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="PublicException"></exception>
        [HttpGet("permalink/sync")]
        public async ValueTask<string> AdminTriggerPermalinkSync(Context context)
        {
            if (!context.Role.CanViewAdmin)
            {
                throw new PublicException("You do not have permission to view this endpoint", "permissions/not-admin");
            }

            if ((_service as ProductService).IsSyncRunning)
            {
                return "Sync already running";
            }

            await (_service as ProductService).SyncPermalinks(context);

            return "Content synced";
        }
    }
}