import Loop from 'UI/Loop';
import CustomFieldForm from 'Admin/CustomFieldEditor/CustomFieldForm';
import Spacer from 'UI/Spacer';

export default class CustomFieldEditor extends React.Component {
	constructor(props) {
		super(props);

		this.state = {
			showCreateFieldForm: false
		}
	}

	onClickPlus(e) {
		this.setState({showCreateFieldForm: true});
	}

	onFieldCreate() {
		this.setState({showCreateFieldForm: false});
	}

	render(){
		
		if (!this.props.currentContent || this.props.currentContent.type !== "CustomContentType") {
			console.log("CustomFieldEditor only supports the CustomContentType module");
			return null;
		}

		return <div className="custom-field-editor">

			<label class="form-label">Fields</label>

			<div className="custom-field-editor-content">
				<Loop 
					over='customContentTypeField' 
					filter={
						{
							where: {
								customContentTypeId: this.props.currentContent.id,
								deleted: false
							}
						}
					}
					live
				>
					{
						field => {
								return <CustomFieldForm key={field.id} field={field} customContentTypeId={this.props.currentContent.id} />
						}
					}
				</Loop>

				<Spacer />

				<button onClick={e => {
					e.preventDefault();
					this.onClickPlus(e);
				}}>
					Add New Field
				</button>

				{this.state.showCreateFieldForm &&
					<div className="custom-field-editor-new-field">
							<CustomFieldForm customContentTypeId={this.props.currentContent.id} onCreate={() => this.onFieldCreate()} />
					</div>
				}
			</div>
		</div>;
		
	}
	
}

/*
// propTypes are used to describe configuration on your component in the editor.
// Just setting it to an empty object will make your component appear as something that can be added.
// Define your available props like the examples below.

CustomFieldEditor.propTypes = {
	
	title: 'string', // text input
	size: [1,2,3,4], // dropdowns
	
	// All <Input type='x' /> values are supported - checkbox, color etc.
	// Also the special id type which can be used to select some other piece of content (by listEditor name), like this:
	templateToUse: {type: 'id', content: 'Template'}
	
};

CustomFieldEditor.icon='align-center'; // fontawesome icon
*/