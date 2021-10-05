import omit from 'UI/Functions/Omit';

/**
 * This component displays translated text. Usage:
 * <Text>Hello world</Text>
 * <Text group='header'>Hello world</Text>
 * <Text group='header' key='hello_world'>Hello world</Text>
 */
export default function Text (props) {
	var className = this.props.className || undefined;
	var Tag = props.paragraph ? "p" : "span";

	return <Tag className={className}
				dangerouslySetInnerHTML={{__html: (props.text || props.children)}}
				{...omit(props, ['text', 'children', 'paragraph'])}>
		</Tag>;
}

Text.propTypes = {
	paragraph: 'boolean'
};

Text.icon = 'align-justify';

Text.rendererPropTypes = {
	text: 'string',
	paragraph: 'boolean'
};
