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
		return <span 
			dangerouslySetInnerHTML={{__html: (this.props.text || this.props.children)}}
			{...omit(this.props, ['text', 'children'])}
		/>;
    }
}

Text.propTypes = {};
Text.icon = 'align-justify';

Text.rendererPropTypes = {
	text: 'string'
};
