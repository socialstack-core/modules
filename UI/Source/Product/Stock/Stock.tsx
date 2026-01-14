import { Product } from 'Api/Product';

/**
 * Props for the Stock component.
 */
interface StockProps {
	/**
	 * The content to display
	 */
	product: Product,
}

/**
 * The Stock React component.
 * @param props React props.
 */
const Stock: React.FC<StockProps> = (props) => {
	const { product } = props;

	// TODO: determine when product has options
	let hasOptions = false;
	let options = 12;
	let approved = 4;

	const stockLevel = product.stock;

	const stockInfoClass = [
		'ui-product-stock__info',
		stockLevel ? 'ui-product-stock__info--in-stock' : 'ui-product-stock__info--out-of-stock'];

	return <div className="ui-product-stock">
		<div className="ui-product-stock__wrapper">
			{!hasOptions && <>
				<span className="ui-product-stock__sku">
					{/*
					<i className="fr fr-barcode"></i>
					*/}
					{product.sku}
				</span>
				<span className={stockInfoClass.join(' ')}>
					{stockLevel > 0 && <>
						{/*
						<i className="fr fr-check-circle"></i>
						*/}
						{`${stockLevel} in stock`}
					</>}
					{!stockLevel && <>
						<i className="fr fr-exclamation-circle"></i>
						{`Available to order (non-stock item)`}
					</>}
				</span>
			</>}

			{hasOptions && <>
				<span className="ui-product-stock__options">
					{`${options} options - ${approved} approved`}
				</span>
			</>}
		</div>
	</div>;
}

export default Stock;