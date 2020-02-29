
/**
 * This component will substitute itself with something known by the canvas by calling onSubstitute on the canvas itself.
 */
export default class Substitute extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		var canvas = this.props.__canvas;
		return canvas && canvas.props.onSubstitute && canvas.props.onSubstitute(this.props.name);
	}
	
}

Substitute.propTypes={
	name: 'string'
}