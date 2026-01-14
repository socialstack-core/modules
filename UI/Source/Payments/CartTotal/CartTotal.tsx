import { formatCurrency } from "UI/Functions/CurrencyTools";
import Button from 'UI/Button';
import Modal from 'UI/Modal';
import { useState } from 'react';
import { useRouter } from 'UI/Router';
import { useSession } from 'UI/Session';
import getConfig from 'UI/Config';

/**
 * Props for the CartTotal component.
 */
interface CartTotal {
}

/**
 * The CartTotal React component.
 * @param props React props.
 */
const CartTotal: React.FC<CartTotalProps> = (props) => {
	var { shoppingCart, emptyCart, lessTax } = props;
	var [showEmptyCartPrompt, setShowEmptyCartPrompt] = useState(null);

	var pricedCart = shoppingCart?.cartContents;

	if (!pricedCart || !pricedCart.contents.length) {
		return;
	}

	const { setPage } = useRouter();

	const { session } = useSession();
	var { user } = session;

	// if we have the guest user module, ensure that is it enabled?
	var cfg = getConfig<GuestUsersConfig>("GuestUsers");
	var guestCheckoutEnabled = cfg?.[0]?.isEnabled ?? false;

	if (user) {
		guestCheckoutEnabled = false;
	}

	//var itemSet = pricedCart.contents;
	var currencyCode = pricedCart.currencyCode;
	var hasAtLeastOneSubscription = pricedCart.hasSubscriptionProducts;

	let totalIncVat = pricedCart.total;
	let totalExcVat = pricedCart.totalLessTax;
	let totalVat = totalIncVat - totalExcVat;

	return <>
		<footer className="shopping-cart__total">
			<dl className="shopping-cart__total-rows">
				<div class="shopping-cart__total-row shopping-cart__total-row--exc-vat">
					<dt>
						{`Total exc VAT`}
					</dt>
					<dd>
						{formatCurrency(totalExcVat, { currencyCode })}
					</dd>
				</div>

				<div class="shopping-cart__total-row shopping-cart__total-row--vat">
					<dt>
						{`VAT`}
					</dt>
					<dd>
						{formatCurrency(totalVat, { currencyCode })}
					</dd>
				</div>

				<div class="shopping-cart__total-row shopping-cart__total-row--inc-vat">
					<dt>
						{`Total inc VAT`}
					</dt>
					<dd>
						{formatCurrency(totalIncVat, { currencyCode })}
					</dd>
				</div>
			</dl>

			<div className="shopping-cart__total-cta">
				<Button variant="danger" outlined onClick={() => setShowEmptyCartPrompt(true)}>
					<i className="fr fr-trash-alt" />
					{`Empty basket`}
				</Button>

				<Button variant="primary" onClick={() => setPage('/cart/checkout')}>
					<i className="fr fr-credit-card" />
					{`Checkout`}
				</Button>

				{guestCheckoutEnabled && 
					<Button variant="primary" onClick={() => setPage('/guest/register')}>
						<i className="fr fr-credit-card" />
						{`Checkout as Guest`}
					</Button>					
				}

			</div>

			{/* TODO: reinstate subscription footnote?
				{hasAtLeastOneSubscription && <small>
					<span className="footnote-asterisk"></span> {`Your payment information will be securely stored in order to process future subscription payments. The total stated will also be charged today.`}
				</small>}
			*/}

		</footer>
		{
			showEmptyCartPrompt && <>
				<Modal visible isSmall className="empty-cart-modal" title={`Empty basket`} onClose={() => setShowEmptyCartPrompt(false)}>
					<p>{`This will remove all selected products from your shopping basket.`}</p>
					<p>{`Are you sure you wish to do this?`}</p>
					<footer>
						<Button variant="danger" outlined onClick={() => setShowEmptyCartPrompt(false)}>
							{`Cancel`}
						</Button>
						<Button variant="danger" onClick={() => {
							emptyCart();
							setShowEmptyCartPrompt(false);
						}}>
							{`Empty`}
						</Button>
					</footer>
				</Modal>
			</>
		}
	</>;
}

export default CartTotal;