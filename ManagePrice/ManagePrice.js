import webRequest from 'UI/Functions/WebRequest';
import Loop from 'UI/Loop'; 
import Form from 'UI/Form';
import Input from 'UI/Input';
import Loading from 'UI/Loading';
import { useRouter } from 'UI/Session';
import Alert from 'UI/Alert';
import Row from 'UI/Row';
import Col from 'UI/Column';

export default function ManagePrice(props) {
	const { product, manageProductUrl, onSuccess, onDelete } = props;

	var [price, setPrice] = React.useState(props.price);
	var [isRecurring, setIsRecurring] = React.useState(props.price?.isRecurring);
	var [failed, setFailed] = React.useState();
	var [loading, setLoading] = React.useState();

	const removePrice = price => {
		if (!price) {
			return;
		}

		setLoading(true);

		webRequest('price/' + price.id, price, { method: 'delete' }).then(response => {
			setPage(manageProductUrl + "/" + price.productId);
		}).catch(e => {
			console.log(e);
	
			if (!e.message) {
				e.message = "Something went wrong, please try again later.";
			}
	
			setFailed(e);
			setLoading(false);
			if(onDelete) {
				onDelete();
			}
		});
	}

	if (!product) {
		return <div className="products-manage-price">
			This component is missing a product.
		</div>;
	}
	
	return (
		<div className="products-manage-price">
			<Form 
				action = { price ? "price" + "/" + price.id : "price" }
				className="create-product-form"
				onSuccess={response => {
					setPrice(response);
					setIsRecurring(response?.isRecurring);
					setLoading(false);
					setFailed(false);
					if(onSuccess) {
						onSuccess();
					}
				}}
				onValues = {values => {
					values.productId = product.id

					return values;
				}}
				onFailed={e=>{
					setFailed(e);
					setLoading(false);
				}}
			>
				<Input id="name" name="name" type="text" label="Price Name" placeholder="Price name" validate={['Required']} value={price?.name}/>
				<Input id="costPence" name="costPence" type="number" label="Cost (pence)" validate={['Required']} value={price?.costPence}/>
				<Input id="isRecurring" name="isRecurring" type="checkbox" label="Is Recurring?" value={isRecurring} onChange={e => {setIsRecurring(!isRecurring);}}/>
				{isRecurring &&
					<div className="price-recurring-options">
						<h5>Recurring Options</h5>
						<Input id="isMetered" name="isMetered" type="checkbox" label="Is Metered?" defaultValue={price?.isMetered}/>
						<Input id="recurringPaymentIntervalMonths" name="recurringPaymentIntervalMonths" type="number" label="Recurring Payment Interval (months)" value={price ? price.recurringPaymentIntervalMonths : 1}/>
					</div>
				}

				<div className="submit-button">
					<Input type="submit" label={price ? "Update Price" : "Create Price"} disabled={loading} />
				</div>

				<div className="remove-button">
					<button className="btn btn-primary" onClick={e => removePrice(price)} disabled={loading}>Remove</button>
				</div>

				{loading &&
					<div>
						<Loading message="Loading..." />
					</div>
				}
				{failed &&
					<Alert type="fail">
						{failed.message ? failed.message : failed == "VALIDATION" && "Please fill in all required fields."}
					</Alert>
				}
			</Form>
		</div>
	);
}


ManagePrice.propTypes = {
	manageProductUrl: 'string'
};

// use defaultProps to define default values, if required
ManagePrice.defaultProps = {
	manageProductUrl: '/product'
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
ManagePrice.icon='dollar-sign'; // fontawesome icon
