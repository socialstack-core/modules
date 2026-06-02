import ContentSelect, { ContentSelectProps } from 'Admin/ContentSelect';
import productApi, { Product } from 'Api/Product';

export default function ProductSelect(props: ContentSelectProps<Product>) {
	
	return (
		<ContentSelect
			{...props}
			contentType="Product"
			search="name"
			label={props.label || "Product"}
			endpoint={productApi.list}
			onQuery={(filter, query) => {
				filter.query = "name contains ? OR sku contains ?";
				filter.args = [query, query];
			}}
			onRender={(product: Product) => {
				return (
					<span>
						{product.sku ? '(' + product.sku + ') ' : ''}{product.name} 
					</span>
				)
			}}
		/>
	);
}