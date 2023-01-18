import Form from 'UI/Form';
import Input from 'UI/Input';
import Modal from 'UI/Modal';
import webRequest from 'UI/Functions/WebRequest';
import Loop from 'UI/Loop';
import Alert from 'UI/Alert';
import Spacer from 'UI/Spacer';

export default class CustomFieldSelectForm extends React.Component {
	constructor(props) {
		super(props);
		this.newOptionInputRef = React.createRef();
	}

	deleteOption(id) {
		webRequest("customContentTypeSelectOption/" + id, undefined, { method: "delete" }).then(() => {
			this.setState({showConfirmDeleteModal: false});
		}).catch(e => {
			console.error(e);
		});
	}

	render() {
		return <div className="custom-field-select-form">
				<Loop 
					over='customContentTypeSelectOption' 
					live
					filter={
						{
							where: {
								customContentTypeFieldId: this.props.fieldId
							}
						}
					}
				>
					{
						option => {
								return <div className="custom-field-select-form--option">
									<span className="option-value">{option.value}</span>
									<button className="option-delete" onClick={e => { e.preventDefault(); this.deleteOption(option.id); }}>
										<icon className="fa fa-trash" />
									</button>
								</div>
						}
					}
				</Loop>
				<Form
					action={"customContentTypeSelectOption"}
					onValues={values => {
						values.customContentTypeFieldId = this.props.fieldId;
						return values;
					}}
					onSuccess={response => {
						if (this.newOptionInputRef && this.newOptionInputRef.current && this.newOptionInputRef.current.inputRef) {
							this.newOptionInputRef.current.inputRef.value = "";
						}
					}}
				>
					<Input name={"value"} label={"New Option"} validate={['Required']} ref={this.newOptionInputRef}/>
					<Input type="submit" label="Create Option" />
				</Form>
		</div>;
		
	}
	
}