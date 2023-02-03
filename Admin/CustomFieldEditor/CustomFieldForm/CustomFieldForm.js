import Form from 'UI/Form';
import Input from 'UI/Input';
import CustomFieldSelectForm from 'Admin/CustomFieldEditor/CustomFieldSelectForm';

export default class CustomFieldForm extends React.Component {
	constructor(props) {
		super(props);

		this.state={
			field: props.field,
			dataType: props.field ? props.field.dataType : null
		};

		this.dataTypes = [
			{ value: "string", name: "Text" },
			{ value: "textarea", name: "Text Area" },
			{ value: "jsonstring", name: "Styleable Text Block" },
			{ value: "file", name: "Media" },
			{ value: "select", name: "Select" },
			{ value: "dateTime", name: "Date" },
			{ value: "double", name: "Number" },
			{ value: "bool", name: "Boolean" }
		];

		if (!props.isFormField) {
			this.dataTypes = [...this.dataTypes, { value: "entity", name: "Link To Another Data Type" }, { value: "entitylist", name: "List Of Another Data Type" }, ];
		}
	}

	render() {
		var action = "customContentTypeField";
		var createMode = true;

		if (this.props.field) {
			action += "/" + this.props.field.id;
			createMode = false;
		}

		var name;

		if (this.props.field) {
			if (this.props.field.nickName) {
				name = this.props.field.nickName;
			} else if (this.props.field.name) {
				name = this.props.field.name;
			}
		}

		return <div className="custom-field-form">
			<Form
				action={action}
				onValues={values => {
					values.customContentTypeId = this.props.customContentTypeId;
					return values;
				}}
				onSuccess={response => {
					if (this.props.onCreate && !this.props.field) {
						this.props.onCreate(response);
					} else if (this.props.onUpdate) {
						this.props.onUpdate(response);
					}
					this.setState({ field: response });

					if (this.props.onCancel) {
						this.props.onCancel();
					}

				}}
			>
				<Input name={"nickName"} label={"Name"} defaultValue={name} validate={['Required']} />

				<Input name="dataType" label="Data Type" type="select" value={this.state.dataType} disabled={!createMode} validate={createMode ? ['Required'] : null}
					onChange={(e) => {
						this.setState({ dataType: e.target.value });
					}}>
					{this.dataTypes.map(dataType =>
						<option value={dataType.value} selected={dataType.value == this.state.dataType}>
							{dataType.name}
						</option>
					)}
				</Input>

				{(this.state.dataType === "entity" || this.state.dataType === "entitylist") &&
					<Input 
						label="Linked Data Type"
						name="linkedEntity"
						type="select"
						contentType="customcontenttype"
						displayField="nickName"
						contentTypeValue="name"
						filter={{ where: { deleted: "false" } }}
						defaultValue={this.props.field ? this.props.field.linkedEntity : null}
						disabled={!createMode} validate={createMode ? ['Required'] : null}
					/>
				}

				{(this.state.field && this.state.dataType === "select") &&
					<CustomFieldSelectForm fieldId={this.state.field.id} />
				}

				{!this.props.isFormField &&
					<Input 
						label="Localised"
						name="localised"
						type="checkbox"
						defaultValue={this.props.field ? this.props.field.localised : null}
						validate={['Required']}
					/>
				}

				<footer className="custom-field-editor__modal-footer">
					<button type="button" className="btn btn-outline-primary cancelButton" onClick={() => {
						if (this.props.onCancel) {
							this.props.onCancel();
                        }
					}}>
						{`Cancel`}
					</button>
					<Input type="submit" noWrapper />
				</footer>

			</Form>
		</div>;
		
	}
	
}

/*
// propTypes are used to describe configuration on your component in the editor.
// Just setting it to an empty object will make your component appear as something that can be added.
// Define your available props like the examples below.

CustomFieldForm.propTypes = {
	
	title: 'string', // text input
	size: [1,2,3,4], // dropdowns
	
	// All <Input type='x' /> values are supported - checkbox, color etc.
	// Also the special id type which can be used to select some other piece of content (by customFieldForm name), like this:
	templateToUse: {type: 'id', content: 'Template'}
	
};

CustomFieldForm.icon='align-center'; // fontawesome icon
*/