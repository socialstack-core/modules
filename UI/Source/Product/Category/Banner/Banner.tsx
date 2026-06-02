import { ProductCategory } from 'Api/ProductCategory';
import * as fileRef from 'UI/FileRef';
import Html from 'UI/Html';

/**
 * Props for the Banner component.
 */
interface BannerProps {
	/**
	 * product category to display
	 */
	category: ProductCategory
}

/**
 * The Banner React component.
 * @param props React props.
 */
const Banner: React.FC<BannerProps> = (props) => {
	const { category } = props;

	if (!category || !category.name?.length || !category.descriptionHtml?.length) {
		return;
	}

	const featureInfo = category.featureRef ? fileRef.parse(category.featureRef) : { focalX: 50, focalY: 50 };
	const focalX = featureInfo?.focalX || 50;
	const focalY = featureInfo?.focalY || 50;
	const backgroundStyle = category.featureRef ? {
		backgroundImage: `url(${fileRef.getUrl(category.featureRef)})`,
		backgroundPosition: `${focalX}% ${focalY}%`
	} : {};

	return (
		<div className="ui-category-banner ui-component--full-width" style={backgroundStyle}>
			<div className="ui-category-banner__internal">
				<h1 className="ui-category-banner__title">
					{category.name}
				</h1>
				{category.descriptionHtml?.length > 0 && <>
					<Html>
						{category.descriptionHtml}
					</Html>
				</>}
			</div>
		</div>
	);
}

export default Banner;