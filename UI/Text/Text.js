import omit from 'UI/Functions/Omit';
//import Canvas from 'UI/Canvas';
/**
 * This component displays translated text. Usage:
 * <Text>Hello world</Text>
 * <Text group='header'>Hello world</Text>
 * <Text group='header' key='hello_world'>Hello world</Text>
 */
export default function Text (props) {
	const { paragraph, bold, className, animation, animationDirection } = props;
	const omitProps = ['text', 'children', 'paragraph', 'bold', 'className', 'animation', 'animationDirection'];

	var anim = animation ? animation : undefined;

	// ref: https://github.com/michalsnik/aos
	// TODO: disable horizontal anims on mobile to prevent triggering horizontal scrolling issues
	switch (anim) {
		case 'fade':
		case 'zoom-in':
		case 'zoom-out':

			if (animationDirection) {
				anim += "-" + animationDirection;
			}

			break;
	
		case 'flip':
		case 'slide':

			// default static flip / slide animations to "up" variants
			if (animationDirection) {
				anim += "-" + animationDirection;
			} else {
				anim += "-up";
			}

		break;
	}

	var Tag = paragraph ? "p" : "span";

	if (bold && !paragraph) {
		Tag = "strong";
	}

	if (bold && paragraph) {
		return <p className={className} data-aos={anim} {...omit(props, omitProps)}>
				<strong dangerouslySetInnerHTML={{__html: (props.text || props.children)}}>
				</strong>
			</p>;
	}

	return <Tag className={className} data-aos={anim} dangerouslySetInnerHTML={{ __html: (props.text || props.children) }} {...omit(props, omitProps)}>
			{
				/*<Canvas>props.text</Canvas>*/
				props.text ? props.text : props.children
			}
			
		</Tag>;
}

Text.propTypes = {
	text: 'canvas',
	paragraph: 'boolean',
	bold: 'boolean',
	className: 'string',
	animation: [
		{ name: 'None', value: null },
		{ name: 'Fade', value: 'fade' },
		{ name: 'Flip', value: 'flip' },
		{ name: 'Slide', value: 'slide' },
		{ name: 'Zoom in', value: 'zoom-in' },
		{ name: 'Zoom out', value: 'zoom-out' }
	],

	// NB - currently unsupported:
	//fade-up-right
	//fade-up-left
	//fade-down-right
	//fade-down-left
	animationDirection: [
		{ name: 'Static', value: null },
		{ name: 'Up', value: 'up' },
		{ name: 'Down', value: 'down' },
		{ name: 'Left', value: 'left' },
		{ name: 'Right', value: 'right' },
	]
};

Text.defaultProps = {
	paragraph: false,
	bold: false,
	animation: 'none',
	animationDirection: 'static',
};

Text.icon = 'align-justify';

Text.rendererPropTypes = {
	text: 'string',
	paragraph: 'boolean',
	bold: 'boolean',
	className: 'string'
};

Text.priority = true;
