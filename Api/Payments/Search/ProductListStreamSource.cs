using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Startup;

namespace Api.Payments
{
    /// <summary>
    /// A simple content stream source that wraps a list of products from a <see cref="ProductSearch"/> result.
    /// This implementation is less efficient than mainline services due to list allocation, but is suitable for
    /// quick implementations or infrequently used endpoints.
    /// </summary>
    /// <typeparam name="T">The type of content being streamed.</typeparam>
    /// <typeparam name="ID">The type of the content identifier.</typeparam>
    public class ProductListStreamSource<T, ID> : ContentStreamSource<T, ID>
        where T : Content<ID>, new()
        where ID : struct, IEquatable<ID>, IComparable<ID>, IConvertible
    {
        /// <summary>
        /// The underlying product search containing the products.
        /// </summary>
        public ProductSearch Target;

        /// <summary>
        /// Gets the enumerable list of content items.
        /// </summary>
        public IEnumerable<T> Content => (IEnumerable<T>)Target.Products;

        /// <summary>
        /// Constructs a new instance of <see cref="ProductListStreamSource{T, ID}"/>.
        /// </summary>
        /// <param name="content">The product search result to wrap.</param>
        public ProductListStreamSource(ProductSearch content)
        {
            Target = content;
        }

        /// <summary>
        /// Starts streaming the content items, invoking the given callback for each one.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <param name="filter">The content filter to apply (currently unused).</param>
        /// <param name="onResult">The callback to invoke for each item.</param>
        /// <param name="srcA">Optional source parameter A for callback context.</param>
        /// <param name="srcB">Optional source parameter B for callback context.</param>
        /// <returns>The total number of items in the original product search.</returns>
        public async ValueTask<int> GetResults(Context context, Filter<T, ID> filter, Func<Context, T, int, object, object, ValueTask> onResult, object srcA, object srcB)
        {
            if (Content == null)
            {
                return 0;
            }

            var counter = 0;

            foreach (var item in Content)
            {
                await onResult(context, item, counter, srcA, srcB);
                counter++;
            }

            return Target.Total;
        }

        /// <summary>
        /// Gets the next content stream source, if any.
        /// Always returns <c>null</c> for this implementation.
        /// </summary>
        public SecondaryContentStreamSource GetNextSource()
        {
            return null;
        }
    }
}
