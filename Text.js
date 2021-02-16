import omit from 'UI/Functions/Omit';

/**
 * This component displays translated text. Usage:
 * <Text>Hello world</Text>
 * <Text group='header'>Hello world</Text>
 * <Text group='header' key='hello_world'>Hello world</Text>
 */
export default function Text (props) {
	var text = <span 
		dangerouslySetInnerHTML={{__html: (props.text || props.children)}}
		{...omit(props, ['text', 'children', 'paragraph'])}
	/>;
	
	return props.paragraph ? <p>{text}</p> : text;
}

Text.propTypes = {
	paragraph: 'boolean'
};
Text.icon = 'align-justify';

Text.rendererPropTypes = {
	text: 'string',
	paragraph: 'boolean'
};
