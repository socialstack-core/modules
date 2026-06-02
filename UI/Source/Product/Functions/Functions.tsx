import { Product } from 'Api/Product';
import { PriceCurrency } from 'Api/Payments';
import { Price } from 'Api/Price';

/**
 * returns pricing tiers for given product
 * @param product
 * @returns array of pricing tiers
 */
function getPriceTiers(product: Product) {
	var tiers : PriceCurrency[] = [];

	if (product?.calculatedPrice) {
		var calculatedPrice = product.calculatedPrice;

		if (calculatedPrice?.discountedPrice?.length > 0) {
			tiers = calculatedPrice.discountedPrice;
		} else if (calculatedPrice?.listPrice?.length > 0) {
			tiers = calculatedPrice.listPrice;
		}
	}

	return tiers;
}

/**
 * returns the relevant price tier matching the given product quantity
 * @param product
 * @param currentQuantity
 * @returns matching price tier index
 */
function getPriceTierForQuantity(product: Product, currentQuantity: number) {
	const priceTiers = getPriceTiers(product);

	// default if no tier matches
	let matchedIndex: int = 0 as int;

	priceTiers.forEach((tier: PriceCurrency, index: number) => {
		if (currentQuantity >= tier.minimumQuantity) {
			matchedIndex = index as int;
		}
	});

	return matchedIndex as int;
}

/**
 * returns the descriptive text for units of this product (e.g. "per pair", "per 50")
 * NB: ignores units such as EACH or SET
 * @param product
 * @returns descriptive text for sold units of the given product
 */
function getSellUnit(product: Product) {

	// NB: we have two methods of determining the selling unit; orderIncrements (numeric) and sellUnitDescription (text)
	// use orderIncrements if > 0, otherwise check sellUnitDescription (as these aren't always in sync)
	if (product.orderIncrements > 1) {
		return `per ${product.orderIncrements}`;
	}

	let sellUnit = "";

	if (product.sellUnitDescription && product.sellUnitDescription != "EACH" && product.sellUnitDescription != "SET") {
		sellUnit = `per ${product.sellUnitDescription.toLowerCase()}`;
	}

	return sellUnit;
}

export {
	getPriceTiers,
	getPriceTierForQuantity,
	getSellUnit
};