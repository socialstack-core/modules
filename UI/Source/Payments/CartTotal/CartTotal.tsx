import { formatCurrency } from "UI/Functions/CurrencyTools";
import Button from 'UI/Button';
import ConfirmDialog from 'UI/Dialog/ConfirmDialog';
import { useState } from 'react';
import { useRouter } from 'UI/Router';
import { useSession } from 'UI/Session';
import { ShoppingCart } from 'Api/ShoppingCart';
import getConfig from 'UI/Config';
import Alert from "UI/Alert";
import { useCart } from "UI/Payments/CartSession";

/**
 * Props for the CartTotal component.
 */
interface CartTotalProps {
	/**
	 * basket contents
	 */
	shoppingCart: ShoppingCart,

	/** 
	 * method to run when clicking "empty cart"
	 */
	emptyCart?: Function,

	/**
	 * set true to hide call to action buttons (used on checkout view)
	 */
	hideCTAs?: boolean
}

type GuestUsersConfig = {
	isEnabled?: boolean
};

/**
 * The CartTotal React component.
 * @param props React props.
 */
const CartTotal: React.FC<CartTotalProps> = (props) => {
	var { shoppingCart, emptyCart, hideCTAs } = props;
	var [showEmptyCartPrompt, setShowEmptyCartPrompt] = useState<boolean>(false);

	var pricedCart = shoppingCart?.cartContents;

	const { setPage } = useRouter();

	const { session } = useSession();
	var { user } = session;

	// if we have the guest user module, ensure that is it enabled?
	var cfg = getConfig<GuestUsersConfig>("GuestUsers");
	var guestCheckoutEnabled = cfg?.[0]?.isEnabled ?? false;

	if (user) {
		guestCheckoutEnabled = false;
	}

	const { lessTax } = useCart();

	if (!pricedCart || !pricedCart.contents.length) {
		return null;
	}

	//var itemSet = pricedCart.contents;
	var currencyCode = pricedCart.currencyCode;
	var hasAtLeastOneSubscription = pricedCart.hasSubscriptionProducts;

	let totalIncVat = pricedCart.total;
	let totalExcVat = pricedCart.totalLessTax;
	let totalVat = totalIncVat - totalExcVat;

	let discounts = [];
	var cartContents = shoppingCart?.cartContents;

	if (shoppingCart?.coupon && cartContents) {

		if (shoppingCart.coupon.discountFixedAmount > 0) {
			discounts.push(formatCurrency(shoppingCart.coupon.discountFixedAmount, { currencyCode: cartContents.currencyCode }));
		}

		if (shoppingCart.coupon.discountPercent > 0) {
			discounts.push(new Intl.NumberFormat('default', {
				style: 'percent',
				minimumFractionDigits: 0,
				maximumFractionDigits: 0,
			}).format(shoppingCart.coupon.discountPercent / 100));
		}

	}

	return <>
		<footer className="shopping-cart__total">

			{/*
				- Legal liability notice -
				Be careful! Deliveries are not VAT free.
				*Do not* change the order of these values.
			*/}
			<p className="shopping-cart__total-row">
				<span>{lessTax ? `Total (ex VAT)` : `Total (inc VAT)`}</span>
				{formatCurrency((lessTax ? shoppingCart.cartContents?.totalLessTax : shoppingCart.cartContents?.total) || 0 as int, { currencyCode })}
			</p>

			{discounts.length > 0 && <>
				<p className="shopping-cart__total-row">
					<span>{discounts.length == 1 ? `Includes discount` : `Includes discounts`}</span>
					-{discounts.join(', -')}
				</p>
			</>}

			{lessTax && (
				<p className="shopping-cart__total-row">
					<span>{`VAT`}</span>
					{formatCurrency((shoppingCart.cartContents?.total ?? 0) - (shoppingCart.cartContents?.totalLessTax ?? 0), { currencyCode })}
				</p>
			)}

			<p className="shopping-cart__total-row shopping-cart__total-row--grand">
				<span>{`Total`}</span>
				{formatCurrency(shoppingCart.cartContents?.total || 0, { currencyCode })}
			</p>

			{!lessTax && (
				<p className="shopping-cart__total-row">
					<span>{`VAT`}</span>
					{formatCurrency((shoppingCart.cartContents?.total ?? 0) - (shoppingCart.cartContents?.totalLessTax ?? 0), { currencyCode })}
				</p>
			)}

			{Boolean(shoppingCart?.cartContents?.errorCode) ? <Alert variant={'danger'}>{shoppingCart.cartContents?.errorMessage}</Alert> : null}

			{!hideCTAs && <>
				<div className="shopping-cart__total-cta">
					<Button variant="danger" outlined onClick={() => setShowEmptyCartPrompt(true)}>
						<i className="fr fr-trash-alt" />
						{`Empty Basket`}
					</Button>

					<Button variant="primary" onClick={() => {
						setPage('/cart/checkout');
					}}>
						<i className="fr fr-credit-card" />
						{`Checkout`}
					</Button>

					{guestCheckoutEnabled &&
						<Button variant="primary" onClick={() => {

							setPage('/guest/register');

						}}>
							<i className="fr fr-credit-card" />
							{`Checkout as Guest`}
						</Button>
					}

				</div>
			</>}

			{/* TODO: reinstate subscription footnote?
				{hasAtLeastOneSubscription && <small>
					<span className="footnote-asterisk"></span> {`Your payment information will be securely stored in order to process future subscription payments. The total stated will also be charged today.`}
				</small>}
			*/}

		</footer>

		{showEmptyCartPrompt && <>
			<ConfirmDialog variant="danger" isOpen={showEmptyCartPrompt} title={`Empty Basket`} onClose={() => setShowEmptyCartPrompt(false)}
				confirmText={`Empty`}
				confirmCallback={() => {
					emptyCart!();
					setShowEmptyCartPrompt(false);
				}}>
				<p>{`This will remove all selected products from your shopping basket.`}</p>
				<p>{`Are you sure you wish to do this?`}</p>
			</ConfirmDialog>
		</>}

	</>;
}

export default CartTotal;