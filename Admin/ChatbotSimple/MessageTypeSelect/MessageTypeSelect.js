import Input from 'UI/Input';

export default class MessageTypeSelect extends React.Component {
	
	render(){
		return <Input {...this.props} type="select">
				<option value="0">User response is just free text</option>
				<option value="1">Form input - payload contains some sort of form field for the user to fill</option>
				<option value="2">Hand over to human agent</option>
				<option value="3">Validated response must be a full name</option>
				<option value="4">Validated response must be an email address</option>
				<option value="5">Validated response must be a phone number</option>
				<option value="6">When received, terminates chat.</option>
				<option value="7">When received, return control to bot.</option>
				<option value="8">Validated response must be a date</option>
				<option value="9">Response is meeting topic</option>
				<option value="10">Response is timezone id</option>
				<option value="11">Response is timezone string</option>
				<option value="12">Response is expert question offering area</option>
				<option value="13">Response is expert question initiative</option>
				<option value="14">Response is expert question question</option>
				<option value="15">Message is sent when operator is requested out of hours</option>
			</Input>;
	}
	
}
