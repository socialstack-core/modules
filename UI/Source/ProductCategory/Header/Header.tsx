import { ProductCategory } from "Api/ProductCategory";
import Image from 'UI/Image';
import Html from 'UI/Html';
import defaultImageRef from './image_placeholder.png';

/**
 * Props for the ProductCategory Header component.
 */
interface ProductCategoryHeaderProps {
	// Connected via a graph in the page, which is also where the includes are defined.
	// This component requires at least the following includes:
	// productCategories, productCategories.primaryUrl
	productCategory: ProductCategory
}

/**
 * The ProductCategory Header React component.
 * @param props React props.
 */
const ProductCategoryHeader: React.FC<ProductCategoryHeaderProps> = (props) => {
	const { productCategory } = props;

	if (!productCategory) {
		return;
	}

	return (
		<div className="ui-productcategory-header">
			<h1 className="ui-productcategory-header__title">
				{productCategory.name}
			</h1>
			<div className="ui-productcategory-header__details">

				<Html>
					{productCategory.descriptionHtml}
				</Html>

				{/* TODO: remove once we have info coming in from productCategory.description */}
				{!productCategory.descriptionHtml && productCategory.parentId != null && <>
					{`:: Product description to appear here ::`}
				</>}
			</div>
			<Image className="ui-productcategory-header__image" size={512} fileRef={productCategory.featureRef || defaultImageRef} />
		</div>
	);
}

export default ProductCategoryHeader;