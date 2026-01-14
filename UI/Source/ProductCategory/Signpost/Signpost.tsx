import { ProductCategory } from 'Api/ProductCategory';
import Image from 'UI/Image';
import Link from 'UI/Link';

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
				<Image size={200} fileRef={content.featureRef!} />
				{content.name}
			</Link>
		</div>
	);
}

export default Signpost;