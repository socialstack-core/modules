import { Product } from 'Api/Product';
import Link from 'UI/Link';
import Button from 'UI/Button';
import Alert from 'UI/Alert';
import Quantity from 'UI/Product/Quantity';
import ProductPrice, { CurrencyAmount } from 'UI/Product/Price';
import ProductStock from 'UI/Product/Stock';
import { useCart } from 'UI/Payments/CartSession';
import ProductImage from 'UI/ProductImage';

/**
 * Props for the Signpost component.
 */
interface SignpostProps {
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
	 * The content to display in this signpost.
	 */
	content: Product,

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
	 * Used to display custom content inside the signpost.
	 */
	customContent?: React.ReactNode,

	/**
	 * view style
	 */
	viewStyle?: "list" | "small-thumbs" | "large-thumbs",

	/**
	 * Updates the cart if onChangeQuantity is not specified.
	 * @param newQty
	 * @returns
	 */
	onChangeQuantity?: (newQty: int) => void,

	/**
	 * Show components after "Add to basket" button.
	 */
	after?: (React.FC | React.FC<unknown>)[],
}

/**
 * The Signpost React component.
 * @param props React props.
 */
const Signpost: React.FC<SignpostProps> = (props) => {
	const { disableLink, content, hideQuantity, priceOverride, showRemove, readOnly,
		qtyOverride, onChangeQuantity, customContent } = props;
	const viewStyle = props.viewStyle || "list";
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

	// TODO: determine when product has options
	let hasOptions = false;

	function renderInner() {
		return <>
			<div className="ui-product-signpost__image">
				{/* optional featured product header */}
				{isFeatured && <>
					<span className="ui-product-signpost__featured">
						<i className="fr fr-star"></i>
						{featuredLabel}
					</span>
				</>}

				{/* product image
				  * reference 512px image as we have 3 views:
				  * list: ~150px
				  * small thumbnails: ~150px
				  * large thumbnails: ~322px
				  * 
				  * NB: while "no-image" is invalid, this will trigger a broken image;
				  * UI/Image spots this and replaces this with a placeholder image via CSS
				  * 
				  * LB edit: the invalid ref crashed the image loader - Image should just spot 
				  * an empty ref as that is the universal signal for "there isn't one"
				  */}
				<ProductImage size={512} fileRef={content.featureRef} />
			</div>

			{/* category */}
			{categoryName && categoryName.length > 0 && <>
				<span className="ui-product-signpost__category">
					<i className="fr fr-tag"></i>
					<span>
						{categoryName}
					</span>
				</span>
			</>}

			{/* product name */}
			<span className="ui-product-signpost__name">
				{content.name}
			</span>

			{content.productNotices?.length > 0 && <div className="ui-product-signpost__notices">
				{content.productNotices.map(notice => {
					return <Alert variant={notice.type}>{notice.message}</Alert>
				})}
			</div>}

			{/* price */}
			<ProductPrice product={content} override={priceOverride} />

			{
				customContent
			}

			{/* stock info */}
			<ProductStock product={content} />
		</>;
	}

	var classNames = ['ui-product-signpost'];
	classNames.push(`ui-product-signpost--${viewStyle}`);

	if (disableLink) {
		classNames.push('ui-product-signpost--disabled');
	}

	return (
		<div className={classNames.join(' ')}>
			<div className="ui-product-signpost__outer">
				{disableLink && <>
					<div className="ui-product-signpost__inner">
						{renderInner()}
					</div>
				</>}

				{!disableLink && <>
					<Link className="ui-product-signpost__inner" href={content.primaryUrl || `/product/${content.slug}`}>
						{renderInner()}
					</Link>
				</>}

				{/* quantity controls */}
				{!hideQuantity && <>
					<Quantity product={content} qtyOverride={qtyOverride} readOnly={readOnly} onChange={onChangeQuantity} />
				</>}
				{props?.after?.map((Component) => <Component product={content}/>)}
				{/* remove (used when viewed within basket) */}
				{showRemove && <>
					<Button xs outlined variant="danger" className="ui-product-signpost__remove" title={`Remove`} onClick={() => onChangeQuantity ? onChangeQuantity(0 as int) : addToCart(content.id, 0)}>
						<i className='fr fr-trash-alt' />
					</Button>
				</>}
			</div>
		</div>
	);
}

export default Signpost;