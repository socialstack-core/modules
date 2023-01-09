import ProductTable from 'UI/Payments/ProductTable';
import Input from 'UI/Input';
import Form from 'UI/Form';
import Alert from 'UI/Alert';
import webRequest from 'UI/Functions/WebRequest';
import { useState, useEffect } from 'react';
import { useRouter, useSession } from 'UI/Session';
import { useCart } from 'UI/Payments/CartSession';
import store from 'UI/Functions/Store';


export default function Checkout(props) {
	const { hideCoupon } = props;
	const { session } = useSession();
	const { setPage } = useRouter();
	var { shoppingCart, cartIsEmpty, emptyCart, addToCart } = useCart();
	var [termsAccepted, setTermsAccepted] = useState(false);
	var [couponCode, setCouponCode] = useState(null);
	var [coupon, setCoupon] = useState(null);
	var [codeLoading, setCodeLoading] = useState(false);
	var [couponValid, setCouponValid] = useState(true);
	var [couponValidationMessage, setCouponValidationMessage] = useState('');
	var [couponApplyDisabled, setCouponApplyDisabled] = useState(true);

	useEffect(() => {
		loadCouponCode(store.get('coupon_code'));
	}, []);
	
	function loadCouponCode(code) {
		if(!code){
			return Promise.resolve(null);
		}
		
		setCouponCode(code);
		setCodeLoading(true);
		setCouponApplyDisabled(false);

		return webRequest('coupon/check/' + code)
			.then(response => {
				setCodeLoading(false);
				setCouponValid(true);
				setCouponValidationMessage(`This coupon has been applied to your total.`);
				var coupon = response.json;
				setCoupon(coupon);
			})
			.catch(e => {
				console.error(e);
				setCouponValid(false);
				setCouponValidationMessage(e.message);
				setCodeLoading(false);
				setCoupon(null);
			});
	}

	function updateCouponCode(code) {
		store.set('coupon_code', code);
		loadCouponCode(code);
    }
	
	function canPurchase() {
		return !cartIsEmpty() && termsAccepted;
	}

	function applyCouponCode() {
		var codeField = document.getElementById("coupon_code");
		var code = codeField.value;
		updateCouponCode(code);
	}

	function removeCouponCode() {
		setCouponCode(null);
		setCoupon(null);
		setCouponValid(true);
		setCouponValidationMessage('');
		setCouponApplyDisabled(true);
    }
	
	return <div className="payment-checkout">
		<h2 className="payment-checkout__title">
			{`Checkout`}
		</h2>
		<Form 
			action='purchase/submit'
			failedMessage={`Unable to purchase`}
			loadingMessage={`Purchasing..`}
			onSuccess={info => {
				
				// Clear cart:
				emptyCart();
				
				if(info.nextAction){
					// Go to it now:
					window.location = info.nextAction;
				}else{
					if(info.status >= 200 && info.status < 300){
						setPage('/complete?status=success');
					}else if(info.status < 300){
						setPage('/complete?status=pending');
					}else{
						setPage('/complete?status=failed');
					}
				}
			}}
		>
			<div className="mb-3">
				<ProductTable shoppingCart={shoppingCart} addToCart={addToCart} coupon={coupon}/>
				
				<input type='hidden' name='items' ref={ir=>{
					if(ir){
						ir.onGetValue=(val, ele)=>{
							if(ele == ir){
								return shoppingCart.items;
							}
						};
					}
				}} />
				
				{!cartIsEmpty() && <>
					{!hideCoupon && <>
						<div className="mb-3">
							<label htmlFor="coupon_code" className="form-label">
								{`Coupon code`}
							</label>
							<div className="input-group coupon-code-group">
								<input id="coupon_code" type="text" className={couponValid ? "form-control" : "form-control is-invalid"}
									placeholder={`Enter discount code here`} name="couponCode" readonly={coupon && couponValid}
									onKeypress={e => {

										if (!(coupon && couponValid) && e.keyCode === 13) {
											applyCouponCode();
                                        }

                                    }}
									onInput={e => {
										var val = e.target.value;
										var disabled = !val || !val.length;
										setCouponApplyDisabled(disabled);

										if (disabled) {
											setCouponValid(true);
											setCouponValidationMessage('');
                                        }
									}}
								defaultValue={couponCode}
								/>
								<button className={coupon && couponValid ? "btn btn-danger" : "btn btn-primary"} type="button"
									disabled={(couponApplyDisabled || codeLoading) ? 'disabled' : undefined}
									onClick={() => {
										if (coupon && couponValid) {
											removeCouponCode();
										} else {
											applyCouponCode()
                                        }
									}} style="minwidth: 12.5rem">
									{coupon && couponValid ? `Remove coupon` : `Apply coupon`}
								</button>
							</div>
							{!couponValid && <>
								<div className="validation-error">
									{couponValidationMessage}
								</div>
							</>}
						</div>
						{coupon && <Alert type='success'>
							{couponValidationMessage}
						</Alert>}
					</>}
					<Input type='payment' name='paymentMethod' label='Payment method' validate={['Required']} />
					<div class="form-check">
						<input class="form-check-input" type="checkbox" id="termsCheckbox" checked={termsAccepted ? 'checked' : undefined}
							onChange={e => setTermsAccepted(e.target.checked)} />
						<label class="form-check-label" htmlFor="termsCheckbox">
							{`I have read and agree to both the `}
							<a href="/terms-and-conditions" target="_blank" rel="noopener noreferrer">
								{`terms and conditions`}
							</a>
							{` and `}
							<a href="/privacy-policy" target="_blank" rel="noopener noreferrer">
								{`privacy policy`}
							</a>
						</label>
					</div>
				</>}
				
			</div>
			{!cartIsEmpty() && <>
				<div className="payment-checkout__footer">
					<button type="submit" className="btn btn-primary"
						disabled={!canPurchase() ? "disabled" : undefined}>
						<i className="fal fa-fw fa-credit-card" />
						{`Confirm Purchase`}
					</button>
				</div>
			</>}
		</Form>
	</div>;
}

Checkout.propTypes = {
	hideCoupon: 'boolean'
};

Checkout.defaultProps = {
	hideCoupon: false
}

Checkout.icon='register';
