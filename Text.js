import omit from 'UI/Functions/Omit';

/**
 * This component displays translated text. Usage:
 * <Text>Hello world</Text>
 * <Text group='header'>Hello world</Text>
 * <Text group='header' key='hello_world'>Hello world</Text>
 */
export default function Text (props) {
	const { paragraph, bold, animate } = props;

	var className = props.className || undefined;
	var animation = animate ? "fade-up" : undefined;
	var Tag = paragraph ? "p" : "span";

	if (bold && !paragraph) {
		Tag = "strong";
	}

	if (bold && paragraph) {
		return <p className={className} data-aos={animation}
				{...omit(props, ['text', 'children', 'paragraph', 'bold', 'animate'])}>
				<strong dangerouslySetInnerHTML={{__html: (props.text || props.children)}}>
				</strong>
			</p>;
	}

	return <Tag className={className} data-aos={animation}
				dangerouslySetInnerHTML={{__html: (props.text || props.children)}}
				{...omit(props, ['text', 'children', 'paragraph', 'bold', 'animate'])}>
		</Tag>;
}

Text.propTypes = {
	paragraph: 'boolean',
	bold: 'boolean',
	animate: 'boolean'
};

Text.defaultProps = {
	paragraph: false,
	bold: false,
	animate: true
};

Text.icon = 'align-justify';

Text.rendererPropTypes = {
	text: 'string',
	paragraph: 'boolean',
	bold: 'boolean',
	animate: 'boolean'
};
