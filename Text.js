import omit from 'UI/Functions/Omit';

/**
 * This component displays translated text. Usage:
 * <Text>Hello world</Text>
 * <Text group='header'>Hello world</Text>
 * <Text group='header' key='hello_world'>Hello world</Text>
 */
export default class Text extends React.Component {
    constructor(props) {
        super(props);
    }
	
    render() {
		var text = <span 
			dangerouslySetInnerHTML={{__html: (this.props.text || this.props.children)}}
			{...omit(this.props, ['text', 'children', 'paragraph'])}
		/>;
		
		return this.props.paragraph ? <p>{text}</p> : text;
    }
}

Text.propTypes = {
	paragraph: 'boolean'
};
Text.icon = 'align-justify';

Text.rendererPropTypes = {
	text: 'string',
	paragraph: 'boolean'
};
