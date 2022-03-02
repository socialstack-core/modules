import webRequest from 'UI/Functions/WebRequest';
import Loop from 'UI/Loop'; 
import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import Row from 'UI/Row';
import Col from 'UI/Column';

export default function ManageProducts(props) {
	const { createProductUrl, manageProductUrl } = props;
	
	var [failed, setFailed] = React.useState();
	var [loading, setLoading] = React.useState();

	const removeProduct = product => {
		if (!product) {
			return;
		}

		setLoading(true);

		webRequest('product/' + product.id, product, { method: 'delete' }).then(response => {
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

	return (
		<div className="products-manage-products">
			<Loop
				over="product/list"
				asTable
				live
				orNone={() => <div className="no-products">
					No products created.
				</div>}
			>
				{
					[
						// Render Header
						results => {
							return <> 
								<th>Name</th>
								<th>Description</th>
								<th></th>
								<th></th>
							</>;
						},
						// Render Row
						(product, index, resultsCount) => {
							return <>
								<td className="discount--info name">
									<a href={manageProductUrl + "/" + product.id}>{product.name}</a>
								</td>
								<td className="discount--info description">
									{product.description}
								</td>
								<td>
									<a href={manageProductUrl + "/" + product.id} className="btn btn-primary">Edit</a>
								</td>
								<td>
									<button className="btn btn-primary" onClick={e => removeProduct(product)} disabled={loading}>Remove</button>
								</td>
							</>;
						}
					]
				}
			</Loop>
			<div className="create-new-product">
				<a href={createProductUrl} className="btn btn-primary">Create</a>
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
		</div>
	);
}


ManageProducts.propTypes = {
	createProductUrl: 'string',
	manageProductUrl: 'string'
};

// use defaultProps to define default values, if required
ManageProducts.defaultProps = {
	createProductUrl: '/product/create',
	manageProductUrl: '/product'
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
ManageProducts.icon='boxes'; // fontawesome icon
