using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Api.Pages.PageController;

namespace Api.Payments
{
	/// <summary>
	/// The base category controller extends the auto controller
	/// and serves as a framework for a category class.
	/// </summary>
	/// <typeparam name="TCategory"></typeparam>
	/// <typeparam name="TCategoryNode"></typeparam>
	/// <typeparam name="TService"></typeparam>
    public abstract class BaseCategoryController<TCategory, TCategoryNode, TService> : AutoController<TCategory>
            where TCategory : BaseCategory, new()
            where TCategoryNode : CategoryNode<TCategory, TCategoryNode>, new()
            where TService : BaseCategoryService<TCategory, TCategoryNode>
    {
	    /// <summary>
	    /// Holds reference to the service.
	    /// </summary>
        protected readonly TService _categoryService;
		
        /// <summary>
        /// Constructor, autowires the service.
        /// </summary>
        /// <param name="service"></param>
        protected BaseCategoryController(TService service)
        {
            _categoryService = service;
        }

        /// <summary>
        /// List the entire product category structure
        /// </summary>
        /// <param name="context"></param>
        /// <param name="includeProducts"></param>
        /// <returns></returns>
        [HttpGet("structure")]
		public virtual async ValueTask<List<TCategoryNode>> Structure(Context context, [FromQuery] bool includeProducts = false)
		{
			if (includeProducts)
			{
				if (context.Role == null || !context.Role.CanViewAdmin)
				{
					throw new PublicException("Admin only", "Product Category/admin_required");
				}

				var slowTree = await _categoryService.GetProductTreeAndProducts(context);
				return slowTree.Roots;
			}
			else
			{
				var tree = await _categoryService.GetTree(context);
				return tree.Roots;
			}
		}
		
        /// <summary>
        /// Permalink sync endpoint.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="PublicException"></exception>
        [HttpGet("permalink/sync")]
        public async ValueTask<string> PermalinkSync(Context context)
        {
            if (context.Role == null || !context.Role.CanViewAdmin)
            {
                throw new PublicException("You do not have permission to view this endpoint", "permissions/not-admin");
            }

            if (_categoryService.IsSyncRunning)
            {
                return "Sync already running";
            }

            await _categoryService.SyncPermalinks(context);

            return "Content synced";
        }



        /// <summary>
        /// Gets information about a category tree node. The path is a 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="PublicException"></exception>
        [HttpPost("tree")]
		public async ValueTask<TreeNodeDetail?> GetTreeNode(Context context, [FromBody] CategoryTreeLocation location)
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

			return await _categoryService.GetTreeNodeAtPath(context, path);
		}
		
		/// <summary>
		/// List the products for a category
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("{id}/products")]
		public virtual async ValueTask<List<Product>> GetProducts(Context context, [FromRoute] uint id)
		{
			return await _categoryService.GetProducts(context, id);
		}

		/// <summary>
		/// List the categories for a product
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("product/{id}")]
		public virtual async ValueTask<List<TCategory>> GetProductCategories(Context context, [FromRoute] uint id)
		{
			return await _categoryService.GetProductCategories(context, id);
		}

		/// <summary>
		/// List the children for a category (equiv to a filter: ParentId=id)
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("{id}/children")]
		public virtual async ValueTask<List<TCategoryNode>> GetChildren(Context context, [FromRoute] uint id)
		{
			return await _categoryService.GetChildren(context, id);
		}


        /// <summary>
        /// List the parents for a category
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}/parents")]
		public virtual async ValueTask<List<TCategoryNode>> GetParents(Context context, [FromRoute] uint id)
		{
			return await _categoryService.GetParents(context, id);
		}
	    
		/// <summary>
		/// Gets category descendants as a flat array
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("{id}/descendants")]
		public async ValueTask<ContentStream<TCategory, uint>> GetDescendants(Context context, [FromRoute] uint id)
		{
			return new ContentStream<TCategory, uint>(await _categoryService.GetChildrenAsFlatList(context, id), _categoryService);
		}
    }
}