export type BadgeType = "primary" | "secondary" | "success" | "danger" | "warning" | "info";

/**
 * Props for the Badge component.
 */
interface BadgeProps {
	/**
	 * The style variant, "primary", "secondary" etc.  Badge defaults to a nondescript greyscale theme if none selected
	 */
	variant?: BadgeType,

	/**
	 * set true if badge should be extra small
	 */
	xs?: boolean,

	/**
	 * set true to restrict text to a single line
	 */
	noWrap?: boolean,

	/**
	 * optional additional classnames
	 */
	className?: string
}

/**
 * Badge component
 */
const Badge: React.FC<React.PropsWithChildren<BadgeProps>> = (props) => {
	var { children, variant, xs, noWrap, className } = props;

	var badgeClass: string[] = ['ui-badge'];

	if (xs) {
		badgeClass.push('ui-badge--xs');
	}

	if (noWrap) {
		badgeClass.push('ui-badge--nowrap');
	}

	if (variant?.length) {
		badgeClass.push(`ui-badge--${variant}`);
	}

	if (className?.length) {
		badgeClass.push(className);
	}

	return <>
		<span className={badgeClass.join(' ')}>
			{children}
		</span>
	</>;
}

export default Badge;