import omit from 'UI/Functions/Omit';

/**
 * For h1/h2/h3 etc.
 */
export default function Heading (props) {
	const { size, className, children, animation, animationDirection } = props;

	var Mod = 'h' + (size || '1');
	var headerClass = 'heading ' + (className || '');

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

	return <Mod className={headerClass} data-aos={anim}
				{...omit(props, ['size', 'children', 'className', 'animation', 'animationDirection'])}>
			{children}
		</Mod>;
}

Heading.propTypes={
	size: ['1','2','3','4','5','6'],
	children: {default: 'My New Heading'},
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
}

Heading.defaultProps = {
	animation: 'none',
	animationDirection: 'static',
	className: '',
};

Heading.icon='heading';