const BADGE_PREFIX = 'badge';
const DEFAULT_VARIANT = 'primary';

const supportedVariants = [
	'primary',
	'secondary',
	'success',
	'danger',
	'warning',
	'info',
	'light',
	'dark'
];

/**
 * Bootstrap Badge component
 * @param {any} children		- content
 * @param {string} variant		- determines appearance (primary / secondary etc.)
 * @param {boolean} isRounded	- set true to display rounded badge
 */
export default function Badge(props) {
	const { children, variant, isRounded } = props;

	var badgeVariant;

	if (variant) {
		badgeVariant = variant.toLowerCase();
	}

	if (!badgeVariant || !supportedVariants.includes(badgeVariant)) {
		badgeVariant = DEFAULT_VARIANT;
	}

	var badgeClass = [BADGE_PREFIX];

	if (isRounded) {
		badgeClass.push('rounded-pill');
	}

	badgeClass.push('bg-' + badgeVariant);

	// NB: default warning/info/light variants need darker text (e.g. .text-dark)
	// TODO: automate text contrast change

	return (<>
		<span className={badgeClass.join(' ')}>
			{children}
		</span>
	</>);
}

Badge.propTypes = {
	variant: supportedVariants,
	isRounded: 'bool'
};

Badge.defaultProps = {
	children: 'Badge',
	variant: DEFAULT_VARIANT,
	isRounded: false
}

Badge.icon = 'exclamation-circle';
