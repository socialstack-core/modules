import MultiSelect, { MultiSelectProps } from 'Admin/MultiSelect';
import { Product } from 'Api/Product';

export default function ProductMultiSelect(props: MultiSelectProps<Product>) {
	return (
		<MultiSelect
			{...props}
			contentType="Product"
			onQuery={(filter, query) => {
				filter.query = "name contains ? OR sku contains ?";
				filter.args = [query, query];
			}}
			renderEntry={(entry: Product) => (
				<span>{entry.sku ? '(' + entry.sku + ') ' : ''}{entry.name}</span>
			)}
			renderSearchResult={(entry: Product) => (
				<span>{entry.sku ? '(' + entry.sku + ') ' : ''}{entry.name}</span>
			)}
		/>
	);
}