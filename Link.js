import omit from 'UI/Functions/Omit';

export default class Link extends React.Component {
    constructor(props) {
        super(props);
    }
	
    render() {
		var attribs = omit(this.props, ['text', 'url', 'children']);
		attribs.alt = attribs.alt || attribs.title;
		return <a href={this.props.url} 
			dangerouslySetInnerHTML={{__html: (this.props.text || this.props.children)}}
			{...attribs}
		/>;
    }
}

Link.propTypes = {
	text: 'string',
	title: 'string',
	url: 'string'
};
