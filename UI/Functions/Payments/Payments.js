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

/**
 * Calculates the price of the given product in the given quantity. You *must* include price, tiers and tiers.price for this to be fully available.
 * @param {any} product
 * @param {any} quantity
 */
const calculatePrice = (product, quantity) => {
	
	if (quantity < product.minQuantity) {
		// Round:
		quantity = product.minQuantity;
	}
	
	// Get the product tiers (null if there are none):
	var tiers = product.tiers;
	var prodToUse = product;
	var totalCost;

	// Get the base price:
	var price = prodToUse.price;

	if (price == null) {
		return;
	}

	if (tiers && tiers.length > 0) {

		if (product.minQuantity > tiers[0].minQuantity) {
			tiers[0].minQuantity = product.minQuantity + 1;
        }

		// Which tier is the quantity going to target?
		var targetTier = -1;

		for (var i = tiers.length - 1; i >= 0; i--) {
			if (quantity >= tiers[i].minQuantity) {
				// Using this tiered product.
				targetTier = i;
				break;
			}
		}

		if (targetTier == -1) {
			// Within the base price. Doesn't matter what the price strategy is - this is always the same.

			// Total cost is:
			totalCost = quantity * price.amount;
		} else {
			// See the wiki for details on pricing strategies.
			switch (product.priceStrategy) {
				case 0:
					// Standard pricing strategy.

					// You pay the base product rate unless quantity passes any of the thresholds in the tiers.
					prodToUse = tiers[targetTier];

					// Get the price:
					price = prodToUse.price;

					if (!price) {
						return;
					}
					
					// Total cost is:
					totalCost = quantity * price.amount;
					
					break;
				case 1:
					// Step once.

					// You pay the base product rate unless quantity passes any of the thresholds in the tiers.

					// The step - we know we're above at least the threshold of the first tier.
					var excessThreshold = tiers[0].minQuantity;

					// Add base number of products.
					totalCost = (excessThreshold - 1) * price.amount;

					// Get the excess:
					var excess = quantity - (excessThreshold - 1);

					// Next establish which tier the price is in:
					prodToUse = tiers[targetTier];

					// Get the price:
					price = prodToUse.price;

					if (price == null)
					{
						return;
					}

					// Add excess number of items to the total, ensuring that we don't overflow.
					var excessCost = excess * price.amount;
					
					var origTotal = totalCost;
					totalCost += excessCost;

					break;
				case 2:
					// Step always.

					// Base price first:
					excessThreshold = tiers[0].minQuantity;

					// Add base number of products.
					totalCost = (excessThreshold - 1) * price.amount;

					// Handle each fully passed tier next.
					for (var i = 0; i < targetTier; i++) {
						// The max amt for this tier is the following tiers min minus this tiers min.
						var tier = tiers[i];
						var max = tiers[i + 1].minQuantity - tier.minQuantity;
						price = tier.price;
						var tierTotal = max * price.amount;
						// A singular tier is expected to never be so large that it always overflows.
						// Adding it on however might do so.
						var prevTotal = totalCost;
						totalCost += tierTotal;
					}

					// Handle any final excess.
					prodToUse = tiers[targetTier];
					excess = quantity - (prodToUse.minQuantity - 1);

					price = prodToUse.price;

					if (price == null) {
						return;
					}

					excessCost = excess * price.amount;
					origTotal = totalCost;
					totalCost += excessCost;
					break;
				default:
					return;
			}
		}
	} else {
		// Just a simple price * qty.

		// Get the price:
		price = prodToUse.price;

		if (!price){
			return;
		}

		// Total cost is:
		totalCost = quantity * price.amount;
	}
	
	return {
		currencyCode: price.currencyCode,
		amount: totalCost
	};
}

const recurrenceText = (billingFreq) => {
	switch(billingFreq){
		case 0:
			return '';
		case 1:
			return `/ week`;
		case 2:
			return `/ month`;
		case 3:
			return `/ quarter`;
		case 4:
			return `/ year`;
	}
}


export {
	renderProducts,
	renderAllProducts,
	renderTieredProducts,
	calculatePrice,
	recurrenceText
};
