import { Product } from 'Api/Product';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { getSellUnit } from "UI/Product/Functions";

/**
 * Props for the Price component.
 * @icon fal fa-tag
 * @description Displays a product price with optional tax display and sell unit.
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
     * Show price with or without tax
     */
    lessTax?: boolean

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
const Price: React.FC<PriceProps> = ({lessTax = true, showSellUnit = true, ...props}) => {
	const {product, override } = props;

    let currencyCode = override?.currencyCode || 'GBP';
    let amount = override?.amount || 0   ;

	//Add unis of product in question
    let sellUnit = showSellUnit ? getSellUnit(product) : null;

	return (
		<span className="ui-product-price">
			{formatCurrency(amount, { currencyCode })}
			<span className="white-space--nowrap">{lessTax ? `ex VAT` : `inc VAT`} {sellUnit}</span>
		</span>
	);
}

export default Price;