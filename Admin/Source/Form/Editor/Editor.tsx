import Input from 'UI/Input';
import Button from 'UI/Button';
import ContentListEditor, { ContentListEditorNode, ContentDialogMode } from 'Admin/ContentListEditor';

export type OptionsType = 'dropdown' | 'radio' | 'checkbox' | undefined;

interface FormFieldOption {
	label: string;
}

interface FormFieldData {
	name: string;
	label: string;
	validation?: string;
	optionsType?: OptionsType;
	optionsArePrices?: boolean;
	required?: boolean;
}

interface FormField extends ContentListEditorNode {
	inputType: string;
	data: FormFieldData;
	options?: FormFieldOption[];
}

interface EditorProps {
	name?: string;
	value?: string;
	defaultValue?: string;
	label?: string;
	hideLabel?: boolean;
	readonly?: boolean;
}

const INPUT_TYPES = [
	{ value: 'text', label: `Text` },
	{ value: 'textarea', label: `Textarea` },
	{ value: 'tel', label: `Phone Number` },
	{ value: 'email', label: `Email Address` },
	// now covered under "multiple" option
	//{ value: 'dropdown', label: `Dropdown` },
	{ value: 'multiple', label: `Multiple choice` },
	{ value: 'checkbox', label: `Checkbox` },
	{ value: 'number', label: `Number` },
	{ value: 'hidden', label: `Hidden` },
];

const generateId = (): string => {
	return Math.random().toString(36).substring(2, 11);
};

const parseItems = (value: string): FormField[] => {
	var parsed = JSON.parse(value);
	return (parsed.fields || []).map((f: any) => ({
		...f,
		id: generateId()
	}));
};

const serializeItems = (items: FormField[]): string => {
	return JSON.stringify({
		fields: items.map(({ id, ...rest }) => rest)
	});
};

const createItem = (): FormField => {
	return {
		id: generateId(),
		inputType: 'text',
		data: {
			name: '',
			label: ''
		}
	};
};

const beforeSave = (draft: FormField): FormField => {
	const validationParts: string[] = [];
	const required = draft.data.required !== undefined
		? draft.data.required
		: (draft.data.validation || '').split(',').includes('Required');
	if (required) {
		validationParts.push('Required');
	}
	if (draft.inputType === 'email') {
		validationParts.push('EmailAddress');
	}
	if (draft.inputType === 'tel') {
		validationParts.push('PhoneNumber');
	}
	const data: FormFieldData = {
		name: draft.data.name,
		label: draft.data.label,
		...(validationParts.length > 0 ? { validation: validationParts.join(',') } : {})
	};
	if (draft.inputType === 'dropdown') {
		if (draft.data.optionsArePrices) {
			data.optionsArePrices = true;
		}
	} else if (draft.inputType === 'multiple') {
		data.optionsType = draft.data.optionsType || 'dropdown';
		if (draft.data.optionsArePrices) {
			data.optionsArePrices = true;
		}
	}
	return {
		id: draft.id,
		inputType: draft.inputType,
		data,
		...(draft.inputType === 'dropdown' || draft.inputType === 'multiple'
			? { options: draft.options || [] }
			: {})
	};
};

const inputTypeLabelFor = (item: FormField): string => {
	return INPUT_TYPES.find(t => t.value === item.inputType)?.label || item.inputType;
};

const renderTitle = (item: FormField, dragHandle: React.ReactNode) => (
	<div className="form-fields-item-title">
		{dragHandle}
		<span className="form-fields-item-label">
			{item.data.label || `Untitled`}
		</span>
		<span className="form-fields-item-type">
			<i className="fal fa-fw fa-input"></i> {inputTypeLabelFor(item)}
		</span>
		{item.data.name && (
			<span className="form-fields-item-name">
				<code>{item.data.name}</code>
			</span>
		)}
	</div>
);

