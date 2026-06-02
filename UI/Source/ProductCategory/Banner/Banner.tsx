import { ProductCategory } from 'Api/ProductCategory';
import * as fileRef from 'UI/FileRef';
import Html from 'UI/Html';
import Image from 'UI/Image';

/**
 * Props for the Product Category banner component.
 */
interface ProductCategoryBannerProps {
	/**
	 * product category to display
	 */
	category: ProductCategory
}

/**
 * The Product Category banner React component.
 * @param props React props.
 */
const ProductCategoryBanner: React.FC<ProductCategoryBannerProps> = (props) => {
	const { category } = props;

	if (!category || !category.name?.length || !category.descriptionHtml?.length) {
		return;
	}

	var baseClass = 'ui-product-category-banner';
	var classNames = [baseClass];

	if (category.replaceWhiteWithTransparency) {
		classNames.push(`${baseClass}--white-to-transparent`);
	}

	const featureInfo = category.featureRef ? fileRef.parse(category.featureRef) : { focalX: 50, focalY: 50 };
	const focalX = featureInfo?.focalX || 50;
	const focalY = featureInfo?.focalY || 50;
	const backgroundStyle = category.featureRef ? {
		backgroundImage: `url(${fileRef.getUrl(category.featureRef)})`,
		backgroundPosition: `${focalX}% ${focalY}%`
	} : {};

	return <>
		{/* hide background image - limit to product image overlay */}
		{/* <div className={classNames.join(' ')} style={backgroundStyle}>*/}
		<div className={classNames.join(' ')}>
			<div className={`${baseClass}__internal`}>
				{category.productImageRef && <>
					<Image className={`${baseClass}__image`} size={512} fileRef={category.productImageRef} />
				</>}
				<h1 className={`${baseClass}__title`}>
					{category.name}
				</h1>
				{category.descriptionHtml?.length > 0 && <>
					<Html>
						{category.descriptionHtml}
					</Html>
				</>}
			</div>
		</div>
	</>;
}

export default ProductCategoryBanner;