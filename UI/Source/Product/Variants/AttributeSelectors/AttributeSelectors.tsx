import { useState, useMemo, useEffect } from "react";
import Input from 'UI/Input';
import { ProductAttributeValue } from 'Api/ProductAttributeValue';
import { ProductAttribute } from 'Api/ProductAttribute';


// ------------------------
// Type Definitions
// ------------------------
export interface AttributeInfo {
	attribute: ProductAttribute;
	selected?: uint;
	values: ProductAttributeValue[];
}

interface AttributeSelectorsProps {
	attributes: AttributeInfo[];

	/* Called when a value is selected in a given selector. */
	onSelect: (attr: ProductAttribute, value: ProductAttributeValue | null) => void;
}

// ------------------------
// Component
// ------------------------
const AttributeSelectors: React.FC<AttributeSelectorsProps> = (props) => {
	const { attributes, onSelect } = props;

	return <fieldset className="fieldset--xs">
		{attributes.map(attrInfo => {
			const { attribute, selected } = attrInfo;

			return <div key={attribute.id}>
				<Input type="select" label={attribute.name} noSelection={`Select an option ...`} clearable onChange={e => {
					const strId = (e.target as HTMLSelectElement).value;

					if (!strId) {
						// Clear the selection:
						onSelect(attribute, null);
					} else {
						// Locate the original attrib value instance and select it:
						const id = parseInt(strId) as uint;
						const attrValue = attrInfo.values.find(val => val.id == id);
						onSelect(attribute, attrValue ? attrValue : null);
					}

				}}>
					{
						attrInfo.values.map(attrValue => {
							return <option value={attrValue.id} selected={attrValue.id == selected}>
								{attrValue.value + (attribute.units || '')}
							</option>;
						})
					}
				</Input>
			</div>

		})}
	</fieldset>
};

export default AttributeSelectors;
