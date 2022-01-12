import Form from 'UI/Form';
import Input from 'UI/Input';
import { useRouter } from 'UI/Session';
import Loading from 'UI/Loading';
import Spacer from 'UI/Spacer';
import Alert from 'UI/Alert';

export default function CreateDiscount(props) {
	const { hideDiscountPence, hideDiscountPercentage, backButtonUrl } = props;
	const { setPage } = useRouter();

	var [success, setSuccess] = React.useState();
	var [failed, setFailed] = React.useState();
	var [loading, setLoading] = React.useState();

	return (
		<div className="coupons-create-discount">
			<Form 
				action = "discount"
				onSuccess={response => {
					this.setState({success: response, loading: false})
					setPage("/coupon/" + response.id);
				}}
				className="create-discount-form"
				onFailed={e=>{
					console.log(e);
					this.setState({failed:e, loading: false})
				}}
			>
				<Input id="name" name="name" type="text" label="Discount Name"/>
				<Input id="productId" name="productId" type="select" contentType="product" name="productId" label="Discount Product" />
				{!hideDiscountPence && 
					<Input id="discountPercentage" name="discountPercentage" type="number" label="Discount Percentage"/>
				}
				{!hideDiscountPercentage &&
					<Input id="discountPence" name="discountPence" type="number" label="Discount Amount In Pence"/>
				}
				{failed && (
					<Alert type="fail">
						{failed.message ? failed.message : failed == "VALIDATION" && "Please fill in all required fields."}
					</Alert>
				)}
				{loading 
					? (
						<Loading message={"Please wait..."}/>
					) 
					: (
						!success &&
							<div className="submit-button">
								<Spacer height="20"/>
								<Input type="submit" label="Create discount" />
							</div>
					)
				}
			</Form>

			<Spacer height="20"/>
			<div className="back-button">
				<a href={backButtonUrl} className="btn btn-primary">Cancel</a>
			</div>
		</div>
	);
}


CreateDiscount.propTypes = {
	hideDiscountPence: 'bool',
	hideDiscountPercentage: 'bool',
	backButtonUrl: 'string'
};

// use defaultProps to define default values, if required
CreateDiscount.defaultProps = {
	backButtonUrl: '/coupon'	
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
CreateDiscount.icon='badge-dollar'; // fontawesome icon
