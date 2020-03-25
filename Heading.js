
/**
 * For h1/h2/h3 etc.
 */
export default class Heading extends React.Component {
	
	render() {
		var Mod = 'h' + (this.props.size || '1');
		return <Mod {...this.props}>{this.props.children}</Mod>;
	}
	
}

Heading.propTypes={
	size: ['1','2','3','4','5','6'],
	children: true
}

Heading.icon='heading';