import { Product } from 'Api/Product';
import Quantity from 'UI/Product/Quantity';
import ProductImage from 'UI/ProductImage';
import Link from 'UI/Link';
import ProductPrice, { CurrencyAmount } from 'UI/Product/Price';
import ProductStock from 'UI/Product/Stock';
import Alert, { AlertType } from 'UI/Alert';
import Button from 'UI/Button';
import { useCart } from 'UI/Payments/CartSession';

/**
 * Props for the BasketItem component.
 */
interface BasketItemProps {
	/**
	 * The content to display.
	 */
	content: Product,

	/**
	 * set true to hide quantity controls
	 */
	hideQuantity?: boolean,

	/**
	 * set true to disable link to product info
	 */
	disableLink?: boolean,

	/**
	 * Line item price (in basket)
	 */
	priceOverride?: CurrencyAmount,

	/**
	 * label for featured products
	 */
	featuredLabel?: string,

	/**
	 * allow removal from basket
	 */
	showRemove?: boolean

	/**
	 * set true to render readonly
	 */
	readOnly?: boolean,

	/**
	 * optionally override quantity displayed
	 */
	qtyOverride?: Number,

	/**
	 * Used to display custom content
	 */
	customContent?: React.ReactNode,

	/**
	 * Updates the cart if onChangeQuantity is not specified.
	 * @param newQty
	 * @returns
	 */
	onChangeQuantity?: (newQty: int) => void
}

/**
 * The BasketItem React component.
 * @param props React props.
 */
const BasketItem: React.FC<BasketItemProps> = (props) => {
	const { content, hideQuantity, disableLink, priceOverride, showRemove, readOnly,
		qtyOverride, onChangeQuantity, customContent } = props;

	var { addToCart } = useCart();

	if (!content) {
		return;
	}

	let isFeatured = content.isFeatured;
	let featuredLabel = props.featuredLabel || `Recommended`;

	// retrieve associated category name
	let categoryName: string | null = null;

	if (content.primaryCategory) {
		categoryName = content.primaryCategory.name;
	} else if (content.productCategories?.length) {
		categoryName = content.productCategories[0].name;
	}

	var classNames = ['ui-product-basket-item'];

	if (disableLink) {
		classNames.push('ui-product-basket-item--disabled');
	}

	return (
		<div className={classNames.join(' ')}>
			<div className="ui-product-basket-item__internal">

				{/* product image 
				  * reference 512px image as the image is shown @ 100% width when the shopping cart is viewed on mobile;
				  * this is sized down via CSS to a more appropriate size for the main cart view / sidebar
				  */}
				{content.featureRef && <ProductImage size={512} fileRef={content.featureRef} className="ui-product-basket-item__img" />}

				{/* category */}
				{categoryName && categoryName.length > 0 && <>
					<span className="ui-product-basket-item__category">
						<i className="fr fr-tag"></i>
						<span>
							{categoryName}
						</span>
					</span>
				</>}

				{/* product name */}
				{!disableLink && <>
					<Link href={content.primaryUrl || `/product/${content.slug}`} className="ui-product-basket-item__link">
						<span className="ui-product-basket-item__name">
							{content.name}
						</span>
					</Link>
				</>}
				{disableLink && <>
					<span className="ui-product-basket-item__name">
						{content.name}
					</span>
				</>}

				{/* price */}
				<ProductPrice product={content} override={priceOverride} />

				{/* stock info */}
				<ProductStock product={content} />

				{/* custom content */}
				{!!customContent && <>
					<span className="ui-product-basket-item__custom">
						{customContent}
					</span>
				</>}

				{!hideQuantity && <>
					<Quantity product={content} qtyOverride={qtyOverride} readOnly={readOnly} onChange={onChangeQuantity} />
				</>}

				{showRemove && <>
					<Button xs outlined variant="danger" className="ui-product-basket-item__remove"
						title={`Remove`} onClick={() => onChangeQuantity ? onChangeQuantity(0 as int) : addToCart!(content.id, 0 as int, false)}>
						<i className='fr fr-trash-alt' />
					</Button>
				</>}
			</div>

			{/* product notices */}
			{!!content.productNotices && <div className="ui-product-basket-item__notices">
				{content.productNotices.map(notice => (
					<Alert variant={notice.type as AlertType}>{notice.message}</Alert>
				))}
			</div>}
		</div>
	);
}

export default BasketItem;