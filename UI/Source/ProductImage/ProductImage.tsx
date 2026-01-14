import Image, {ImageProps} from "UI/Image";
import fallbackImage from './image_placeholder.png';

export type ProductImageProps = ImageProps;

const ProductImage: React.FC<ProductImageProps> = props => {
	return (
		<Image {...props} fileRef={props?.fileRef ?? fallbackImage} />
	)
}

export default ProductImage;