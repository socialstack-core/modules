import webRequest from 'UI/Functions/WebRequest';
import {useTokens} from 'UI/Token';
import { useRouter } from 'UI/Session';
import Loop from 'UI/Loop'; 
import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import Form from 'UI/Form';
import Input from 'UI/Input';
import Modal from 'UI/Modal';
import ManagePrice from 'UI/ManageProducts/ManagePrice';
import Row from 'UI/Row';
import Col from 'UI/Column';

export default function ManageProduct(props) {
	const { manageProductsUrl, manageProductUrl } = props;
	const { setPage } = useRouter();

	var [product, setProduct] = React.useState();
	var [price, setPrice] = React.useState();
	var [isPriceModalOpen, setIsPriceModalOpen] = React.useState();
	var [failed, setFailed] = React.useState();
	var [loading, setLoading] = React.useState();

	var productId = useTokens('${url.product.id}');

	React.useEffect(() => {		
		if (productId) {
			setLoading(true);

			webRequest('product/' + productId).then(response => {
				setProduct(response?.json);
				setLoading(false);
			}).catch(e => {
				console.log(e);
				setFailed(e);
				setLoading(false);
			});
		}
	}, []);

	const removePrice = price => {
		if (!price) {
			return;
		}

		setLoading(true);

		webRequest('price/' + price.id, price, { method: 'delete' }).then(response => {
			setLoading(false);
		}).catch(e => {
			console.log(e);
	
			if (!e.message) {
				e.message = "Something went wrong, please try again later.";
			}
	
			setFailed(e);
			setLoading(false);
		});
	}

	const editPrice = thePrice => {
		setIsPriceModalOpen(true);
		setPrice(thePrice);
	}

	const closePriceModal = () => {
		setIsPriceModalOpen(false);
		setPrice(null);
	}

	return (
		<div className="products-manage-product">

			<div className="back-button">
				<a className="btn-back" href={manageProductsUrl}>Back</a>
			</div>

			<div className="product-details">
				<h4>Product Details</h4>
				<Form 
					action = { product ? "product" + "/" + product.id : "product" }
					onSuccess={response => {
						if (!product) {
							setPage(manageProductUrl + "/" + response.id);
						}

						setProduct(response);
						setLoading(false);
						setFailed(false);
					}}
					className="create-product-form"
					onFailed={e=>{
						setFailed(e);
						setLoading(false);
					}}
				>
					<Input id="name" name="name" type="text" label="Product Name" placeholder="Product name" validate={['Required']} value={product?.name}/>
					<Input id="description" name="description" type="textarea" label="Description" placeholder="Product description" value={product?.description}/>

					<div className="submit-button">
						<Input type="submit" label={product ? "Update Product" : "Create Product"} disabled={loading} />
					</div>
				</Form>
			</div>

			{product &&
				<div className="price-details">
					<h4>Prices</h4>
					<Loop
						over="price/list"
						asTable
						live
						filter={{where: {productId: product.id}}}
						orNone={() => <div className="no-prices">
							No prices for this product.
						</div>}
					>
						{
							[
								// Render Header
								results => {
									return <> 
										<th>Name</th>
										<th>Cost (pence)</th>
										<th>Recurring</th>
										<th>Metered</th>
										<th>Payment Interval (months)</th>
										<th></th>
										<th></th>
									</>;
								},
								// Render Row
								(price, index, resultsCount) => {
									return <>
										<td className="discount--info name">
											<a href="#" onClick={e => editPrice(price)} disabled={loading}>{price.name}</a>
										</td>
										<td>
											{price.costPence}
										</td>
										<td>
											<i class={price.isRecurring ? "fas fa-check" : "fas fa-times"}></i>
										</td>
										<td>
											<i class={price.isMetered ? "fas fa-check" : "fas fa-times"}></i>
										</td>
										<td>
											{price.recurringPaymentIntervalMonths}
										</td>
										<td>
											<button className="btn btn-primary" onClick={e => editPrice(price)} disabled={loading}>Edit</button>
										</td>
										<td>
											<button className="btn btn-primary" onClick={e => removePrice(price)} disabled={loading}>Remove</button>
										</td>
									</>;
								}
							]
						}
					</Loop>
					<div className="create-new-price">
						<button className="btn btn-primary" onClick={e => editPrice()} disabled={loading}>Add new price</button>
					</div>

					{isPriceModalOpen &&
						<Modal 
							visible
							isLarge
							title={price ? "Edit Price" : "Create Price"}
							onClose={closePriceModal}
						>
							<ManagePrice 
								price={price}
								product={product}
								onDelete={closePriceModal}
								onSuccess={closePriceModal}
							/>
						</Modal>
					}
				</div>
			}
			
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
		</div>
	);
}


ManageProduct.propTypes = {
	manageProductsUrl: 'string',
	manageProductUrl: 'string'
};

// use defaultProps to define default values, if required
ManageProduct.defaultProps = {
	manageProductsUrl: '/products',
	manageProductUrl: '/product'
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
ManageProduct.icon='box'; // fontawesome icon
