import Input from 'UI/Input';
import { Product } from 'Api/Product';


/**
 * Props for the ProductTypes component.
 */
interface ProductTypesProps {
	currentContent?: Product
}

const ProductTypes: React.FC<ProductTypesProps> = (props) => {
	const { currentContent } = props;
	const isVariant = !!(currentContent?.variantOfId);

	return <Input {...props} type='select'>
			<option value='0'>{`Physical product`}</option>
			<option value='1'>{`Digital product - no delivery necessary`}</option>
			{!isVariant && <option value='2'>{`Variant product - varies by colour, size etc`}</option>}
		</Input>;
}

export default ProductTypes;
