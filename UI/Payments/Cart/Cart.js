import Modal from 'UI/Modal';
import { useSession, useRouter } from 'UI/Session';
import { useCart } from '../CartSession';
import ProductTable from '../ProductTable';
import { useToast } from 'UI/Functions/Toast';
import { useState } from 'react';

export default function Cart(props) {
	const { session } = useSession();
	const { setPage } = useRouter();
	const { pop } = useToast();
	var { addToCart, emptyCart, shoppingCart, cartIsEmpty } = useCart();

	var [showEmptyCartPrompt, setShowEmptyCartPrompt] = useState(null);
	
	return <>
		<h2 className="shopping-cart__title">
			{`Shopping Cart`}
		</h2>

		<ProductTable shoppingCart={shoppingCart} addToCart={addToCart}/>

		{!cartIsEmpty() && <>
			<div className="shopping-cart__footer">
				<button type="button" className="btn btn-outline-danger" onClick={() => setShowEmptyCartPrompt(true)}>
					<i className="fal fa-fw fa-trash" />
					{`Empty Cart`}
				</button>

				<button type="button" className="btn btn-primary" onClick={() => setPage('/checkout')}>
					<i className="fal fa-fw fa-credit-card" />
					{`Checkout`}
				</button>
			</div>
		</>}
		{
			showEmptyCartPrompt && <>
				<Modal visible isSmall className="empty-cart-modal" title={`Empty Cart`} onClose={() => setShowEmptyCartPrompt(false)}>
					<p>{`This will remove all selected products from your shopping cart.`}</p>
					<p>{`Are you sure you wish to do this?`}</p>
					<footer>
						<button type="button" className="btn btn-outline-danger" onClick={() => setShowEmptyCartPrompt(false)}>
							{`Cancel`}
						</button>
						<button type="button" className="btn btn-danger" onClick={() => {
							emptyCart();
							setShowEmptyCartPrompt(false);
						}}>
							{`Empty`}
						</button>
					</footer>
				</Modal>
			</>
		}
	</>
}

Cart.propTypes = {
};

Cart.defaultProps = {
}

Cart.icon='shopping-cart';
