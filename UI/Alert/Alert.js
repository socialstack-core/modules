import { useState } from 'react';
import CloseButton from 'UI/CloseButton';

const ALERT_PREFIX = 'alert';
const DEFAULT_VARIANT = 'info';

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

const variantAliases = [
	{
		variant: 'primary',
		aliases: [
		]
	},
	{
		variant: 'secondary',
		aliases: [
		]
	},
	{
		variant: 'success',
		aliases: [
			'successful',
			'ok',
			'good'
		]
	},
	{
		variant: 'danger',
		aliases: [
			'fail',
			'failed',
			'failure',
			'error'
		]
	},
	{
		variant: 'warning',
		aliases: [
			'warn'
		]
	},
	{
		variant: 'info',
		aliases: [
			'information',
			'note'
		]
	},
	{
		variant: 'light',
		aliases: [
		]
	},
	{
		variant: 'dark',
		aliases: [
		]
	}
];

/**
 * Bootstrap Alert component
 * @param {any} children			- content
 * @param {string} variant			- determines appearance (primary / secondary etc.)
 * @param {string} type				- deprecated (use 'variant' in preference to this)
 * @param {boolean} showIcon		- set true to display icon
 * @param {string} icon				- set to override default icon classname
 * @param {boolean} isDismissable	- set true to display close button
 */
export default function Alert(props) {
	const { children, variant, type, showIcon, icon, isDismissable } = props;
	const [showAlert, setShowAlert] = useState(true);

	var alertVariant, iconClass;

	if (type) {
		alertVariant = type.toLowerCase();
	} else {
		alertVariant = variant.toLowerCase();
	}

	if (!alertVariant) {
		alertVariant = DEFAULT_VARIANT;
	}

	var aliases = variantAliases.filter(i => i.aliases.includes(alertVariant));

	if (aliases.length) {
		alertVariant = aliases[0].variant;
    }

	if (!supportedVariants.includes(alertVariant)) {
		alertVariant = DEFAULT_VARIANT;
	}

	// resolve default icon class
	switch (alertVariant) {
		//case 'primary':
		//break;

		//case 'secondary':
		//break;

		case 'success':
			iconClass = 'fal fa-check-circle';
			break;

		case 'danger':
			iconClass = 'fal fa-times-circle';
			break;

		case 'warning':
			iconClass = 'fal fa-exclamation-triangle';
			break;

		case 'info':
			iconClass = 'fal fa-info-circle';
			break;

		//case 'light':
		//break;

		//case 'dark':
		//break;
	}

	if (icon) {
		iconClass = icon;
	}

	if (iconClass) {
		iconClass = ALERT_PREFIX + '__icon ' + iconClass;
	}

	var alertClass = [ALERT_PREFIX];
	alertClass.push(ALERT_PREFIX + '-' + alertVariant);

	if (isDismissable) {
		alertClass.push(ALERT_PREFIX + '-dismissable');
	}

	return (<>
		{showAlert && <>
			<div className={alertClass.join(' ')} role="alert">
				{isDismissable && <>
					<CloseButton callback={() => setShowAlert(false)} />
				</>}
				<div className="alert__internal">
					{showIcon && iconClass && <i className={iconClass}></i>}
					<span className="alert__content">
						{children}
					</span>
				</div>
			</div>
		</>}
	</>);
}

Alert.propTypes = {
	variant: ['error', 'warning', 'success', 'info'],
	isDismissable: 'bool',
	showIcon: 'bool',
	icon: 'string',
	children:'jsx'
};

Alert.defaultProps = {
	children: 'My New Alert',
	variant: DEFAULT_VARIANT,
	isDismissable: false,
	showIcon: true
}

Alert.icon = 'exclamation-circle';