import { useCart } from '../CartSession';
import { useToast } from 'UI/Functions/Toast';
import { useRouter } from 'UI/Session';


export default function ProductQuantity(props) {
	const { product, variant, addDescription, removeDescription, goStraightToCart, cartUrl, allowMultiple, quantity } = props;
	const { pop } = useToast();
	const { setPage } = useRouter();
	var { addToCart } = useCart();
	var outOfStock = product.stock === 0;
	var variantClass = variant && variant.length ? 'btn-' + variant : '';
	
	function updateQuantity(qty) {
		
		addToCart({
			product: product.id,
			quantity: qty,
			allowMultiple
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
		
		if(qty > 0 && goStraightToCart && !allowMultiple){
			setPage(cartUrl ? cartUrl : '/cart');
		}
	}

	if (outOfStock) {
		var isSubscription = product.billingFrequency!=0 || product.isBilledByUsage;
		
		return <div className="product-quantity">
			{props.children}
			<button type="button" className={'btn ' + variantClass + ' product-quantity__toggle'} disabled>
				{!isSubscription && `Out of stock`}
				{isSubscription && `Unavailable`}
			</button>
		</div>;
    }
	
	var addButton = <div className="product-quantity">
		{(!quantity || (quantity && !allowMultiple)) && <button type="button" className={'btn ' + variantClass + ' product-quantity__toggle'}
			onClick={e => {
				e.stopPropagation();
				updateQuantity(quantity == 0 ? 1 : -1);
			}}>
			{quantity == 0 && addDescription}
			{quantity > 0 && !allowMultiple && removeDescription}
		</button>}
		{quantity > 0 && allowMultiple && <>
			<button type="button" className={'btn ' + variantClass + ' product-quantity__remove'} onClick={e => {
				e.stopPropagation();
				updateQuantity(-1);
			}}>
				<span>-</span>
			</button>
			<span className="product-quantity__value">
				{quantity}
			</span>
			<button type="button" className={'btn ' + variantClass + ' product-quantity__add'} onClick={e => {
				e.stopPropagation();
				updateQuantity(1);
			}}>
				<span>+</span>
			</button>
		</>}
	</div>;
	
	var buttonClass = 'product-quantity-button';
	
	if(props.className){
		buttonClass += ' ' + props.className;
	}
	
	return <button 
		className={buttonClass}
		onClick={e => {
			e.stopPropagation();
			updateQuantity(quantity == 0 ? 1 : -1);
		}}
	>
		{props.children(addButton)}
	</button>;
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
