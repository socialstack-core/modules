import Form from 'UI/Form';
import Input from 'UI/Input';
import CustomFieldSelectForm from 'Admin/CustomFieldEditor/CustomFieldSelectForm';
import webRequest from 'UI/Functions/WebRequest';

export default class CustomFieldForm extends React.Component {
	constructor(props) {
		super(props);

		this.state={
			field: props.field,
			dataType: props.field ? props.field.dataType : null,
			types: null
		};

		this.dataTypes = [
			{ value: "string", name: "Text" },
			{ value: "textarea", name: "Text Area" },
			{ value: "jsonstring", name: "Styleable Text Block" },
			{ value: "file", name: "Media" },
			{ value: "select", name: "Select" },
			{ value: "dateTime", name: "Date" },
			{ value: "double", name: "Number" },
			{ value: "price", name: "Price" },
			{ value: "bool", name: "Boolean" }
		];

		if (!props.isFormField) {
			this.dataTypes = [
				...this.dataTypes, 
				{ value: "entity", name: "Another Data Type" }, 
				{ value: "entitylist", name: "List Of Another Data Type" },
				{ value: "entitylink", name: "Link To Another Page Data Type" }
			];
		}
	}

	componentDidMount() {
		webRequest("customContentType/allcustomtypesplus").then(resp => {
			var types = resp.json;
			this.setState({ types: types });
		});
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
					var validation = [];
					values.customContentTypeId = this.props.customContentTypeId;
					
					if (values.isRequired) {
						validation.push("Required");
					}

					if (values.isEmail) {
						validation.push("EmailAddress");
					}

					if (validation && validation.length > 0) {
						values.validation = validation.join(',');
					}

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
						defaultValue={this.props.field ? this.props.field.linkedEntity : null}
						disabled={!createMode} validate={createMode ? ['Required'] : null}
					>
						{this.state.types && this.state.types.map(type =>
							<option value={type.value}>
								{type.name}
							</option>
					)}
					</Input>
				}

				{(this.state.field && this.state.dataType === "select") &&
					<CustomFieldSelectForm fieldId={this.state.field.id} />
				}

				{this.state.dataType === "select" &&
					<Input 
						label="Are the options prices?"
						name="optionsArePrices"
						type="checkbox"
						defaultValue={this.props.field && this.props.field.optionsArePrices ? true : null}
					/>
				}

				{!this.props.isFormField
					?
						this.state.dataType != "entitylink" &&
							<div className="attributes">
								<Input 
									label="Localised"
									name="localised"
									type="checkbox"
									defaultValue={this.props.field ? this.props.field.localised : null}
									validate={['Required']}
								/>
								{this.state.dataType === "string" &&
									<Input 
										label="Should this text be url encoded?"
										name="urlEncoded"
										type="checkbox"
										defaultValue={this.props.field && this.props.field.urlEncoded ? true : null}
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
									defaultValue={this.props.field && this.props.field.validation && this.props.field.validation.includes("Required") ? true : null}
								/>
								<Input 
									label="Is this field hidden?"
									name="isHidden"
									type="checkbox"
									defaultValue={this.props.field && this.props.field.isHidden ? true : null}
								/>
								{(this.state.dataType === "string" || this.state.dataType === "textarea") &&
									<Input 
										label="Is this field an email address?"
										name="isEmail"
										type="checkbox"
										defaultValue={this.props.field && this.props.field.validation && this.props.field.validation.includes("EmailAddress") ? true : null}
									/>
								}
								<Input 
									label="Peak 15 Identifier"
									name="peak15FieldIdentifier"
									defaultValue={this.props.field && this.props.field.peak15FieldIdentifier ? this.props.field.peak15FieldIdentifier : null}
									placeholder="Leave blank if this form does not intergrate with peak 15"
								/>
							</div>
						</div>
				}

				<Input name={"order"} label={"Order"} type="number" defaultValue={this.props.field ? this.props.field.order : 0} />

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