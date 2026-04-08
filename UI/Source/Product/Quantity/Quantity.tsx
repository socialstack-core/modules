import { Product } from 'Api/Product';
import { useSession } from 'UI/Session';
import { useCart } from 'UI/Payments/CartSession';
import { useRef, useState, useEffect, useId } from "react";
import { CurrencyAmount } from 'UI/Product/Price';
import getConfig from 'UI/Config';
import Button from 'UI/Button';

/**
 * Props for the Quantity component.
 */
interface QuantityProps {
	product: Product,

	/**
	 * optional call to action label
	 */
	ctaLabel?: string,

	/**
	 * optional additional classnames
	 */
	className?: string,

	/**
	 * Overriding price to display
	 */
	priceOverride?: CurrencyAmount,

	/**
	 * Overriding quantity to display
	 */
	qtyOverride?: Number,

	/** 
	 * set true if quantity should be fixed
	 */
	readOnly?: boolean,

	/**
	 * Allow the passing in a minimum value
	*/
	min?:Number,

	/**
	 * Allow the passing in max value for returns etc 
	*/
	max?:Number,

	/**
	 * Allow the passing in of a bundle size 
	*/
	bundle?:Number,	

	/**
	 * Updates the cart if onChange is not specified.
	 * @param newQty
	 * @returns
	 */
	onChange?: (newQty: int) => void

	/** 
	 * set true if control should be as small as possible
	 * (used within order details view)
	 */
	compact?: boolean,
}

/**
 * The Quantity React component.
 * @param props React props.
 */
