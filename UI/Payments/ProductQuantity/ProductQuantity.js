import { useCart } from '../CartSession';
import { useToast } from 'UI/Functions/Toast';

export default function ProductQuantity(props) {
	const { product, variant, addDescription, removeDescription } = props;
	const { pop } = useToast();
	var { addToCart, getCartQuantity } = useCart();
	var isSubscription = product.isBilledByUsage;
	var outOfStock = product.stock === 0;
	var variantClass = variant && variant.length ? 'btn-' + variant : '';

	var quantity = getCartQuantity(product.id);

	if (isSubscription && quantity > 1) {
		quantity = 1;
    }

	function updateQuantity(qty, isSubscription) {
		addToCart({
			product: product.id,
			quantity: qty,
			isSubscribing: isSubscription
		});

		var absQty = Math.abs(qty);
		var qtyDesc = absQty == 1 ? '' : absQty + 'x ';
		var activity = (qty < 0) ? `${qtyDesc}${product.name} removed from cart` : `${qtyDesc}${product.name} added to cart`;
		var title;

		if (absQty == 1) {
			title = qty < 0 ? `Product removed` : `Product added`;
		} else {
			title = qty < 0 ? `Products removed` : `Products added`;
        }

		pop({
			title: title,
			description: activity,
			duration: 4,
			variant: 'success'
		});
	}

	if (outOfStock) {
		return <div className="product-quantity">
			<button type="button" className={'btn ' + variantClass + ' product-quantity__toggle'} disabled>
				{!isSubscription && `Out of stock`}
				{isSubscription && `Unavailable`}
			</button>
		</div>;
    }

	return (
		<div className="product-quantity">
			{(!quantity || (quantity && isSubscription)) && <button type="button" className={'btn ' + variantClass + ' product-quantity__toggle'}
				onClick={() => updateQuantity(quantity == 0 ? 1 : -1, isSubscription)}>
				{quantity == 0 && addDescription}
				{quantity > 0 && isSubscription && removeDescription}
			</button>}
			{quantity > 0 && !isSubscription && <>
				<button type="button" className={'btn ' + variantClass + ' product-quantity__remove'} onClick={() => updateQuantity(-1, isSubscription)}>
					<span>-</span>
				</button>
				<span className="product-quantity__value">
					{quantity}
				</span>
				<button type="button" className={'btn ' + variantClass + ' product-quantity__add'} onClick={() => updateQuantity(1, isSubscription)}>
					<span>+</span>
				</button>
			</>}
		</div>
	);
}

ProductQuantity.propTypes = {
	variant: 'string',
	addDescription: 'string',
	removeDescription: 'string'
};

ProductQuantity.defaultProps = {
	variant: 'primary',
	addDescription: `Add to cart`,
	removeDescription: `Remove from cart`
}

ProductQuantity.icon = 'align-center';
