import Link from 'UI/Link';

/**
 * Props for the Specifications component.
 */
interface SpecificationsProps {
}

/**
 * The Specifications React component.
 * @param props React props.
 */
const Specifications: React.FC<SpecificationsProps> = (props) => {
	//const { } = props;

	return (
		<aside className="ui-product-specifications">
			<h4 className="ui-product-specifications__title">
				{`Specifications`}
			</h4>
			<p className="ui-product-specifications__description">
				{`No detailed specifications are available for this product.`}
			</p>

			<h4 className="ui-product-specifications__title">
				{`Downloads`}
			</h4>
			<p className="ui-product-specifications__description">
				{`There are no downloads for this product.`}
			</p>
		</aside>
	);
}

export default Specifications;