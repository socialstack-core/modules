import Input from 'UI/Input';

export default class MultiSelect extends React.Component {
	
	constructor(props) {
		super(props);
		this.state = {
			selectedValue: null
		};
	}

	optionSelected(option) {
		this.state = {
			selectedValue: option
		};
	}
	
	render() {
		
		return <div className="livesupport-multiselect">
			{
				this.props.answers.map((text, i) => {
					var optionClass = this.state.selectedValue == text ? "selected" : "";
					var disabled = this.state.selectedValue != null ? "disabled" : null;
					{/* TODO: onClick not firing?  may be unnecessary if these options will be hidden after selection */}
					<Input key={i} className={optionClass} disabled={disabled} type="button" name="message" value={text} onClick={() => this.optionSelected(text)}>{text}</Input>
				})
			}
		</div>;
		
	}
	
}