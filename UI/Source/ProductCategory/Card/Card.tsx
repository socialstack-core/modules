import { ProductCategory } from 'Api/ProductCategory';
import Image from 'UI/Image';
import Link from 'UI/Link';
import defaultImageRef from './image_placeholder.png';

/**
 * Props for the Card component.
 */
interface CardProps {
	/**
	 * The content to display in this signpost. Requires primaryUrl to have been included.
	 */
	content: ProductCategory,

	/* optional CTA label (defaults to "View range") */
	ctaLabel?: string
}

/**
 * The Card React component.
 * @param props React props.
 */
const Card: React.FC<CardProps> = (props) => {
	const { content } = props;
	const ctaLabel = props.ctaLabel || `View range`;

	return (
		<div className="ui-productcategory-card">
			<Image className="ui-productcategory-card__image" size={512} fileRef={content.productImageRef || content.featureRef || defaultImageRef} />
			<div className="ui-productcategory-card__internal">
				<p className="ui-productcategory-card__category">
					{content.name}
				</p>

				<Link className="ui-productcategory-card__link" href={content.primaryUrl}>
					<span>
						{ctaLabel}
					</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
			</div>
		</div>
	);
}

export default Card;