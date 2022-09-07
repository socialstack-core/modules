import ProductQuantity from 'UI/Payments/ProductQuantity';
import { useRef } from 'react';
import { useSession, useRouter } from 'UI/Session';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { useCart } from 'UI/Payments/CartSession';
import { renderProducts, renderTieredProducts, recurrenceText } from 'UI/Functions/Payments';
import getRef from 'UI/Functions/GetRef';
import FlipCard from 'UI/FlipCard';
import Loop from 'UI/Loop';
import Container from 'UI/Container';

const STRATEGY_STD = 0;
const STRATEGY_STEPONCE = 1;

export default function SelectSubscription(props) {
	const { session } = useSession();
	const { setPage } = useRouter();
	const productOptionsRef = useRef(null);
	var { cartIsEmpty, getCartQuantity } = useCart();
	
	function renderWithTiers(product) {
		var productName = product.name;
		var cheapestCost = formatCurrency(product.tiers[product.tiers.length - 1].price.amount, session.locale, { hideDecimals: false });
		
		var recurrence = recurrenceText(product.billingFrequency);
		
		var quantity = getCartQuantity(product.id);

		return <ProductQuantity 
			key={product.id}
			product={product}
			quantity={quantity}
			allowMultiple={props.allowMultiple}
			goStraightToCart={props.goStraightToCart}
			cartUrl={props.cartUrl}
			addDescription={props.addDescription}
			removeDescription={props.removeDescription}
			className={quantity > 0 ? "select-subscription__option selected-in-cart" : "select-subscription__option"}
		>
			{addButton => <FlipCard className="select-subscription__option-internal">
					<>
						<span className="select-subscription__option-image-wrapper">
							{getRef(product.featureRef, { size: 128, attribs: { className: 'select-subscription__option-image' } })}
							{!product.featureRef && <div className="select-subscription__option-image"></div>}
						</span>
						<span className="select-subscription__option-name">
							{productName}
						</span>
						<span className="select-subscription__option-price">
							{`As low as`} {cheapestCost} {recurrence}
						</span>
						{addButton}
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
						{addButton}
					</>
				</FlipCard>
		}
		</ProductQuantity>;
	}

	function renderTierInfo(product) {
		var tiersTotal = product.tiers.length;

		return [
			renderTierInfo("<=", " ", new Intl.NumberFormat(session.locale.code).format(product.tiers[0].minQuantity - 1), product.price.amount),
			product.tiers.map((tier, tierIndex) => {
				var fromSeats = new Intl.NumberFormat(session.locale.code).format(tier.minQuantity);
				var sep = tierIndex == tiersTotal - 1 ? " " : " \u2013 ";
				var toSeats = tierIndex == tiersTotal - 1 ? "+" : new Intl.NumberFormat(session.locale.code).format(product.tiers[tierIndex + 1].minQuantity - 1);

				return renderTierInfo(fromSeats, sep, toSeats, tier.price.amount);
			})
		];
	}

	function renderTierInfo(fromSeats, sep, toSeats, unitCost) {
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

		return <div className="select-subscription__wrapper">
				<ProductQuantity 
				className={quantity > 0 ? "select-subscription__option selected-in-cart" : "select-subscription__option"}
				key={product.id}
				product={product}
				quantity={quantity}
				allowMultiple={props.allowMultiple}
				goStraightToCart={props.goStraightToCart}
				cartUrl={props.cartUrl}
				addDescription={props.addDescription}
				removeDescription={props.removeDescription}
			>
			{addButton => <FlipCard className="select-subscription__option">
					<div className="select-subscription__option-image-wrapper">
						{getRef(product.featureRef, { size: 128, attribs: { className: 'select-subscription__option-image' } })}
						{!product.featureRef && <div className="select-subscription__option-image"></div>}
					</div>
					<p className="select-subscription__option-name">
						{productName}
					</p>
					<p className="select-subscription__option-price">
						{cost}
						{recurrence}
					</p>
					<center>
						{addButton}
					</center>
				</FlipCard>
			}
			</ProductQuantity>
		</div>;
    }

	return (
		<div className="subscribe-select-subscription">
			<h2 className="subscribe-select-subscription__title">
				{`New Subscription`}
			</h2>

			<div className="mb-3">
				<label className="form-label" id="select_product_label">
					{`Please select a product`}
				</label>
				<div className="product-options">
					<div className={'btn-group'}
						role="group" aria-labelledby={"select_product_label"} ref={productOptionsRef}>
						<Container>
							<Loop over="product" asCols size={props.colSize || 6} filter={{where: {billingFrequency: {not: 0}}}} includes={['tiers', 'tiers.price', 'price']}>
								{product => {
									
									// Is this a tiered product?
									var isTiered = product.tiers && product.tiers.length;
									
									if(isTiered){
										return renderWithTiers(product);
									}else{
										return renderNoTiers(product);
									}
									
								}}
							</Loop>
						</Container>
					</div>
				</div>
			</div>

			<div className="subscribe-select-subscription__footer">
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
