import { useCart } from 'UI/Payments/CartSession';
import { useState } from 'react';
import { Coupon } from 'Api/Coupon';
import Input from 'UI/Input';
import Form from 'UI/Form';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { ShoppingCart } from 'Api/ShoppingCart';

/**
 * Props for the PromoCode component.
 */
interface PromoCodeProps {
	/**
	 * basket contents
	 */
	shoppingCart: ShoppingCart,
}

/**
 * The PromoCode React component.
 * @param props React props.
 */
const PromoCode: React.FC<PromoCodeProps> = (props) => {
	var { shoppingCart } = props;
	var { cartIsEmpty, setCoupon } = useCart();
	var [applyCodeEnabled, setApplyCodeEnabled] = useState(false);

	const renderCouponDetails = (coupon: Coupon) => {
		var pricedCart = shoppingCart?.cartContents;

		if (!coupon || !pricedCart) {
			return null;
		}

		const minSpend = coupon.minimumSpendAmount ? `Spend ${formatCurrency(coupon.minimumSpendAmount, { currencyCode: pricedCart.currencyCode })}, get ` : '';

		return <>
			<ul className="shopping-cart__coupon">
				{/* fixed discount amount */}
				{coupon.discountFixedAmount > 0 && <>
					<li>
						{`${minSpend}${formatCurrency(coupon.discountFixedAmount, { currencyCode: pricedCart.currencyCode })} off`}
					</li>
				</>}

				{/* discount percentage */}
				{coupon.discountPercent > 0 && <>
					<li>
						{`${minSpend}${new Intl.NumberFormat('default', {
							style: 'percent',
							minimumFractionDigits: 0,
							maximumFractionDigits: 0,
						}).format(coupon.discountPercent / 100)} off`}
					</li>
				</>}

				{/* free delivery */}
				{(coupon.minimumSpendAmount > 0 && coupon.freeDelivery) && <>
					<li>
						{`${minSpend}free delivery`}
					</li>
				</>}

			</ul>
		</>;
	};

	const updateApplyCodeEnabled = (e: React.ChangeEvent<Element>) => {
		setApplyCodeEnabled(!!(e.target as HTMLInputElement).value.trim().length);
	}

	if (!cartIsEmpty || cartIsEmpty()) {
		return null;
	}

	type CouponFields = {
		coupon: string
	};

	return <>
		<fieldset className="shopping-cart__promo fieldset--bordered">
			<legend>
				{shoppingCart?.coupon ? `Active coupon` : `Have a promotional code? Enter it here`}
			</legend>
			<Form className="shopping-cart__promo-form" action={(fields : CouponFields) => setCoupon!(fields.coupon)}
				successMessage={`Coupon applied`} failedMessage={`Unable to apply coupon`}>
				<Input type='text' name='coupon' placeholder={`Enter promotional code`} noWrapper readOnly={shoppingCart?.coupon ? true : undefined}
					onChange={(e:React.ChangeEvent<Element>) => updateApplyCodeEnabled(e)} value={shoppingCart?.coupon?.token || undefined} />
				{!shoppingCart?.coupon && <>
					<Input type="submit" label={`Apply Code`} noWrapper disabled={applyCodeEnabled ? undefined : true} />
				</>}
				{shoppingCart?.coupon && <>
					<Input type="reset" variant="danger" label={`Remove Code`} noWrapper onClick={() => setCoupon!(null)} />
				</>}
			</Form>
			{renderCouponDetails(shoppingCart?.coupon)}
		</fieldset>
	</>;
}

export default PromoCode;