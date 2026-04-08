import { Product } from 'Api/Product';
import { useSession } from 'UI/Session';
import { formatCurrency, formatPOA } from "UI/Functions/CurrencyTools";
import { useCart } from 'UI/Payments/CartSession';
import { getPriceTiers, getPriceTierForQuantity, getSellUnit } from "UI/Product/Functions";

/**
 * Props for the Price component.
 */
interface PriceProps {
	/**
	 * The content to display
	 */
	product: Product,

	/**
	 * Overriding price to display
	 */
	override?: CurrencyAmount,

	/**
	 * Overriding quantity to display
	 */
	qtyOverride?: Number,

	/**
	 * true if this a "from" price (i.e. the product has variants)
	 */
	isFrom?: boolean,

	/**
	 * true if "from" and "was" pricing should be omitted (used on order view)
	 */
	currentPriceOnly?: boolean,

	/**
	 * optional multiplier (used to show total value for [n] items - see order view)
	 */
	multiple?: int

	/**
	 * Should we show the sell units? Defaults to true
	 */
	showSellUnit: boolean
}

export interface CurrencyAmount {
	currencyCode: string,
	amount: ulong
}

/**
 * The Price React component.
 * @param props React props.
 */
const Price: React.FC<PriceProps> = ({showSellUnit = true, ...props}) => {
	const { product, override, qtyOverride, isFrom, currentPriceOnly, multiple } = props;
	const { session } = useSession();
	const { lessTax, getCartQuantity } = useCart();
	const { locale } = session;

	// current quantity of this product in the basket
	const quantity = qtyOverride || (getCartQuantity ? getCartQuantity(product?.id ?? 0) : 0);

	let currencyCode: string | undefined;
	let amount: ulong | undefined;
	let hasOptions = false;

	if (override) {
		currencyCode = override.currencyCode;
		amount = override.amount;
		hasOptions = !!isFrom;
	} else {
		var tiers = getPriceTiers(product);
		currencyCode = locale?.currencyCode || 'GBP';

		if(!tiers.length){
			return (
				<span className="ui-product-price">
					{formatPOA({ currencyCode })}
				</span>
			);
		}

		hasOptions = tiers.length > 1;

		// get the price tier associated with the current quantity
		// (0 represents default price tier to use if nothing matches)
		const tierIndex = getPriceTierForQuantity(product, quantity);
		var tier = tiers[tierIndex];

		amount = lessTax ? tier.amountLessTax : tier.amount;
	}

	// TODO: optional previous price
	let oldPrice = 0;

	// check - show total for multiple items?
	if (amount && multiple) {
		amount = (multiple * amount) as int;
	}

	//Add unis of product in question
	let sellUnit = showSellUnit ? getSellUnit(product) : null;

	return (
		<span className="ui-product-price">
			{!currentPriceOnly && hasOptions && <span className="ui-product-price--from">{`From`}</span>}
			{formatCurrency(amount, { currencyCode })}
			<span className="white-space--nowrap">{lessTax ? `ex VAT` : `inc VAT`} {sellUnit}</span>
			{!currentPriceOnly && oldPrice > 0 && <span className="ui-product-price--was">{`Was ${formatCurrency(oldPrice, { currencyCode })}`}</span>}
		</span>
	);
}

export default Price;