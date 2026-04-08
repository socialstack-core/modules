/**
 * Props for the Notification Badge component.
 */
interface NotificationBadgeProps {
	/**
	 * notification count
	 */
	count?: number,

	/**
	 * icon
	 */
	icon?: React.ReactNode,

	/**
	 * variant (e.g. info / success / warning - defaults to danger)
	 */
	variant?: "primary" | "secondary" | "success" | "danger" | "warning" | "info",
}

/**
 * Notificaton Badge component
 */
const NotificationBadge: React.FC<NotificationBadgeProps> = (props) => {
	var { count, icon, variant } = props;
	const MAX_COUNT = 99;

	if (!icon && count <= 0) {
		return;
	}

	var notificationClass: string[] = ['notification-badge'];

	if (!icon && count > MAX_COUNT) {
		notificationClass.push('notification-badge--small');
	}

	if (variant?.length) {
		notificationClass.push(`notification-badge--${variant}`);
	}

	return <>
		<span className={notificationClass.join(' ')}>
			{icon}
			{!icon && count > 0 && Math.min(count, MAX_COUNT)}
		</span>
	</>;
}

export default NotificationBadge;