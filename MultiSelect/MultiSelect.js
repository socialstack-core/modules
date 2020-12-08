import Input from 'UI/Input';

export default class MultiSelect extends React.Component {
	
	constructor(props) {
		super(props);
		this.state = {
		};
	}
	
	render() {
		
		return <div className="livesupport-multiselect">
		{
			this.props.answers.map((text, i) => <Input key={i} type="submit" name="message" value={text}>{text}</Input>)
		}
		</div>;
		
	}
	
}