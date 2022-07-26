import CloseButton from 'UI/CloseButton';
import { useState, useEffect, useRef } from 'react';

export default function Notifications(props) {
	const { notifications, showingNotifications } = props;
	const [ notificationIndex, setNotificationIndex] = useState(0);

	function prevNotification() {
		setNotificationIndex(notificationIndex == 0 ? notifications.length - 1 : notificationIndex - 1);
	}

	function nextNotification() {
		setNotificationIndex((notificationIndex + 1) % notifications.length);
	}

	function removeNotification(index) {
		if (index == notifications.length - 1) {

			if (index > 0) {
				setNotificationIndex(index - 1);
            }
		}

		props.removeNotification(index);
    }

	return <>
		{showingNotifications && <>
			<div className="huddle-chat__notifications">
				<header className="huddle-chat__notifications-header">
					<h2 className="huddle-chat__notifications-title">
						{`Notifications`}
						{notifications && notifications.length > 1 && <>
							&nbsp;({notificationIndex + 1} of {notifications.length})
						</>}
					</h2>
					<CloseButton isLight className="huddle-chat__notifications-close" callback={() => props.toggleNotifications()} />
				</header>
				<div className="huddle-chat__notifications-content">
					{notifications[notificationIndex]}
				</div>
				<footer className="huddle-chat__notifications-footer">
					{notifications && notifications.length > 1 && <>
						<button type="button" className="btn btn-outline-light huddle-chat__notifications-prev" onClick={() => prevNotification()}>
							<i className="fal fa-chevron-left"></i>
						</button>
						<button type="button" className="btn btn-outline-light huddle-chat__notifications-next" onClick={() => nextNotification()}>
							<i className="fal fa-chevron-right"></i>
						</button>
					</>}
					<button type="button" className="btn btn-light huddle-chat__notifications-remove" onClick={() => removeNotification(notificationIndex)}>
						{`Remove`}
					</button>
				</footer>
			</div>
		</>}
	</>;
}