const renderDialogForm = (item: FormField, onChange: (updates: Partial<FormField>) => void) => {
	const required = item.data.required !== undefined
		? item.data.required
		: (item.data.validation || '').split(',').includes('Required');

	return (
		<div className="form-fields-dialog-form">
			<Input
				type="select"
				label={`Input Type`}
				value={item.inputType}
				onChange={(e) => onChange({ inputType: (e.target as HTMLSelectElement).value })}
			>
				{INPUT_TYPES.map(t => (
					<option key={t.value} value={t.value}>{t.label}</option>
				))}
			</Input>

			<Input
				type="text"
				label={`Field Name`}
				value={item.data.name}
				onChange={(e) => onChange({ data: { ...item.data, name: (e.target as HTMLInputElement).value } })}
				placeholder="fieldName"
			/>

			<Input
				type="text"
				label={`Label`}
				value={item.data.label}
				onChange={(e) => onChange({ data: { ...item.data, label: (e.target as HTMLInputElement).value } })}
				placeholder="Field Label"
			/>

			{item.inputType !== 'checkbox' && (
				<Input
					type="checkbox"
					label={`Required`}
					checked={required}
					onChange={e => onChange({ data: { ...item.data, required: (e.target as HTMLInputElement).checked } })}
				/>
			)}

			{(item.inputType === 'dropdown' || item.inputType === 'multiple') && (
				<div className="form-fields-options-section">
					<label className="form-label">{`Options`}</label>
					{(item.options || []).map((opt, index) => (
						<div key={index} className="form-fields-option-row">
							<div className="input-group mb-3">
								<Input
									type="text" noWrapper
									value={opt.label}
									onChange={(e) => onChange({
										options: (item.options || []).map((o, i) => i === index ? { label: (e.target as HTMLInputElement).value } : o)
									})}
									placeholder="Option label"
								/>
								<Button
									sm
									outlined
									variant="danger"
									onClick={() => onChange({
										options: (item.options || []).filter((_, i) => i !== index)
									})}
								>
									<i className="fal fa-fw fa-times"></i>
									<span className="sr-only">
										{`Remove option`}
									</span>
								</Button>
							</div>
						</div>
					))}
					<Button
						sm
						outlined
						onClick={() => onChange({ options: [...(item.options || []), { label: '' }] })}
						className="form-fields-add-option"
					>
						<i className="fal fa-fw fa-plus"></i> {`Add option`}
					</Button>

					{item.inputType === 'multiple' && (
						<Input
							type="select"
							label={`Options Type`}
							value={item.data.optionsType || 'dropdown'}
							onChange={(e) => onChange({ data: { ...item.data, optionsType: (e.target as HTMLSelectElement).value as OptionsType } })}
						>
							<option key="dropdown" value="dropdown">
								{`Dropdown list`}
							</option>
							<option key="radio" value="radio">
								{`Select one (radio buttons)`}
							</option>
							<option key="checkbox" value="checkbox">
								{`Select multiple (checkboxes)`}
							</option>
						</Input>
					)}

					<Input
						type="checkbox"
						label={`Options are prices`}
						checked={!!item.data.optionsArePrices}
						onChange={e => onChange({ data: { ...item.data, optionsArePrices: (e.target as HTMLInputElement).checked } })}
					/>
				</div>
			)}
		</div>
	);
};

const dialogTitleFor = (item: FormField, mode: ContentDialogMode): string => {
	if (mode === 'edit') {
		return item.data.label ? `Edit ${item.data.label}` : `Edit field`;
	}
	return `Add field`;
};

const Editor: React.FC<EditorProps> = (props) => {
	return (
		<ContentListEditor<FormField>
			{...props}
			className="admin-form-fields-editor"
			nestable={false}
			addLabel={`Add field`}
			emptyText={`No form fields. Click "Add field" to create one.`}
			parseItems={parseItems}
			serializeItems={serializeItems}
			createItem={createItem}
			beforeSave={beforeSave}
			renderTitle={renderTitle}
			renderDialogForm={renderDialogForm}
			dialogTitleFor={dialogTitleFor}
		/>
	);
};

export default Editor;
