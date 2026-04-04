import { useEffect, useState } from "react";
import Loop from "UI/Loop";
import ProductAttributeApi, { ProductAttribute } from "Api/ProductAttribute";
import { Product } from "Api/Product";
import ProductAttributeValueApi from "Api/ProductAttributeValue";
import Input from "UI/Input";
import Button from "UI/Button";
import Image from "UI/Image";
import Alert from "UI/Alert";
import Icon from "UI/Icon";
import Link from "UI/Link";
import Video from "UI/Video";
import Col from "UI/Column";
import Row from "UI/Row";
import AttributeSelect from "Admin/Payments/AttributeSelect";
import Container from "UI/Container";
import ConfirmDialog from "UI/Dialog/ConfirmDialog";

type VariantEditorProps = {
	variant: Product,
	prefix: string,
	onChange: (variant: Product) => void,
	requiredAttributes: ProductAttribute[] | undefined
};

const VariantEditor: React.FC<VariantEditorProps> = (props) => {
	
	// NB this can't be a form as it's surrounded by the main admin form.
	const { variant, requiredAttributes, prefix, onChange } = props;
	const { additionalAttributes } = variant;

	const updateField = (name: string, newValue: any) => {
		const newVariant = { ...variant };
		newVariant[name] = newValue;
		onChange && onChange(newVariant);
	};

	/** Use prefix if giving the inputs a name but do note that by doing so they will end up in any 
	 *  surrounding form fieldsets too, and using onChange makes them most likely unnecessary anyway
	 * */
	return <div className="variants-variant-editor">
		<Row>
			<Col>
				<Input type='image' value={variant.featureRef} onChange={e => updateField('featureRef', e.target.value)} />
			</Col>
			<Col>
				<Input type='text' label={`Name`} required validate={['Required']} value={variant.name} onChange={e => updateField('name', e.target.value)} />
				<Input type='text' defaultValue={variant.sku} label={`Sku`} onChange={e => updateField('sku', e.target.value)} />
				<AttributeSelect
					value={additionalAttributes}
					requiredAttributes={requiredAttributes}
					label={`Attributes`}
					onChange={e => updateField('additionalAttributes', e.fullValue)}
				/>
				<div className="variants-variant-editor__actions">
					{variant.id && <Link target='_blank' href={'/en-admin/product/' + variant.id}>{`Edit more details`}</Link>}
					<Button danger onClick={() => props.onRemove(variant)}><Icon type='fa-trash' /></Button>
				</div>
			</Col>
		</Row>
	</div>;
	
};

const ValueEditor: React.FC = (props : any) => {

	const { requiredAttributes } = props;
	const [inputHandle, setInputHandle] = useState(null);
	const [deleting, setDeleting] = useState<Product | undefined>();
	
	const [value, setValue] = useState(() => {
		var initVal = (props.value || props.defaultValue || []).filter(t => t!=null);
		return initVal;
	});
	
	const onRemove = (variant: Product) => {
		variant.deleteVariant = true;
		setValue([...value]);
	};

    return <div className="variants-value-editor">
		{props.label && !props.hideLabel && (
			<label className="form-label">
				{props.label}
			</label>
		)}
		
		<div className="variants-value-editor__entries">
			{value.map((variant, index) => !variant || variant.deleteVariant ? null : <VariantEditor
				prefix={props.name + '_' + index}
				requiredAttributes={requiredAttributes}
				variant={variant}
				onRemove={() => setDeleting(variant)}
				onChange={variant => {
					const newValue = [...value];
					newValue[index] = variant;
					setValue(newValue)
				}}
			/>)}
		</div>
		<Button onClick={() => {
			setValue([...value, {}]);
		}}>{`Add variant`}</Button>
		
		<input type="hidden" name={props.name} ref={ele => {
			setInputHandle(ele);

			if (ele != null) {
				ele.onGetValue = (v, input, e) => {
					if (input != inputHandle) {
						return v;
					}

					return value
						.filter((entry: Product) => entry != undefined)
						.map((entry: Product) => {
							const {
								additionalAttributes
							} = entry;

							// Specifically only pass the fields we want to update.
							// This helps minimise conflicts with other people editing the same product.
							return {
								id: entry.id,
								deleteVariant: entry.deleteVariant,
								name: entry.name,
								sku: entry.sku,
								featureRef: entry.featureRef,
								additionalAttributes: additionalAttributes ? additionalAttributes.map(attrib => attrib.id) : undefined
							};
						});
				}
			}
		}} />

		{deleting && <>
			<ConfirmDialog variant="primary" isOpen={deleting} onClose={() => setDeleting(null)}
				confirmCallback={() => {
					onRemove(deleting);
					setDeleting(null);
				}}>
				{`Are you sure you want to remove the variant with SKU "${deleting.sku}"? This change isn't permanent until you save it.`}
			</ConfirmDialog>
		</>}
    </div>;
};

export default ValueEditor;
