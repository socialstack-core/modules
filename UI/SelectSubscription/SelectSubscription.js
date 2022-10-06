import ProductQuantity from 'UI/Payments/ProductQuantity';
import { useRef } from 'react';
import { useSession, useRouter } from 'UI/Session';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { useCart } from 'UI/Payments/CartSession';
import { renderProducts, renderTieredProducts, recurrenceText } from 'UI/Functions/Payments';
import getRef from 'UI/Functions/GetRef';
import FlipCard from 'UI/FlipCard';
import Loop from 'UI/Loop';

export default function SelectSubscription(props) {
	const { session } = useSession();
	const { setPage } = useRouter();
	const productOptionsRef = useRef(null);
	var { cartIsEmpty, getCartQuantity } = useCart();
	
	function renderWithTiers(product) {
		var productName = product.name;
		var cheapestCost = product.price.amount;

		for (var i = 0; i < product.tiers.length; i++) {
			var tierPrice = product.tiers[i].price.amount;

			if (tierPrice < cheapestCost) {
				cheapestCost = tierPrice;
            }

        }

		cheapestCost = formatCurrency(cheapestCost, session.locale, { hideDecimals: false });
		var recurrence = recurrenceText(product.billingFrequency);
		var quantity = getCartQuantity(product.id);
		const productQtyRef = useRef(null);

		return <>
			<div className={quantity > 0 ? "select-subscription__option select-subscription__option--selected" : "select-subscription__option"} key={product.id}>
				{/* TODO: allow for linked products to give access to further product details */}
				{/*<a href={"/product/" + product.id} className="select-subscription__option-link">*/}
				<button type="button" className="select-subscription__option-link" onClick={e => {
					e.stopPropagation();

					if (productQtyRef && productQtyRef.current) {
						productQtyRef.current.base.click();
                    }
				}}>
					<FlipCard className="select-subscription__option-internal">
						<>
							<span className="select-subscription__option-image-wrapper">
								{getRef(product.featureRef, { size: 128, attribs: { className: 'select-subscription__option-image' } })}
								{!product.featureRef && <div className="select-subscription__option-image"></div>}
							</span>
							<span className="select-subscription__option-name">
								{productName}
							</span>
							<span className="select-subscription__option-price">
								{`From`} {cheapestCost} {recurrence}
							</span>
						</>
						<>
							<span className="select-subscription__option-name">
								{productName}
							</span>
							<table className="table table-sm select-subscription__option-table">
								<thead>
									<tr>
										<th>
											{`Daily seats`}
										</th>
										<th className="currency-column">
											{`Unit cost`}
										</th>
									</tr>
								</thead>
								<tbody>
									{renderTierInfo(product)}
								</tbody>
							</table>
						</>
					</FlipCard>
				</button>
				{/*</a>*/}
				<ProductQuantity
					ref={productQtyRef}
					key={product.id}
					product={product}
					quantity={quantity}
					allowMultiple={props.allowMultiple}
					goStraightToCart={props.goStraightToCart}
					cartUrl={props.cartUrl}
					addDescription={props.addDescription}
					removeDescription={props.removeDescription}
				/>
			</div>
		</>;
	}

	function renderTierInfo(product) {
		var tiersTotal = product.tiers.length;
		var minQuantity = product.tiers[0].minQuantity - 1;

		if (product.minQuantity > minQuantity) {
			minQuantity = product.minQuantity;
        }

		return [
			renderTierInfoInternal("<=", " ", new Intl.NumberFormat(session.locale.code).format(minQuantity), product.price.amount),
			product.tiers.map((tier, tierIndex) => {
				var tierMinQuantity = tier.minQuantity;

				if (minQuantity > tierMinQuantity) {
					tierMinQuantity = minQuantity + 1;
                }

				var fromSeats = new Intl.NumberFormat(session.locale.code).format(tierMinQuantity);
				var sep = tierIndex == tiersTotal - 1 ? " " : " \u2013 ";
				var toSeats = tierIndex == tiersTotal - 1 ? "+" : new Intl.NumberFormat(session.locale.code).format(product.tiers[tierIndex + 1].minQuantity - 1);

				return renderTierInfoInternal(fromSeats, sep, toSeats, tier.price.amount);
			})
		];
	}

	function renderTierInfoInternal(fromSeats, sep, toSeats, unitCost) {
		return <tr>
			<td>
				{fromSeats}{sep}{toSeats}
			</td>
			<td className="currency-column">
				{formatCurrency(unitCost, session.locale)}
				{/* previous version included both unit and flat cost - still necessary to support this? */}
			</td>
		</tr>;
	}
	
	function renderNoTiers(product) {
		var productName = product.name;
		var cost = formatCurrency(product.price.amount * product.minQuantity, session.locale, { hideDecimals: false });
		var recurrence = recurrenceText(product.billingFrequency);
		var quantity = getCartQuantity(product.id);
		const productQtyRef = useRef(null);

		return <>
			<div className={quantity > 0 ? "select-subscription__option select-subscription__option--selected" : "select-subscription__option"} key={product.id}>
				{/* TODO: allow for linked products to give access to further product details */}
				{/*<a href={"/product/" + product.id} className="select-subscription__option-link">*/}
				<button type="button" className="select-subscription__option-link" onClick={e => {
					e.stopPropagation();

					if (productQtyRef && productQtyRef.current) {
						productQtyRef.current.base.click();
					}
				}}>
					<FlipCard className="select-subscription__option-internal">
						<span className="select-subscription__option-image-wrapper">
							{getRef(product.featureRef, { size: 128, attribs: { className: 'select-subscription__option-image' } })}
							{!product.featureRef && <div className="select-subscription__option-image"></div>}
						</span>
						<span className="select-subscription__option-name">
							{productName}
						</span>
						<span className="select-subscription__option-price">
							{cost}
							{recurrence}
						</span>
						</FlipCard>
				</button>
				{/*</a>*/}
				<ProductQuantity
					ref={productQtyRef}
					key={product.id}
					product={product}
					quantity={quantity}
					allowMultiple={props.allowMultiple}
					goStraightToCart={props.goStraightToCart}
					cartUrl={props.cartUrl}
					addDescription={props.addDescription}
					removeDescription={props.removeDescription}
				/>
			</div>
		</>;
    }

	return (
		<div className="select-subscription">
			<h2 className="select-subscription__title">
				{`New Subscription`}
			</h2>

			<div className="mb-3">
				<label className="form-label" id="select_product_label">
					{`Please select a product`}
				</label>
				<div className="product-options">
					<div className={'btn-group'} role="group" aria-labelledby={"select_product_label"} ref={productOptionsRef}>
						<Loop over="product" raw filter={{where: {billingFrequency: {not: 0}}}} includes={['tiers', 'tiers.price', 'price']}>
							{product => {
									
								// Is this a tiered product?
								var isTiered = product.tiers && product.tiers.length;
									
								if (isTiered) {
									return renderWithTiers(product);
								}else{
									return renderNoTiers(product);
								}
									
							}}
						</Loop>
					</div>
				</div>
			</div>

			<div className="select-subscription__footer">
				{/* rendered as a button rather than a direct link so that we can disable the link if needed */}
				{(!props.goStraightToCart || !cartIsEmpty()) && <button type="button" className="btn btn-secondary" onClick={() => setPage(props.cartUrl || '/cart')}>
					<i className="fal fa-fw fa-shopping-cart" />
					{`View Cart`}
				</button>}
			</div>
		</div>
	);
}

SelectSubscription.propTypes = {
	standaloneOnly: 'boolean',
	tieredOnly: 'boolean',
	allowMultiple: 'boolean',
	cartUrl: 'string',
	goStraightToCart: 'boolean',
	addDescription: 'string',
	removeDescription: 'string'
};

SelectSubscription.defaultProps = {
	standaloneOnly: false,
	tieredOnly: false,
	allowMultiple: false,
	cartUrl: '/cart',
	goStraightToCart: true,
	addDescription: `Select this`,
	removeDescription: `Remove this`
}

SelectSubscription.icon = 'align-center';
