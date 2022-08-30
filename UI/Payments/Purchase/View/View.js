import Alert from 'UI/Alert';
import Content from 'UI/Content';
import Loading from 'UI/Loading';
import ProductTable from 'UI/Payments/ProductTable';

export default function View(props){
	
	return <Content primary includes={['productQuantities']}>
		{(purchase, loading) => {
			
			if(loading){
				return <Loading/>;
			}
			
			if(!purchase){
				return <Alert type='error'>That purchase was not found. You'll need to be logged in as the person who created it to see it.</Alert>
			}
			
			return <>
				<h1>
					{`Details about your purchase`}
				</h1>
				<ProductTable readonly shoppingCart={{items:purchase.productQuantities}} />
			</>;
			
		}}
	</Content>;
	
}