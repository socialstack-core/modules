
/**
 * For h1/h2/h3 etc.
 */
export default class Heading extends React.Component {
	
	render() {
		var Mod = 'h' + (this.props.size || '1');
		return <Mod {...this.props}>{this.props.children}</Mod>;
	}
	
}