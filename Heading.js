import omit from 'UI/Functions/Omit';

/**
 * For h1/h2/h3 etc.
 */
export default class Heading extends React.Component {
	
	render() {
		var Mod = 'h' + (this.props.size || '1');
		
		var className='heading ' + (this.props.className || '');
		
		return <Mod {...omit(this.props, ['children', 'className'])}>{this.props.children}</Mod>;
	}
	
}

Heading.propTypes={
	size: ['1','2','3','4','5','6'],
	children: true
}

Heading.icon='heading';