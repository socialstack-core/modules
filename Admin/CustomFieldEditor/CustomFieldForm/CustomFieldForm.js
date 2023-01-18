import Form from 'UI/Form';
import Input from 'UI/Input';
import Modal from 'UI/Modal';
import webRequest from 'UI/Functions/WebRequest';
import CustomFieldSelectForm from 'Admin/CustomFieldEditor/CustomFieldSelectForm';

const dataTypes = [
	{ value: "string", name: "Text" },
	{ value: "jsonstring", name: "Text Box" },
	{ value: "file", name: "Media" },
	{ value: "entity", name: "Link To Another Data Type" },
	{ value: "entitylist", name: "List Of Another Data Type" },
	{ value: "select", name: "Select" },
	{ value: "dateTime", name: "Date" },
	{ value: "double", name: "Number" },
	{ value: "bool", name: "Boolean" }
];

export default class CustomFieldForm extends React.Component {
	constructor(props) {
		super(props);

		this.state={
			field: props.field,
			dataType: props.field ? props.field.dataType : null,
			editMode: false,
			showConfirmDeleteModal: false
		};
	}

	deleteField() {
		webRequest("customContentTypeField/" + this.props.field.id, undefined, { method: "delete" }).then(() => {
			this.setState({showConfirmDeleteModal: false});
		}).catch(e => {
			console.error(e);
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

		var allowEdit = createMode || this.state.editMode;
		
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
					this.setState({ field: response, editMode: false, showConfirmDeleteModal: false });
				}}
			>
				<Input name={"nickName"} label={"Name"} defaultValue={name} disabled={!allowEdit} validate={allowEdit ? ['Required'] : null} />

				<Input label="Data Type" type="select" name="dataType" value={this.state.dataType} disabled={!createMode} validate={createMode ? ['Required'] : null} onChange={
								e => {
									this.setState({ dataType: e.target.value });
								}
							}>
								{dataTypes.map(dataType => 
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

				{(allowEdit && this.state.field && this.state.dataType === "select") &&
					<CustomFieldSelectForm
						fieldId={this.state.field.id}
					/>
				}

				<Input 
					label="Localised"
					name="localised"
					type="checkbox"
					defaultValue={this.props.field ? this.props.field.localised : null}
					disabled={!allowEdit} validate={allowEdit ? ['Required'] : null}
				/>

				{allowEdit &&
					<div>
						<Input type="submit" />
						{this.state.editMode && 
							<button className="cancelEditButton" onClick={e => { e.preventDefault(); this.setState({ editMode: false }); }}>Cancel Edit</button>
						}	
					</div>
				}

				{!this.state.editMode && !createMode &&
					<div>
						<button className="editButton" onClick={e => { e.preventDefault(); this.setState({ editMode: true }); }}>Edit Field</button>
						<button className="deleteButton" onClick={e => { e.preventDefault(); this.setState({ showConfirmDeleteModal: true }); }}>Delete Field</button>
					</div>
				}

				{this.state.showConfirmDeleteModal &&
					<Modal
						visible
						className={"delete-confirmation-modal"}
						title="Are you sure?"
						onClose={e => { this.setState({ showConfirmDeleteModal: false }); }}
					>
						<button className="cancelButton" onClick={e => { e.preventDefault(); this.setState({ showConfirmDeleteModal: false }); }}>Cancel</button>
						<button className="confirmButton" onClick={e => { e.preventDefault(); this.deleteField(); }}>Confirm</button>
					</Modal>
				}

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