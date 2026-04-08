import { ProductCategory } from 'Api/ProductCategory';
import Image from 'UI/Image';
import Link from 'UI/Link';
import defaultImageRef from './image_placeholder.png';

/**
 * Props for the Signpost component.
 */
interface SignpostProps {
	/**
	 * The content to display in this signpost. Requires primaryUrl to have been included.
	 */
	content: ProductCategory,
}

/**
 * The Signpost React component.
 * @param props React props.
 */
const Signpost: React.FC<SignpostProps> = (props) => {
	const { content } = props;

	return (
		<div className="ui-productcategory-signpost">
			
			<Link href={content.primaryUrl}>
				<div className="ui-productcategory-signpost__wrapper">
					<div className="ui-productcategory-signpost__image">
						<Image size={200} fileRef={content.productImageRef || content.featureRef || defaultImageRef} />
					</div>

					<div className="ui-productcategory-signpost__name">
						{content.name} 
					</div>
				</div>
			</Link> 
		</div>
	);
}

export default Signpost;