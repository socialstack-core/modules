
interface TextProps {
	/**
	 * optional additional class name(s)
	 */
	className?: string,

	paragraph?: boolean,
	bold?: boolean,
	/**
	 * optionally animate Text into place on scroll
	 */
	animation?: AnimationProps
}

/**
 * This component displays translated text. Usage:
 * <Text>Hello world</Text>
 * <Text group='header'>Hello world</Text>
 * <Text group='header' key='hello_world'>Hello world</Text>
 */
const Text: React.FC<React.PropsWithChildren<TextProps>> = ({ children, paragraph, bold, className, animation, ...props }) => {

	let animType: string | undefined = animation ? (animation.type === 'none' ? undefined : animation.type) : undefined;

	// ref: https://github.com/michalsnik/aos
	// TODO: disable horizontal anims on mobile to prevent triggering horizontal scrolling issues
	switch (animType) {
		case 'fade':
		case 'zoom-in':
		case 'zoom-out':

			if (animation?.direction) {
				animType += "-" + animation.direction;
			}

			break;
	
		case 'flip':
		case 'slide':

			// default static flip / slide animations to "up" variants
			if (animation?.direction) {
				animType += "-" + animation.direction;
			} else {
				animType += "-up";
			}

		break;
	}

	var Tag = (paragraph ? "p" : "span") as React.ElementType;

	if (bold && !paragraph) {
		Tag = "strong";
	}

	var content = children as string;

	if (bold && paragraph) {
		return <p className={className} data-aos={animType} {...props}>
				<strong dangerouslySetInnerHTML={{__html: content}} />
			</p>;
	}

	return <Tag
		className={className}
		data-aos={animType}
		dangerouslySetInnerHTML={{ __html: content }}
		{...props}
	/>;
}

export default Text;

/*
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
*/