import Loop from 'UI/Loop';

/**
 * renders a list of products
 * @param {any} renderCallback
 * @param {any} options
 * 
 * options can contain:
 * {
 *   // Loop options (sort, where, includes, etc)
 *   filter: { ... },
 *   includes: { ... }
 *   
 *   includeTiered: true/false (optionally include tiered products - excluded by default)
 * }
 */
const renderProducts = (renderCallback, options) => {
	options = options || {};

	options.filter = options.filter || {};
	options.filter.sort = options.filter.sort || { field: 'name' };
	options.filter.where = options.filter.where || {};

	if (!options.includeTiered) {
		options.filter.where.tierOfId = { equals: 0 };
    }

	options.includes = options.includes || ['tiers', 'tiers.price', 'price'];

	return <Loop over="product" raw filter={options.filter} includes={options.includes}>
		{
			(product) => {

				// filter any tiered products
				if (product.tiers.length && !options.includeTiered) {
					return;
				}

				return renderCallback(product);
			}
		}
	</Loop>;
};

/**
 * renders a list of all products
 * @param {any} renderCallback
 * @param {any} options
 *
 * NB: tiered products are always included
 */
const renderAllProducts = (renderCallback, options) => {
	options = options || {};
	options.filter = options.filter || {};
	options.includeTiered = true;

	return renderProducts(renderCallback, options);
};

/**
 * renders a list of tiered products
 * @param {any} renderCallback
 * @param {any} options
 */
const renderTieredProducts = (renderCallback, options) => {
	options = options || {};

	options.filter = options.filter || {};
	options.filter.sort = options.filter.sort || { field: 'minQuantity' };
	options.includes = options.includes || ['tiers', 'tiers.price', 'price'];

	return <Loop over="product" raw filter={options.filter} includes={options.includes}>
		{
			(product) => {

				if (product.tiers.length) {
					return renderCallback(product);
                }
			}
		}
	</Loop>;
};

/*
const renderSelectableProduct = (renderCallback, session, product, allowMultiple) => {
	var productId = "product_" + product.id;
	var productName = product.name;
	// TODO: check price strategy
	var cost = formatCurrency(product.price.amount * product.minQuantity, session.locale, { hideDecimals: true });
	// TODO: support differing frequencies
	var recurrence = ` pm`;

	return <input className="btn-check" id={productId} autocomplete="off"
				type={allowMultiple ? "checkbox" : "radio"}
				name={allowMultiple ? undefined : "productOption"}
				data-id={product.id}
				data-name={productName}
				onChange={(e) => updateProductSelection(e)} />
			<label className="btn btn-outline-secondary product-option" htmlFor={productId}>
				<span className="product-option__name">
					{productName}
				</span>
				<span className="product-option__price">
					{cost}
					{recurrence}
				</span>
			</label>

};
*/

export {
	renderProducts,
	renderAllProducts,
	renderTieredProducts
};
