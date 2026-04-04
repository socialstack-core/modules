import { useEffect, useState } from "react";
import { ProductQuantity } from "Api/Content";
import Button from "UI/Button";
import Image from "UI/Image";
import Input from "UI/Input";
import Icon from "UI/Icon";
import ConfirmDialog from "UI/Dialog/ConfirmDialog";
import Modal from 'UI/Modal';
import ProductLookup from "Admin/Payments/ProductLookup";

interface ChangeQuantitySelectorProps {
	maxQuantity: number;
	value: number;
	onChange: (quantity: number) => void;
}

const ChangeQuantitySelector: React.FC<ChangeQuantitySelectorProps> = ({ maxQuantity, value, onChange }) => {
	const options = Array.from({ length: maxQuantity }, (_, i) => i + 1);

	return (
		<Input type="select" 
			noWrapper
			className="form-control components-value-editor__entries-qtyselector" 
			value={value} 
			onChange={(e) => onChange(Number(e.target.value))}
		>

			{options.map(qty => (
				<option key={qty} value={qty}>
					{qty}
				</option>
			))}
		</Input>
	);
}

type ComponentEditorProps = {
	component: ProductQuantity,
	prefix: string,
	onChange: (component: ProductQuantity  ) => void
};

const ComponentEditor: React.FC<ComponentEditorProps> = (props) => {
	
	// NB this can't be a form as it's surrounded by the main admin form.
	const { component, prefix, onChange } = props;

	const updateProductQty = (value: number) => {
		const newComponent = { ...component };
		newComponent.quantity = value;
		newComponent.updateComponent = true;
		onChange && onChange(newComponent);
	};

	/** Use prefix if giving the inputs a name but do note that by doing so they will end up in any 
	 *  surrounding form fieldsets too, and using onChange makes them most likely unnecessary anyway
	 * */
	return 	<tr key={String(component.id)} className="components-value-editor__entries-item">
				<td>
					<Image size={32} fileRef={component.product?.featureRef} />
				</td>
				<td>{component.product?.sku}</td>
				<td>
					{component.product?.name}
				</td>
				<td>
					<ChangeQuantitySelector
						maxQuantity={50}
						value={component.quantity ?? 1}
						onChange={(qty: number) => {
							updateProductQty(qty);
						}}
					/>
				</td>
				<td>
					<div className="components-value-editor__entries-actions">
						<Button danger onClick={() => props.onRemove(component)}><Icon type='fa-trash' /></Button>
					</div>
				</td>
			</tr>
};

const ValueEditor: React.FC = (props : any) => {

	const { requiredAttributes } = props;
	const [inputHandle, setInputHandle] = useState(null);
	const [deleting, setDeleting] = useState<ProductQuantity | undefined>();

	const [showAddModal, setShowAddModal] = useState<boolean | undefined>(false);
	
	const [value, setValue] = useState(() => {
		var initVal = (props.value || props.defaultValue || []).filter(t => t!=null);
		return initVal;
	});
	
	const onRemove = (component: ProductQuantity) => {
		component.deleteComponent = true;
		setValue([...value]);
	};

	return <div className="components-value-editor">
		{props.label && !props.hideLabel && (
			<label className="form-label">
				{props.label}
			</label>
		)}
		
		<div className="components-value-editor">
			<table className="components-value-editor__entries">
				<tr>
					<th></th>
					<th>{`Sku`}</th>	
					<th>{`Name`}</th>
					<th>{`Quantity`}</th>
					<th>{`Action`}</th>
				</tr>
				{value.map((component, index) => !component || component.deleteComponent ? null : <ComponentEditor
					prefix={props.name + '_' + index}
					component={component}
					onRemove={() => setDeleting(component)}
					onChange={component => {
						const newValue = [...value];
						newValue[index] = component;
						setValue(newValue)
					}}
				/>)}
			</table>
		</div>

		<Button onClick={() => {setShowAddModal(true);}}>
			{`Add component`}
		</Button>

		{showAddModal &&
			<Modal
				title={`Add new component product`}
				visible
				isExtraLarge
				onClose={() => {
					setShowAddModal(undefined);
				}}
			>
				<ProductLookup  actionTitle={`Add`}  onSelected={(product) => {
					// Merge duplicate selections by incrementing the quantity
					setValue((currentValue: ProductQuantity[]) => {
						const existingIndex = currentValue.findIndex(component =>
							component?.productId === product.id && !component?.deleteComponent
						);

						if (existingIndex === -1) {
							return [...currentValue, { product, productId: product.id, quantity: 1 }];
						}

						const updatedValue = [...currentValue];
						const existingComponent = { ...updatedValue[existingIndex] } as ProductQuantity;
						const currentQty = existingComponent.quantity ?? 0;

						updatedValue[existingIndex] = {
							...existingComponent,
							quantity: currentQty + 1,
							updateComponent: true
						};

						return updatedValue;
					});
				}} />

			</Modal>
		}
		
		<input type="hidden" name={props.name} ref={ele => {
			setInputHandle(ele);

			if (ele != null) {
				ele.onGetValue = (v, input, e) => {
					if (input != inputHandle) {
						return v;
					}

					return value
						.filter((entry: ProductQuantity) => entry != undefined)
						.map((entry: ProductQuantity) => {

							// Specifically only pass the fields we want to update.
							// This helps minimise conflicts with other people editing the same product.
							return {
								id: entry.id,
								deleteComponent: entry.deleteComponent,
								updateComponent: entry.updateComponent,
								productId: entry.productId,
								quantity: entry.quantity
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
				<p>{`Are you sure you want to remove the component?`}</p>
				<p>{`This change isn't permanent until you save it.`}</p>
			</ConfirmDialog>
		</>}
    </div>;
};

export default ValueEditor;
