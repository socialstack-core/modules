import { parse } from 'UI/FileRef';

interface IconBaseProps {
	/**
	 * Use the light variation.
	 */
	light?: boolean,

	/**
	 * Use the solid variation.
	 */
	solid?: boolean,

	/**
	 * Use the duotone variation.
	 */
	duotone?: boolean,

	/**
	 * Use the regular variation.
	 */
	regular?: boolean,

	/**
	 * Use the brand variation.
	 */
	brand?: boolean,

	/**
	 * True if this icon should be a fixed width.
	 */
	fixedWidth?: boolean,

	/**
	 * True if the icon should spin.
	 */
	spin?: boolean,

	/**
	 * Flip horizontally
	 */
	horizontalFlip?: boolean,

	/**
	 * Character, used internally by the compiler.
	 */
	c?: string,

	/**
	 * sizing flags
	 */
	xxs?: Boolean,
	xs?: Boolean,
	sm?: Boolean,
	md?: Boolean,
	lg?: Boolean,
	xl?: Boolean,
	xxl?: Boolean,
	x2?: Boolean,
	x3?: Boolean,
	x4?: Boolean,
	x5?: Boolean,
	x6?: Boolean,
	x7?: Boolean,
	x8?: Boolean,
	x9?: Boolean,
	x10?: Boolean,
}

/**
 * Props for the Icon component.
 */
type IconProps = IconBaseProps & {
	/**
	 * You MUST use a constant string. UI/Icon is a first-class component in the compile process.
	 * The compiler will use "type" to identify in-use icons and strip accordingly.
	 * This is the type of icon you want to display, such as type="fa-bullhorn". 
	 * Don't target this type with CSS: instead, target ui-icon if you need to do so.
	 */
	type: string,
}

const Icon: React.FC<IconProps> = (props) => {
	const {
		type, light, solid, duotone, regular, brand, fixedWidth, spin, horizontalFlip, c,
		xxs, xs, sm, md, lg, xl, xxl,
		x2, x3, x4, x5, x6, x7, x8, x9, x10
	} = props;
	
	var variant = 'fa';
	
	if(light){
		variant = 'fal';
	}else if(duotone){
		variant = 'fad';
	}else if(brand){
		variant = 'fab';
	}else if(regular){
		variant = 'far';
	}else if(solid){
		variant = 'fas';
	}
	
	var size = '';

	if (xxs) {
		size = 'fa-2xs';
	}else if (xs) {
		size = 'fa-xs';
	} else if (sm) {
		size = 'fa-sm';
	} else if (lg) {
		size = 'fa-lg';
	} else if (xl) {
		size = 'fa-xl';
	} else if (xxl) {
		size = 'fa-2xl';
	} else if (x2) {
		size = 'fa-2x';
	} else if (x3) {
		size = 'fa-3x';
	} else if (x4) {
		size = 'fa-4x';
	} else if (x5) {
		size = 'fa-5x';
	} else if (x6) {
		size = 'fa-6x';
	} else if (x7) {
		size = 'fa-7x';
	} else if (x8) {
		size = 'fa-8x';
	} else if (x9) {
		size = 'fa-9x';
	} else if (x10) {
		size = 'fa-10x';
	}

	var classNames = [variant, size, 'ui-icon'];

	if (!c) {
		// May need to reconsider this if people specifically target an icon using these class names.
		// They should generally target ui-icon instead though.
		// It exists such that static icons, which use a tiny highly accelerated subset of icons, 
		// don't end up being given 2 characters when both the accelerated set and the 
		// entire set are present (like on the admin panel).
		classNames.push(type);
	}

	if (fixedWidth) {
		classNames.push("fa-fw");
    }

	if (spin) {
		classNames.push("fa-spin");
	}

	if (horizontalFlip) {
		classNames.push("fa-flip-horizontal");
	}

	// NB: count removal: if you need it, use a wrapping component instead
	// due to the special case that Icon is to the compiler.

	return <i className={classNames.join(' ')}>{c}</i>;
}

export default Icon;

type IconRefProps = IconBaseProps & {
	fileRef: FileRef
};

export const IconRef : React.FC<IconRefProps> = (props) => {
	const { fileRef, ...iconProps } = props;

	// future version where the inline icons are actually stripped -
	// this one would not do so and instead refs the main font files with all.
	var fullRef = parse(fileRef);

	return <Icon {...iconProps} type={fullRef?.basepath || ''} />;
};