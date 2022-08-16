import Wrapper from '../Wrapper';
import { getSteps, getStepIndex } from '../Steps.js';
import { useState, useRef } from 'react';
import { useSession, useRouter } from 'UI/Session';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { useToast } from 'UI/Functions/Toast';
import { useCart } from '../CartSession';
import { renderTieredProducts } from 'UI/Functions/Payments';

const STRATEGY_PAYG = 0;
const STRATEGY_BULK = 1;

export default function SelectProduct(props) {
	const { allowMultiple } = props;
	const { session } = useSession();
	const { setPage } = useRouter();
	const { pop } = useToast();
	const productOptionsRef = useRef(null);
	var { addToCart, getCartQuantity, cartIsEmpty } = useCart();

	var [productSelection, setProductSelection] = useState(null);

	function clearSelections() {

		if (!productOptionsRef || !productOptionsRef.current) {
			return;
		}

		[...productOptionsRef.current.getElementsByTagName("input")].forEach(input => {
			input.checked = false;
        });

    }

	function updateCart() {

		if (!productSelection || !productSelection.length) {
			return;
		}

		productSelection.forEach(product => {
			addToCart({
				product: product.id,
				isSubscribing: true
			});

			pop({
				title: `Product added`,
				description: `${product.name} added to cart`,
				duration: 4,
				variant: 'success'
			});

		});

		clearSelections();
    }

	function validateProductSelection(productOptionsWrapper) {
		var productOptions = productOptionsWrapper.getElementsByClassName("btn-check");
		var selected = [];

		[...productOptions].forEach(option => {

			if (option.checked) {
				var id = parseInt(option.dataset.id, 10);
				var name = option.dataset.name;

				if (id) {
					selected.push({
						id: id,
						name: name
					});
                }
			}

		});

		setProductSelection(selected);
	}

	function updateProductSelection(e) {
		var selectedElement = e.currentTarget;
		validateProductSelection(selectedElement.parentElement);
	}

	function addTiers(product, allowMultiple) {

		switch (product.priceStrategy) {
			case STRATEGY_PAYG:
				var productId = "product_" + product.id;
				var productName = product.name;
				var labelClass = "btn btn-outline-secondary product-option";

				if (getCartQuantity(product.id)) {
					labelClass += " product--selected";
                }

				return <>
					<input className="btn-check" id={productId} autocomplete="off"
						type={allowMultiple ? "checkbox" : "radio"} 
						name={allowMultiple ? undefined : "productOption"}
						data-id={product.id}
						data-name={productName}
						onChange={(e) => updateProductSelection(e)} />
					<label className={labelClass} htmlFor={productId}>
						<span className="product-option__name">
							{productName}
						</span>
						<table className="table table-sm product-option__table">
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
								{addPaygTiers(product)}
							</tbody>
						</table>
					</label>
				</>;

			case STRATEGY_BULK:
				return addBulkTiers(product, allowMultiple);
		}

	}

	function addPaygTiers(product) {
		var tiersTotal = product.tiers.length;

		return [
			addPaygTier("<=", " ", new Intl.NumberFormat(session.locale.code).format(product.tiers[0].minQuantity - 1), product.price.amount),
			product.tiers.map((tier, tierIndex) => {
				var fromSeats = new Intl.NumberFormat(session.locale.code).format(tier.minQuantity);
				var sep = tierIndex == tiersTotal - 1 ? " " : " \u2013 ";
				var toSeats = tierIndex == tiersTotal - 1 ? "+" : new Intl.NumberFormat(session.locale.code).format(product.tiers[tierIndex + 1].minQuantity - 1);

				return addPaygTier(fromSeats, sep, toSeats, tier.price.amount);
			})
		];
	}

	function addPaygTier(fromSeats, sep, toSeats, unitCost) {
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

	function addBulkTiers(product, allowMultiple) {
		return [
			addProduct(product, allowMultiple),
			product.tiers.map((tier) => {
				return addProduct(tier, allowMultiple);
			})
		];
	}

	function addProduct(product, allowMultiple) {
		var productId = "product_" + product.id;
		var productName = product.name;
		// TODO: check price strategy
		var cost = formatCurrency(product.price.amount * product.minQuantity, session.locale, { hideDecimals: true });
		// TODO: support differing frequencies
		var recurrence = ` pm`;

		return <>
			<input className="btn-check" id={productId} autocomplete="off"
				type={allowMultiple ? "checkbox" : "radio"}
				name={allowMultiple ? undefined : "productOption"}
				data-id={product.id}
				data-name={productName}
				onChange={(e) => updateProductSelection(e)} />
			<label className="btn btn-outline-secondary product-option" htmlFor={productId}>
				<span className="product-option__name">
					{productName}
				</span>
				<span className="product-option__price">
					{cost}
					{recurrence}
				</span>
			</label>
		</>;

    }

	return (
		<Wrapper className="subscribe-select-product" activeStep={getStepIndex(getSteps().SUBSCRIPTION_TYPE)}>
			<h2 className="subscribe-select-product__title">
				{`New Subscription`}
			</h2>

			<div class="mb-3">
				<label className="form-label" id="select_product_label">
					{allowMultiple ? `Please select required products` : `Please select a product`}
					<span class="is-required-field"></span>
				</label>
				<div className="product-options">
					<div className={productSelection && !productSelection.length ? 'btn-group form-invalid' : 'btn-group'}
						role="group" aria-labelledby={"select_product_label"} ref={productOptionsRef}>
						{renderTieredProducts(addTiers)}
					</div>
				</div>
				{productSelection && !productSelection.length && <>
					<div className="validation-error">
						{allowMultiple ? `Please select required products` : `Please select a product`}
					</div>
				</>}
			</div>

			<div className="subscribe-select-product__footer">
				<button type="button" className="btn btn-outline-primary" onClick={() => updateCart()}
					disabled={!productSelection || !productSelection.length ? "disabled" : undefined}>
					<i className="fal fa-fw fa-plus" />
					{`Add to Cart`}
				</button>

				{/* rendered as a button rather than a direct link so that we can disable the link as needed */}
				<button type="button" className="btn btn-primary" onClick={() => setPage('/cart')}
					disabled={cartIsEmpty() ? "disabled" : undefined}>
					<i className="fal fa-fw fa-shopping-cart" />
					{`View Cart`}
				</button>
			</div>
		</Wrapper>
	);
}

SelectProduct.propTypes = {
	allowMultiple: 'boolean'
};

SelectProduct.defaultProps = {
	allowMultiple: false
}

SelectProduct.icon = 'align-center';
