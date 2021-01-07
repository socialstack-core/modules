import Input from 'UI/Input';

export default class MultiSelect extends React.Component {
	
	constructor(props) {
		super(props);
		this.state = {
			selectedValue: null
		};
	}

	optionSelected(option) {
		//console.log("option selected has fired");

		//this.setState({selectedValue: option});
	}
	
	render() {
		if(!this.props.answers) {
			return;
		}
		
		return <div className="livesupport-multiselect">
			<Input type="hidden" name="message" ref={r => this.messageRef = r} value={this.state.message} />
			{
				this.props.answers.map((text, i) => {
					var optionClass = "";
					//var optionClass = this.state.selectedValue == text ? "selected" : "";
					var disabled = this.state.selectedValue != null ? "disabled" : null;
					return <Input 
						disabled = {disabled} 
						className = {optionClass} 
						key={i} 
						type="submit"
						onClick={() => {
							this.setState({message: text});
							this.messageRef.value = text;
						}}
					>{text}</Input>
				})
			}
		</div>;
		
	}
	
}

MultiSelect.propTypes = {

}