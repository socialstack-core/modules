import Input from 'UI/Input';

export default class MultiSelect extends React.Component {
	
	constructor(props) {
		super(props);
		this.state = {
			selectedValue: null
		};
	}

	optionSelected(option) {
		console.log("option selected has fired");

		this.state = {
			selectedValue: option
		};
	}
	
	render() {
		console.log(this.props);
		if(!this.props.answers) {
			return;
		}


		return <div className="livesupport-multiselect">
			{
				this.props.answers.map((text, i) => {
					var optionClass = this.state.selectedValue == text ? "selected" : "";
					var disabled = this.state.selectedValue != null ? "disabled" : null;
					{/* TODO: onClick not firing?  may be unnecessary if these options will be hidden after selection */}
					return <Input disabled = {disabled} className = {optionClass} key={i} type="submit" name="message" value={text}>{text}</Input>
				})
			}
		</div>;
		
	}
	
}

MultiSelect.propTypes = {

}