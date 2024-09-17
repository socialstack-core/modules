import getRef from 'UI/Functions/GetRef';
import omit from 'UI/Functions/Omit';
import { useConfig } from 'UI/Session';

const uploaderConfig = useConfig('uploader') || {};
let UPLOADER_SIZES = uploaderConfig[0].imageSizes || [32, 64, 100, 128, 200, 256, 512, 768, 1024, 1920, 2048];
UPLOADER_SIZES.unshift('original');

/*
Used to display an image from a fileRef.
Min required props: size, fileRef. Size must be an available size from your uploader config. You should pick the nearest bigger size from the physical size you're after.
<Image fileRef='public:2.jpg' size=100 />
*/
export default function Image(props) {
	const { onClick, fileRef, linkUrl, size, fullWidth, float, className,
		animation, animationDirection, animationDuration,
		width, height, disableBlurhash } = props;

	var anim = animation ? animation : undefined;
	var animOnce = animation ? true : undefined;
	var animDuration = animationDuration > 0 ? animationDuration : undefined;

	if (!(fileRef)) {
		return null;
	}

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

	var imageClass = "image";

	switch (float) {
		case "Left":
			imageClass += " image-left";
			break;

		case "Right":
			imageClass += " image-right";
			break;

		case "Center":
			imageClass += " image-center";
			break;
	}

	// NB: separate width/height values will override any size selected
	let testWidth = parseInt(width, 10);
	let testHeight = parseInt(height, 10);

	if (fullWidth || (size == "original" && isNaN(testWidth))) {
		imageClass += " image-wide";
	}

	if (className) {
		imageClass += " " + className;
	}

	var attribs = omit(props, ['fileRef', 'onClick', 'linkUrl', 'size', 'width', 'height', 'fullWidth', 'float', 'className', 'animation', 'animationDirection', 'animationDuration']);

	attribs.alt = attribs.alt || attribs.title;

	if (!isNaN(testWidth)) {
		attribs.width = testWidth;
	}

	if (!isNaN(testHeight)) {
		attribs.height = testHeight;
	}

	var img = <div className={imageClass} onClick={props.onClick} 
			data-aos={linkUrl ? undefined : anim} 
			data-aos-once={linkUrl ? undefined : animOnce}
			data-aos-duration={linkUrl ? undefined : animDuration}>
		{getRef(props.fileRef, {
			attribs, size, nonResponsive: !fullWidth, disableBlurhash: disableBlurhash
		})}
	</div>;
	return linkUrl ? <a alt={attribs.alt} title={attribs.title} href={linkUrl} 
			data-aos={anim} data-aos-once={animOnce} data-aos-duration={animDuration}>
		{img}
	</a> : img;
}

Image.defaultProps = {
	fileRef: null,
	float: 'None',
	animation: 'none',
	animationDirection: 'static',
	animationDuration: 400
};

Image.propTypes = {
	fileRef: 'image',
	linkUrl: 'url',
	title: 'string',
	fullWidth: 'bool',
	width: 'number',
	height: 'number',
	size: {
		type: UPLOADER_SIZES,
		help: `Note: will be overridden by width / height values, if supplied`,
		helpPosition: 'icon'
	},	
	float: { type: ['None', 'Left', 'Right', 'Center'] },
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
	],
	animationDuration: 'int',
	disableBlurhash: 'bool'
};

Image.groups = 'formatting';
Image.icon = 'image';