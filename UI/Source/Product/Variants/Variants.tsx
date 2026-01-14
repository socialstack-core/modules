import { useEffect, useState } from "react";
import { Product } from 'Api/Product';
import AttributeSelectors, { AttributeInfo } from 'UI/Product/Variants/AttributeSelectors';
import { ProductAttributeValue } from 'Api/ProductAttributeValue';
import { ProductAttribute } from 'Api/ProductAttribute';

/**
 * Props for the Variants component.
 */
interface VariantsProps {
	/**
	 * Optional section title to display above the variants.
	 */
	title?: string;

	/**
	 * The product containing variants to display.
	 */
	product: Product;

	/**
	 * Current selected variant (as specified by sku in the URL).
	 */
	currentVariant?: Product;

	/**
	 * Called when a variant is selected. It's null if the variant is unresolved.
	 * @param product
	 * @returns
	 */
	onChange?: (product: Product | undefined) => void;
}

// ------------------------
// Type Definitions
// ------------------------

function getUniqueAttributes(variants: Product[]): ProductAttribute[] {
	const matrix = new Map<uint, ProductAttribute>();

	for (const variant of variants) {
		const attributes = variant.additionalAttributes;

		if (!attributes) {
			continue;
		}

		for (var i = 0; i < attributes.length; i++) {
			const attrValue = attributes[i];
			if (!attrValue || !attrValue.value || !attrValue.attribute) {
				continue;
			}

			const { attribute } = attrValue;

			if (!matrix.has(attribute.id)) {
				matrix.set(attribute.id, attribute);
			}
		}
	}

	// Next, ensure the results are always in the same order. This prevents any potential 
	// odd order behaviour if attributes happen to not be in the same order in the upstream attribute sets.
	const result = Array.from(matrix.values());
	result.sort((a, b) => a.id - b.id);
	return result;
}

function getAttributesWithValues(selectedValues: Map<uint, uint>, variants: Product[]) {

	// Step 1: calculate the unique set of attributes. 
	// We can't get their values just yet as each dropdown has to establish them independently.
	// That is because each dropdown has to ignore its own selectedValue, so some globally pre-filtered array doesn't work.
	// The filtering has to happen as a 2nd step.
	const attributes = getUniqueAttributes(variants);

	// Step 2: calculate the filtered values in each dropdown.
	const attribWithValues = attributes.map(attribute => {

		// Filter out variants that don't satisfy all the selected values *except this one*.
		// This has the effect of removing entries from the dropdowns that aren't otherwise valid.
		const filteredVariants = variants.filter(variant => {
			var hasAll = true;

			selectedValues.forEach((attrValueId, attrId) => {
				if (attrId == attribute.id) {
					// Except this one
					return;
				}

				if (!variant.additionalAttributes?.find(attrVal => attrVal.id == attrValueId)) {
					hasAll = false;
				}
			});

			return hasAll;
		});

		// From the filtered variants we can now collect the set of all values that fit this dropdown.
		const values = [] as ProductAttributeValue[];

		filteredVariants.forEach(variant => {
			const { additionalAttributes } = variant;

			if (!(additionalAttributes?.length)) {
				return;
			}

			additionalAttributes.forEach(attrValue => {
				if (attrValue.productAttributeId != attribute.id) {
					// This value is not for this attribute dropdown.
					return;
				}

				// Already got this value?
				if (!values.find(val => val.id == attrValue.id)) {
					values.push(attrValue);
				}
			});
		});

		let selectedId = selectedValues.get(attribute.id);

		/*
		This is nice but also causes a quirk if a dropdown has 
		1 value and the user selects "select one..". 
		This override will thus cause it to go right back to the singular value (amongst some other fun quirks)

		if (!selectedId && values.length == 1) {
			// If there's only 1 valid value, implicitly auto-select it
			selectedId = values[0].id;
		}
		*/

		return {
			attribute,
			selected: selectedId,
			values
		} as AttributeInfo;

	})
		// Filter out any that have 0 values (it can happen, probably!)
		.filter(awv => !!awv.values.length);

	return attribWithValues;
}

/**
 * The Attributes React component.
 * Renders a table of product attributes grouped by attribute name.
 *
 * @param props React component props.
 */
const Variants: React.FC<VariantsProps> = ({ title, product, onChange, currentVariant }) => {
	const { variants } = product;

	// A mapping of [attribute.id -> attributeValue.id], holding values the user selected.
	// If an attribute isn't present, the user did not select a value for that attribute.
	const [selectedValues, setSelectedValues] = useState<Map<uint, uint>>(() => {
		var result = new Map<uint, uint>();

		// If there is a current variant, populate the map with its values now.
		const attribs = currentVariant?.additionalAttributes;

		if (attribs) {
			// This may set some additional values but they simply won't be hit by the dropdowns so they don't matter.
			// If it misses any (i.e. some other variant has an attrib that this one does not have) then 
			// the .values set will be empty in that dropdown and it will get filtered out anyway.
			attribs.forEach(attrVal => {
				if (!attrVal.value || !attrVal.attribute) {
					return;
				}

				result.set(attrVal.attribute.id, attrVal.id);
			});
		}

		return result;
	});

	if (!variants?.length) {
		return null;
	}

	// Select (or reset by passing null) an attr value
	const selectAttribute = (attr: ProductAttribute, value: ProductAttributeValue | null) => {
		var nextSelectedValues = new Map<uint, uint>(selectedValues);

		if (!value) {
			nextSelectedValues.delete(attr.id);
		} else {
			nextSelectedValues.set(attr.id, value.id);
		}

		// Next calculate if we have a new selected specific product.
		const awv = getAttributesWithValues(nextSelectedValues, variants);

		let selectedVariant: Product | undefined;

		if (awv.filter(val => !val.selected).length == 0) {
			// All of the dropdowns have a value. 
			// We can now filter the product set by this group of selections to get the end result.
			selectedVariant = variants.find(variant => {
				const { additionalAttributes } = variant;

				if (!additionalAttributes) {
					return false;
				}

				let hasAll = true;

				awv.forEach(attribInfo => {

					if (!variant.additionalAttributes?.find(attrVal => attrVal.id == attribInfo.selected)) {
						hasAll = false;
					}

				});

				return hasAll;
			});
		}

		onChange && onChange(selectedVariant);
		setSelectedValues(nextSelectedValues);
	};

	const attribWithValues = getAttributesWithValues(selectedValues, variants);

	return (
		<div className="ui-product-view__variants">
			{/* Optional title header */}
			{title && (
				<h2 className="ui-product-view__subtitle">
					{title}
				</h2>
			)}
			<AttributeSelectors attributes={attribWithValues} onSelect={selectAttribute} />
		</div>
	);
};

export default Variants;