const Quantity: React.FC<QuantityProps> = (props) => {
	const { onChange } = props;
	const uniqueId = useId();
	const anchorName = `--product-qty-${uniqueId}`;

	// min / max / bundle quantity for product
	const min = props.min || 0;
	const max = props.max || 999;
	const bundle = props.bundle || 1; // if product can only be ordered in multiples of [x]

	const { product, className, priceOverride, qtyOverride, readOnly, compact } = props;

	const { session } = useSession();
	const { locale } = session;

	var { lessTax, addToCart, getCartQuantity, loading } = useCart();
	const quantity = qtyOverride || (getCartQuantity ? getCartQuantity(product?.id || 0) : 0);
	const [typedQuantity, setTypedQuantity] = useState(quantity.toString());
	const [isEditing, setIsEditing] = useState(false);
	const [showAddConfirmation, setShowAddConfirmation] = useState(false);
	const [usePopover, setUsePopover] = useState(false);
	const wrapperRef = useRef(null);
	const inputRef = useRef(null);
	const alertRef = useRef(null);

	const cfg = getConfig<ProductConfig>("Product");
	const allowSaleWhenOutOfStock = product.continueSellingWithNoStock;

	const ctaLabel = props.ctaLabel?.length ? props.ctaLabel : `Add to Basket`;
    
	const setQuantity = (newQty: int) => {

		if (!product) {
			return;
		}

		if (onChange) {
			onChange(newQty);
		} else {
			addToCart(product.id, newQty, false);
		}
	};

	// Ensure text copy is updated as well when any other qty edit occurs
	useEffect(() => {
		setTypedQuantity(quantity.toString());
	}, [
		quantity
	]);

	useEffect(() => {

		// for those browsers which don't support CSS anchor positioning (Firefox as of 09/12/2025),
		// trigger absolute positioning instead of the use of a popover
		setUsePopover(CSS.supports('position-anchor', '--anchor-name'));

		const wrapper = wrapperRef.current;

		const handleFocusOut = () => {
			setTimeout(() => {
				if (wrapper && !wrapper.contains(document.activeElement)) {
					setIsEditing(false);
				}
			}, 0);
		};

		if (wrapper) {
			wrapper.addEventListener('focusout', handleFocusOut);
		}

		return () => {
			if (wrapper) {
				wrapper.removeEventListener('focusout', handleFocusOut);
			}
		};
	}, []);

	if (!product) {
		return `Product required`;
	}

	let qtyClasses = ['ui-product-qty'];

	if (!quantity) {
		qtyClasses.push("ui-product-qty--none");
	}

	if (readOnly) {
		qtyClasses.push("ui-product-qty--readonly");
	}

	if (isEditing) {
		qtyClasses.push("ui-product-qty--edit");
	}

	if (loading) {
		qtyClasses.push("ui-product-qty--loading");
	}

	if (compact) {
		qtyClasses.push("ui-product-qty--compact");
	}

	if (className) {
		qtyClasses.push(className);
	}

	function handleFocus(e) {

		setTimeout(() => {
			e.target.select();
		}, 0);

		setIsEditing(true);
	}

	function updateTypedQuantity() {
		let newQty = parseInt(inputRef.current.value, 10);

		if (isNaN(newQty) || newQty < 0) {
			newQty = 0;
		}

		if (max && newQty > max) {
			newQty = max;
		}

		setQuantity(newQty);
		setIsEditing(false);
	}

	function reduceQuantity() {
		let newQty = (quantity == min) ? 0 : quantity - 1;

		if (newQty < 0) {
			newQty = 0;
		}

		setQuantity(newQty);
	}

	function increaseQuantity() {

		if (quantity == 0) {

			if (usePopover) {
				alertRef?.current?.showPopover();
			} else {
				setShowAddConfirmation(true);
			}

			setTimeout(() => {

				if (usePopover) {
					alertRef?.current?.hidePopover();
				} else {
					setShowAddConfirmation(false);
				}

			}, 1500);
		}

		let newQty = quantity + 1;

		if (max && newQty > max) {
			newQty = max;
		}

		setQuantity(newQty);
	}

	let amount: ulong | undefined;
	let disabled: boolean | undefined;

	// TODO: determine when product has options
	let hasOptions = false;

	if (priceOverride) {
		amount = priceOverride.amount;
	} else {
		// NB: This will be replaced again when per-user pricing and the tax resolver is added
		var tiers = null;

		if (product?.calculatedPrice && locale) {
			var calculatedPrice = product.calculatedPrice;

			if (calculatedPrice.discountedPrice && calculatedPrice.discountedPrice.length > 0) {
				tiers = calculatedPrice.discountedPrice;
			} else if (calculatedPrice.listPrice && calculatedPrice.listPrice.length > 0) {
				tiers = calculatedPrice.listPrice;
			}
		}

		if (!tiers) {
			disabled = !allowSaleWhenOutOfStock && !quantity ? true : undefined;
		} else {
			hasOptions = tiers.length > 1;

			// Get the lowest one:
			var tier = tiers[0];

			if (hasOptions) {
				for (var i = 1; i < tiers.length; i++) {
					var current = tiers[i];

					if (current.amount < tier.amount) {
						tier = current;
					}
				}
			}

			amount = lessTax ? tier.amountLessTax : tier.amount;
		}
	}

	const noSelection = !quantity && !readOnly;
	var outOfStock = product.stock === 0;

	if (!allowSaleWhenOutOfStock && outOfStock) {
		disabled = true;
	}

	const alertClasses = "ui-product-qty__alert" + (showAddConfirmation ? " ui-product-qty__alert--shown" : "");
	
	if(product?.hidden){
		return <div className={qtyClasses.join(' ')}>
			<div className="ui-product-qty__inner" style={{ 'anchor-name': anchorName }}>
				<span className="ui-product-qty__na">
					{`No longer available`}
				</span>
				{!isEditing && !readOnly && quantity>0 && <>
					<Button sm={compact ? undefined : true} xs={compact ? true : undefined} className="ui-product-qty__down"
						aria-label={`Remove`} onClick={() => setQuantity(0)}>
						<i className={"fr fr-trash-alt"}></i>
					</Button>
				</>}
			</div>
		</div>;
	}

	// NB: buttons have tabindex explicitly set to zero to prevent Safari from losing focus on click
	// (especially important when this control is rendered within the main search dropdown)
	return (
		<div className={qtyClasses.join(' ')}>
			<div className="ui-product-qty__inner" style={{ 'anchor-name': anchorName }}>
                
				{!isEditing && !readOnly && <>
					<Button tabindex="0" sm={compact ? undefined : true} xs={compact ? true : undefined} className="ui-product-qty__down"
						aria-label={`Reduce quantity`} onClick={() => reduceQuantity()}>
						<i className={quantity > 1 ? "fr fr-minus" : "fr fr-trash-alt"}></i>
					</Button>
				</>}

				<div className="ui-product-qty__value-wrapper" ref={wrapperRef}>
					<input ref={inputRef} type="number" value={typedQuantity} className="ui-product-qty__value"
						min={min} max={max} step={bundle} disabled={readOnly}
						onFocus={(e) => handleFocus(e)}
						onInput={(e) => setTypedQuantity(e.target.value)}
						onKeyDown={(e) => {

							switch (e.key) {

								case 'Enter':
									updateTypedQuantity();
									(e.target as HTMLElement).blur();
									e.preventDefault();
									break;

								case 'Escape':
									(e.target as HTMLElement).blur();
									e.preventDefault();
									break;

							}

						}} />

					<Button tabindex="0" sm={compact ? undefined : true} xs={compact ? true : undefined} className="ui-product-qty__update" onClick={() => updateTypedQuantity()}
						onKeyDown={(e) => {

							switch (e.key) {
								case 'Enter':
									updateTypedQuantity();
									(e.target as HTMLElement).blur();
									e.preventDefault();
									break;

								case 'Escape':
									setIsEditing(false);
									(e.target as HTMLElement).blur();
									break;
							}

						}}>
						{`Update`}
					</Button>
				</div>

				<div popover={usePopover ? "manual" : undefined} className={alertClasses}
					ref={alertRef} style={{ 'position-anchor': anchorName }}>
					{quantity > 1 ? `${quantity} items added to basket` : `Added to basket`}
				</div>

				<Button tabindex="0" sm={compact ? undefined : true} xs={compact ? true : undefined} className="ui-product-qty__up"
					aria-label={noSelection ? undefined : `Increase quantity`} onClick={() => increaseQuantity()} disabled={disabled}>
					{noSelection && !disabled && <>
						{ctaLabel}
					</>}
					{noSelection && disabled && <>
						{`Not available`}
					</>}
					{!noSelection && <>
						<i className="fr fr-plus"></i>
					</>}
				</Button>
			</div>
		</div>
	);
}

export default Quantity;