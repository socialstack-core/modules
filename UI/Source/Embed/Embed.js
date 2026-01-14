//import { getSizeClasses } from 'UI/Functions/Components';

const COMPONENT_PREFIX = 'ui-embed';
//const DEFAULT_VARIANT = 'primary';

const DEFAULT_WIDTH = 560;
const DEFAULT_HEIGHT = 315;
const DEFAULT_ALLOW = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share";
const DEFAULT_REFERRER_POLICY = "strict-origin-when-cross-origin";

export default function Embed(props) {
	const {
		//variant,
		xs, sm, md, lg, xl,
		ratio43,
		//outline, outlined,
		//close,
		//submit,
		//disable, disabled,
		//round, rounded,
		//children,
		//onClick
		width, height,
		resize, resizable,
		maxWidth, maxHeight,
		iframe,
		src, title,
		allow,
		referrerPolicy,
		noFullscreen
	} = props;

	if (!src || !src.length) {
		return;
	}

	let _width = width || DEFAULT_WIDTH;
	let _height = height || DEFAULT_HEIGHT;

	let _resize = resize || resizable;

	if (_resize) {

		switch (_resize) {
			case 'none':
			case 'both':
			case 'horizontal':
			case 'vertical':
				break;

			// block / inline support requires later version of iOS
			case 'block':
			case 'x':
				_resize = 'horizontal';
				break;

			case 'inline':
			case 'y':
				_resize = 'vertical';
				break;

			// default resize behaviour to both axes
			default:
				_resize = 'both';
				break;
		}

	}

	let _allow = allow || DEFAULT_ALLOW;
	let _referrerPolicy = referrerPolicy || DEFAULT_REFERRER_POLICY;

	//var btnVariant = variant?.toLowerCase() || (close ? undefined : DEFAULT_VARIANT);
	//var btnType = submit ? "submit" : "button";

	var componentClasses = [COMPONENT_PREFIX];

	//if (btnVariant) {
	//	componentClasses.push(`${COMPONENT_PREFIX}--${btnVariant}`);
	//}

	//componentClasses = componentClasses.concat(getSizeClasses(COMPONENT_PREFIX, props));

	if (ratio43) {
		componentClasses.push(`${COMPONENT_PREFIX}--ratio-4-3`);
	}

	if (resizable) {
		componentClasses.push(`${COMPONENT_PREFIX}--resizable`);
	}

	//let isDisabled = disable || disabled;

	/* runs only after component initialisation (comparable to legacy componentDidMount lifecycle method)
	useEffect(() => {
		// ...
	}, []);
	*/

	/* runs after both component initialisation and each update (comparable to legacy componentDidMount / componentDidUpdate lifecycle methods)
	useEffect(() => {
		// ...
	});
	*/

	let wrapperStyle = {
		maxWidth: maxWidth ? `calc((${maxWidth} / var(--ui-font-size)) * 1rem)` : undefined,
		maxHeight: maxHeight ? `calc((${maxHeight} / var(--ui-font-size)) * 1rem)` : undefined,
		resize: _resize
	};

	return (
		<section className={componentClasses.join(' ')}>
			<div className={`${COMPONENT_PREFIX}__wrapper`} style={wrapperStyle}>
				<div className={`${COMPONENT_PREFIX}__iframe`}>
					{iframe}
					{!iframe && <>
						<iframe width={_width} height={_height} src={src} title={title}
							frameborder="0" allow={_allow} referrerpolicy={_referrerPolicy} allowfullscreen={noFullscreen ? undefined : true}></iframe>
					</>}
				</div>
			</div>
		</section>
	);
}

Embed.propTypes = {
};

Embed.defaultProps = {
}
