import { useState } from 'react';
import CloseButton from 'UI/CloseButton';
import Icon from 'UI/Icon';

const ALERT_PREFIX = 'alert';
const DEFAULT_VARIANT = 'info';

export type AlertType = 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'info' | 'light' | 'dark';

/**
 * Props for the alert component.
 * @icon fal fa-exclamation-triangle
 * @description Displays an alert message.
 */
interface AlertProps {
	/**
	 * determines appearance (info/ error/ primary / secondary etc.)
	 */
	variant?: AlertType,
	/**
	 * set true to display icon
	 */
	showIcon?: boolean,
	/**
	 * Optionally provide a custom Icon instance.
	 */
	customIcon?: React.ReactElement,
	/**
	 * set true to display close button
	 */
	isDismissable?: boolean,
	
	/**
	 * The alert type
	 */
	type?: string,

	/**
	 * optional additional classnames
	 */
	className?: string
}

/**
 * Alert component
 */
const Alert: React.FC<React.PropsWithChildren<AlertProps>> = (props) => {
	const { children, customIcon, isDismissable, className } = props;
	let { showIcon } = props;
	const [showAlert, setShowAlert] = useState(true);

	if (showIcon === undefined) {
		showIcon = true;
	}

	var variant = props.variant || props.type;
	var alertVariant = variant?.toLowerCase() || DEFAULT_VARIANT;

	var icon: React.ReactNode = undefined;

	// resolve default icon class
	switch (alertVariant) {
		//case 'primary':
		//break;

		//case 'secondary':
		//break;

		case 'success':
			icon = <Icon type="fa-check-circle" lg light />;
			break;

		case 'danger':
			icon = <Icon type="fa-times-circle" lg light />;
			break;

		case 'warning':
			icon = <Icon type="fa-exclamation-triangle" lg light />;
			break;

		case 'info':
			icon = <Icon type="fa-info-circle" lg light />;
			break;

		//case 'light':
		//break;

		//case 'dark':
		//break;
	}

	if (customIcon) {
		icon = customIcon;
	}

	var alertClass = [ALERT_PREFIX];
	alertClass.push(ALERT_PREFIX + '-' + alertVariant);

	if (isDismissable) {
		alertClass.push(ALERT_PREFIX + '-dismissable');
	}

	if (className?.length) {
		alertClass.push(className);
	}

	return (<>
		{showAlert && <>
			<div className={alertClass.join(' ')} role="alert">
				{isDismissable && <>
					<CloseButton callback={e => setShowAlert(false)} />
				</>}
				<div className="alert__internal">
					{showIcon && icon}
					<span className="alert__content">
						{children}
					</span>
				</div>
			</div>
		</>}
	</>);
}

export default Alert;