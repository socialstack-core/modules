import omit from 'UI/Functions/Omit';

export default function Paragraph (props) {
	const { bold, className, animation, animationDirection } = props;

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

	return <p className={className} data-aos={anim}
				{...omit(props, ['children', 'bold', 'className', 'animation', 'animationDirection'])}>
			{bold ? <strong>{props.children}</strong> : <>{props.children}</>}
		</p>;
}

Paragraph.propTypes = {
	children: true,
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

Paragraph.defaultProps = {
	bold: false,
	animation: 'none',
	animationDirection: 'static'
};

Paragraph.icon = 'align-left';
