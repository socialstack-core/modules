import ProductTable from 'UI/Payments/ProductTable';
import Input from 'UI/Input';
import Form from 'UI/Form';
import { useState } from 'react';
import { useRouter, useSession } from 'UI/Session';
import { useCart } from 'UI/Payments/CartSession';


export default function Checkout(props) {
	const { session } = useSession();
	const { setPage } = useRouter();
	var { shoppingCart, cartIsEmpty, emptyCart, addToCart } = useCart();
	var [termsAccepted, setTermsAccepted] = useState(false);
	
	function canPurchase() {
		return !cartIsEmpty() && termsAccepted;
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
				<ProductTable shoppingCart={shoppingCart} addToCart={addToCart}/>
				
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
					<Input type='text' name='couponCode' label='Coupon code' />
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
};

Checkout.defaultProps = {
}

Checkout.icon='register';
