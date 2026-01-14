import { Product } from 'Api/Product';
import ProductSubtitle from 'UI/Product/Subtitle';
import Html from 'UI/Html';

/**
 * Props for the About component.
 */
interface AboutProps {
	/**
	 * associated title
	 */
	title?: string,

	/**
	 * The content to display
	 */
	product: Product,

	/**
	 * The current selected variant if there is one.
	 */
	currentVariant?: Product,
}

/**
 * The About React component.
 * @param props React props.
 */
const About: React.FC<AboutProps> = (props) => {
	const { title, product, currentVariant } = props;
	const content = currentVariant?.descriptionHtml || product.descriptionHtml;

	if (!content?.length) {
		return;
	}

	return <>
		<ProductSubtitle subtitle={title} />
		<div className="ui-product-view__about">
			<Html>
				{content}
			</Html>
		</div>
	</>;
}

export default About;