import { Product } from 'Api/Product';
import ProductSubtitle from 'UI/Product/Subtitle';

/**
 * Props for the Attributes component.
 */
interface AttributesProps {
	/**
	 * Optional section title to display above the attributes.
	 */
	title?: string;

	/**
	 * The product containing attributes to display.
	 */
	product: Product;

	/**
	 * A selected variant if any.
	 */
	currentVariant?: Product;
}

/**
 * Represents a single attribute value with optional units and a reference.
 */
type AttributeValue = {
	value?: string;
	featureRef?: string;
	units?: string;
};

/**
 * The Attributes React component.
 * Renders a table of product attributes grouped by attribute name.
 *
 * @param props React component props.
 */
const Attributes: React.FC<AttributesProps> = ({ title, product, currentVariant }) => {
	let { attributes } = product;

	if (currentVariant?.attributes?.length) {
		// Use the full set of a variants attributes:
		attributes = currentVariant.attributes;
	}

	/**
	 * Build a map of attribute names to their associated values , grouped by attribute type
	 */
	const attributeMap: Record<string, AttributeValue[]> = (attributes || [])
	.sort((a, b) => {
		const groupCompare = (a.attribute?.productAttributeGroupId ?? 0) - (b.attribute?.productAttributeGroupId ?? 0);
		if (groupCompare !== 0) return groupCompare;

		const nameA = a.attribute?.name?.toLowerCase() ?? "";
		const nameB = b.attribute?.name?.toLowerCase() ?? "";
		return nameA.localeCompare(nameB);
	})
	.reduce(function (
		acc: Record<string, AttributeValue[]>,
		attributeObj
	) {
		const { attribute, value, featureRef } = attributeObj;

		// Defensive check in case attribute or name is undefined
		if (!attribute || !attribute.name) {
			return acc;
		}

		const name = attribute.name;

		if (!acc[name]) {
			acc[name] = [];
		}

		acc[name].push({
			value: value,
			featureRef: featureRef,
			units: attribute.units,
		});

		return acc;
	}, {});

	return (
		<>
			{/* Optional title header */}
			<ProductSubtitle subtitle={title} />

			{/* Attribute table */}
			<table className="table table-bordered ui-product-view__attributes">
				<tbody>
				{Object.entries(attributeMap).map(function ([name, values]) {
					return (
						<tr key={name}>
							<td>{name}</td>
							<td>
								{values
									.map(function (attr) {
										let displayValue = '';
										if (attr.value) {
											displayValue += attr.value;
										}
										if (attr.units) {
											displayValue += ' ' + attr.units;
										}
										return displayValue;
									})
									.join(', ')
								}
							</td>
						</tr>
					);
				})}
				</tbody>
			</table>
		</>
	);
};

export default Attributes;
