const SPINNER_BORDER_PREFIX = 'spinner-border';
const SPINNER_GROW_PREFIX = 'spinner-grow';
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
 * Bootstrap Spinner component
 * @param {string} variant		- determines appearance (primary / secondary etc.)
 * @param {boolean} isBorder	- set true to show spinning border (false for fading / growing type)
 * @param {boolean} isSmall		- set true to reduce size of spinner
 * @param {string} label		- accessibility label
 */
export default function Spinner(props) {
	const { variant, isBorder, isSmall, label } = props;

	var spinnerClass = [isBorder ? SPINNER_BORDER_PREFIX : SPINNER_GROW_PREFIX];
	var spinnerVariant, spinnerLabel;

	if (variant) {
		spinnerVariant = variant.toLowerCase();
	}

	if (!spinnerVariant || !supportedVariants.includes(spinnerVariant)) {
		spinnerVariant = DEFAULT_VARIANT;
	}

	spinnerClass.push('text-' + spinnerVariant);

	spinnerLabel = label || `Please wait...`;

	if (isSmall) {
		spinnerClass.push((isBorder ? SPINNER_BORDER_PREFIX : SPINNER_GROW_PREFIX) + '-sm');
    }

	return (
		<div className={spinnerClass.join(' ')} role="status">
			{label && <>
				<span class="visually-hidden">
					{label}
				</span>
			</>}
		</div>
	);
}

Spinner.propTypes = {
};

Spinner.defaultProps = {
}

Spinner.icon='align-center';
