// TODO: support for multiple bars:
// ref: https://getbootstrap.com/docs/5.1/components/progress/#multiple-bars

const PROGRESS_PREFIX = 'progress';
const PROGRESS_BAR_PREFIX = 'progress-bar';
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
 * Bootstrap Progress component
 * @param {string} variant			- determines appearance (primary / secondary etc.)
 * @param {number} min				- min value (default 0)
 * @param {number} max				- max value (default 100)
 * @param {number} value			- current percentage value (default 0)
 * @param {boolean} showLabel		- set true to display label showing value
 * @param {boolean} isStriped		- set true to stripe background
 * @param {boolean} isAnimated		- set true to animate stripe background
 */
export default function Progress(props) {
	var { variant, min, max, value, showLabel, isStriped, isAnimated } = props;
	var progressVariant;

	if (variant) {
		progressVariant = variant.toLowerCase();
	}

	if (!progressVariant || !supportedVariants.includes(progressVariant)) {
		progressVariant = DEFAULT_VARIANT;
	}

	var progressClass = [PROGRESS_PREFIX];

	var progressBarClass = [PROGRESS_BAR_PREFIX];

	if (isStriped) {
		progressBarClass.push(PROGRESS_BAR_PREFIX + '-striped');

		if (isAnimated) {
			progressBarClass.push(PROGRESS_BAR_PREFIX + '-animated');
		}

	}

	progressBarClass.push('bg-' + progressVariant);

	min = min || 0;
	max = max || 100;
	value = value || 0;

	var progressBarStyle = { 'width': value + '%'}

	return (<div className={progressClass.join(' ')}>
		<div className={progressBarClass.join(' ')} role="progressbar" style={progressBarStyle} aria-valuenow={value} aria-valuemin={min} aria-valuemax={max}>
			{showLabel && <>
				{value}%
			</>}
		</div>
	</div>);
}

Progress.propTypes = {
	variant: supportedVariants,
	min: 'int',
	max: 'int',
	value: 'int',
	showLabel: 'bool',
	isStriped: 'bool',
	isAnimated: 'bool'
};

Progress.defaultProps = {
	variant: DEFAULT_VARIANT,
	min: 0,
	max: 100,
	value: 0,
	showLabel: false,
	isStriped: false,
	isAnimated: false
}

Progress.icon = 'exclamation-circle';