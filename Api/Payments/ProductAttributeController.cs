using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using static Api.Pages.PageController;
using System.Threading.Tasks;
using Api.Contexts;

namespace Api.Payments
{
    /// <summary>Handles productAttribute endpoints.</summary>
    [Route("v1/productAttribute")]
	public partial class ProductAttributeController : AutoController<ProductAttribute>
    {

		/// <summary>
		/// Gets information about a category tree node. The path is a 
		/// </summary>
		/// <param name="context"></param>
		/// <param name="location"></param>
		/// <returns></returns>
		/// <exception cref="PublicException"></exception>
		[HttpPost("tree")]
		public async ValueTask<TreeNodeDetail?> GetTreeNode(Context context, [FromBody] AttributeTreeLocation location)
		{
			return await GetTreeNodePath(context, location.Path);
		}

		/// <summary>
		/// Gets information about a category tree node at a path defined by slug/slug/.. . 
		/// Only actually the last slug matters.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		/// <exception cref="PublicException"></exception>
		[HttpGet("tree")]
		public async ValueTask<TreeNodeDetail?> GetTreeNodePath(Context context, [FromQuery] string path)
		{
			if (!context.Role.CanViewAdmin)
			{
				throw new PublicException("Admins only", "category_tree/admin_only");
			}

			return await (_service as ProductAttributeService).GetTreeNodeAtPath(context, path);
		}

	}

	/// <summary>
	/// A location in the attribute tree.
	/// </summary>
	public struct AttributeTreeLocation
	{
		/// <summary>
		/// The path to resolve relative to. Empty string (not /) indicates root set.
		/// </summary>
		public string Path;
	}

}