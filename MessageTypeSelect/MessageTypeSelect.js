import Input from 'UI/Input';

export default class MessageTypeSelect extends React.Component {
	
	render(){
		return <Input {...this.props} type="select">
				<option value="0">User response is just free text</option>
				<option value="1">Form input - payload contains some sort of form field for the user to fill</option>
				<option value="2">Hand over to human agent</option>
			</Input>;
	}
	
}
