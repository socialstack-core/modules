import { useEffect, useState } from "react";
import { ProductAttribute } from "Api/ProductAttribute";
import { Product } from "Api/Product";
import Input from "UI/Input";
import Button from "UI/Button";
import Icon from "UI/Icon";
import Link from "UI/Link";
import Col from "UI/Column";
import { FileSelectEvent } from "UI/FileSelector";
import Row from "UI/Row";
import AttributeSelect from "Admin/Payments/AttributeSelect";
import ConfirmDialog from "UI/Dialog/ConfirmDialog";

type VariantEditorProps = {
	variant: Product,
	prefix: string,
	onChange: (variant: Product) => void,
	onRemove?: (variant: Product) => void,
	requiredAttributes: ProductAttribute[] | undefined
};

interface ExtendedProduct extends Product {
	/* True if this product variant is marked for deletion */
	deleteVariant?: boolean
};

const VariantEditor: React.FC<VariantEditorProps> = (props) => {
	
	// NB this can't be a form as it's surrounded by the main admin form.
	const { variant, requiredAttributes, prefix, onChange } = props;
	const { additionalAttributes } = variant;

	const updateField = (name: string, newValue: any) => {
		const newVariant : Product = { ...variant };
		// @ts-ignore
		newVariant[name] = newValue;
		onChange && onChange(newVariant);
	};

	/** Use prefix if giving the inputs a name but do note that by doing so they will end up in any 
	 *  surrounding form fieldsets too, and using onChange makes them most likely unnecessary anyway
	 * */
	return <div className="variants-variant-editor">
		<Row>
			<Col>
				<Input type='image' value={variant.featureRef || ''} onChange={(e: any) => updateField('featureRef', (e as FileSelectEvent).target.value)} />
			</Col>
			<Col>
				<Input type='text' label={`Name`} required validate={['Required']} value={variant.name || ''} onChange={(e: React.ChangeEvent<Element>) => updateField('name', (e.target as HTMLInputElement).value)} />
				<Input type='text' defaultValue={variant.sku || ''} label={`Sku`} onChange={(e: React.ChangeEvent<Element>) => updateField('sku', (e.target as HTMLInputElement).value)} />
				<AttributeSelect
					value={additionalAttributes}
					requiredAttributes={requiredAttributes}
					label={`Attributes`}
					onChange={e => updateField('additionalAttributes', e.fullValue)}
				/>
				<div className="variants-variant-editor__actions">
					{variant.id && <Link target='_blank' href={'/en-admin/product/' + variant.id}>{`Edit more details`}</Link>}
					<Button variant="danger" onClick={() => props.onRemove && props.onRemove(variant)}><Icon type='fa-trash' /></Button>
				</div>
			</Col>
		</Row>
	</div>;
	
};

const ValueEditor: React.FC = (props : any) => {

	const { requiredAttributes } = props;
	const [inputHandle, setInputHandle] = useState<HTMLInputElement | null>(null);
	const [deleting, setDeleting] = useState<ExtendedProduct | null>(null);
	
	const [value, setValue] = useState<ExtendedProduct[]>(() => {
		var initVal = (props.value || props.defaultValue || []).filter((t:any) => t!=null);
		return initVal as ExtendedProduct[];
	});
	
	const onRemove = (variant: ExtendedProduct) => {
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
			setValue([...value, {} as Product]);
		}}>{`Add variant`}</Button>
		
		<input type="hidden" name={props.name} ref={ele => {
			setInputHandle(ele);

			if (ele != null) {
				// @ts-ignore
				ele.onGetValue = (v, input, e) => {
					if (input != inputHandle) {
						return v;
					}

					return value
						.filter((entry) => entry != undefined)
						.map((entry) => {
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
			<ConfirmDialog variant="primary" isOpen={!!deleting} onClose={() => setDeleting(null)}
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
