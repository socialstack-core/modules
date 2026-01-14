import { useCart } from '../CartSession';
import { useRouter } from 'UI/Router';
import Alert from 'UI/Alert';
import { useState } from 'react';


export default function ProductQuantity(props) {
	const { product, variant, goStraightToCart, cartUrl, allowMultiple, quantity, addText } = props;
	const [selectedQuantity, setSelectedQuantity] = useState(1);
	const [error, setError] = useState();
	const [loading, setLoading] = useState();
	const [typedQuantity, setTypedQuantity] = useState();
	const { setPage } = useRouter();
	var { addToCart } = useCart();
	var outOfStock = product.stock === 0;
	var variantClass = variant && variant.length ? 'btn-' + variant : '';
	
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

	var quants = [];
	for (var i = 1; i <= 10; i++) {
		quants.push(i);
	}

	var addProduct = () => {
		var qty = selectedQuantity == 'more' ? typedQuantity : parseInt(selectedQuantity);

		if (!qty) {
			setError({
				message: `Please enter a quantity`
			});
			return;
		}

		setError(null);
		setLoading(true);
		addToCart(product.id,
			qty,
			true
		).then(() => {
			if (goStraightToCart) {
				setPage(cartUrl || '/cart');
			}
		});
	};

	return <div className="product-quantity">
		<div>
		<select onChange={(e) => {
			var value = e.target.value;
			setSelectedQuantity(value);
		}}>
			{quants.map(quantity => <option value={quantity}>{quantity == 1 ? `Quantity: 1` : quantity}</option>)}
			<option value={'more'}>{`11+`}</option>
		</select>
		{
			selectedQuantity == 'more' && <input type='number' placeholder={`Please enter a number..`} step={1} onChange={(e) => setTypedQuantity(parseInt(e.target.value))} />
		}
		</div>
		{error && <Alert type='error'>{error.message}</Alert>}
		<div>
			<button
				className={'btn btn-primary'}
				disabled={loading}
				onClick={e => {
					e.stopPropagation();
					addProduct();
				}}
			>
				{addText || `Add to cart`}
			</button>
		</div>
	</div>;
}
