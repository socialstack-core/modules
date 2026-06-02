import Form from 'UI/Form';
import Button from 'UI/Button';
import Input from 'UI/Input';
import CustomFieldSelectForm from 'Admin/CustomFieldEditor/CustomFieldSelectForm';
import customContentTypeApi from 'Api/CustomContentType';
import customContentTypeFieldApi, { CustomContentTypeField } from 'Api/CustomContentTypeField';
import { useState, useEffect } from 'react';

interface CustomFieldFormProps {
	field?: any;
	customContentTypeId: int;
	isFormField: boolean;
	onCreate?: (response: any) => void;
	onUpdate?: (response: any) => void;
	onCancel?: () => void;
}

const CustomFieldForm: React.FC<CustomFieldFormProps> = (props) => {
	const [field, setField] = useState<any>(props.field);
	const [dataType, setDataType] = useState<string | null>(props.field ? props.field.dataType : null);
	const [types, setTypes] = useState<any[] | null>(null);

	const dataTypes = [
		{ value: "string", name: `Text` },
		{ value: "tel", name: `Text (Telephone number)` },
		{ value: "email", name: `Text (E-Mail address)` },
		{ value: "url", name: `Text (Web address)` },
		{ value: "textarea", name: `Text (Multiline)` },
		{ value: "jsonstring", name: `Text (Styleable multiline)` },
		{ value: "file", name: `Media` },
		{ value: "select", name: `Select` },
		{ value: "dateTime", name: `Date` },
		{ value: "double", name: `Number` },
		{ value: "bool", name: `Boolean` }
	];

	if (!props.isFormField) {
		dataTypes.push(
			{ value: "entity", name: `Another Data Type` },
			{ value: "entitylist", name: `List of Another Data Type` },
			{ value: "entitylink", name: `Link to Another Page Data Type` }
		);
	}

	useEffect(() => {
		customContentTypeApi.getAllTypesPlus().then((types: any) => {
			setTypes(types);
		});
	}, []);

	var action = "customContentTypeField";
	var createMode = true;

	if (props.field) {
		action += "/" + props.field.id;
		createMode = false;
	}

	var name;

	if (props.field) {
		if (props.field.nickName) {
			name = props.field.nickName;
		} else if (props.field.name) {
			name = props.field.name;
		}
	}

	interface FieldFormValues {
		isRequired: boolean,
		isEmail: boolean,
		dataType: string,
		customContentTypeId: int,
		validation?: string
	}

	return <div className="custom-field-form">
		<Form
			action={props.field ? (field: Partial<CustomContentTypeField>) => customContentTypeFieldApi.update(props.field.id, field) : customContentTypeFieldApi.create}
			onValues={(values : FieldFormValues) => {
				var validation: string[] = [];
				values.customContentTypeId = props.customContentTypeId;

				if (values.isRequired) {
					validation.push("Required");
				}

				if (values.isEmail || values.dataType == "email") {
					validation.push("EmailAddress");
				}

				if (validation && validation.length > 0) {
					values.validation = validation.join(',');
				}

				return values;
			}}
			onSuccess={(response: any) => {
				if (props.onCreate && !props.field) {
					props.onCreate(response);
				} else if (props.onUpdate) {
					props.onUpdate(response);
				}
				setField(response);

				if (props.onCancel) {
					props.onCancel();
				}

			}}
		>
			<Input type="text" name={"nickName"} label={"Name"} defaultValue={name} validate={['Required']} />

			<Input name="dataType" label="Data Type" type="select" value={dataType || undefined} disabled={!createMode} validate={createMode ? ['Required'] : undefined}
				onChange={(e: React.ChangeEvent<Element>) => {
					setDataType((e.target as HTMLSelectElement).value);
				}}>
				{dataTypes.map((dataTypeOption: { value: string; name: string }) =>
					<option value={dataTypeOption.value} selected={dataTypeOption.value == dataType}>
						{dataTypeOption.name}
					</option>
				)}
			</Input>

			{(dataType === "entity" || dataType === "entitylist") &&
				<Input
					label="Linked Data Type"
					name="linkedEntity"
					type="select"
					defaultValue={props.field ? props.field.linkedEntity : null}
					disabled={!createMode} validate={createMode ? ['Required'] : undefined}
				>
					{types && types.map((type: any) =>
						<option value={type.value}>
							{type.name}
						</option>
					)}
				</Input>
			}

			{(field && dataType === "select") &&
				<CustomFieldSelectForm fieldId={field.id} />
			}

			{dataType === "select" &&
				<Input
					label="Are the options prices?"
					name="optionsArePrices"
					type="checkbox"
					defaultChecked={!!(props.field && props.field.optionsArePrices)}
				/>
			}

			{dataType === "dateTime" &&
				<>
					<Input
						label="Should seconds be hidden?"
						name="hideSeconds"
						type="checkbox"
						defaultChecked={!!(props.field && props.field.hideSeconds)}
					/>

					<Input
						label="Should minutes be rounded (5 mins) ?"
						name="roundMinutes"
						type="checkbox"
						defaultChecked={!!(props.field && props.field.roundMinutes)}
					/>
				</>
			}

			{!props.isFormField
				?
				dataType != "entitylink" &&
				<div className="attributes">
					<Input
						label="Localised"
						name="localised"
						type="checkbox"
						defaultChecked={!!(props.field && props.field.localised)}
						validate={['Required']}
					/>
					{dataType === "string" &&
						<Input
							label="Should this text be url encoded?"
							name="urlEncoded"
							type="checkbox"
							defaultChecked={!!(props.field && props.field.urlEncoded)}
						/>
					}
				</div>
				:
				<div className="form-specific-vlaues">
					<div className="validation">
						<Input
							label="Is this field required?"
							name="isRequired"
							type="checkbox"
							defaultChecked={!!(props.field && props.field.validation && props.field.validation.includes("Required"))}
						/>
						<Input
							label="Is this field hidden?"
							name="isHidden"
							type="checkbox"
							defaultChecked={!!(props.field && props.field.isHidden)}
						/>
					</div>
				</div>
			}

			<Input name={"order"} label={"Order"} type="number" defaultValue={props.field ? props.field.order : 0} />

			<footer className="custom-field-editor__modal-footer">
				<Button outlined className="cancelButton" onClick={() => {
					if (props.onCancel) {
						props.onCancel();
					}
				}}>
					{`Cancel`}
				</Button>
				<Input type="submit" noWrapper />
			</footer>

		</Form>
	</div>;
};

export default CustomFieldForm;
