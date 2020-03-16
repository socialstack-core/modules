
/**
 * Header for h1/h2/h3 etc.
 */
export default class Header extends React.Component {
	
	render() {
		var Mod = 'h' + (this.props.size || '1');
		return <Mod {...this.props}>{this.props.children}</Mod>;
	}
	
}

Header.propTypes={
	size: ['1','2','3','4','5','6'],
    name: 'string',
    idOfASpecificPieceOfContent: {
       type: 'id',
       contentType: 'Template'
    },
    items: {
       type: 'set',
       defaultRenderer: 'UI/Signpost'
    }
}

Header.icon='heading';