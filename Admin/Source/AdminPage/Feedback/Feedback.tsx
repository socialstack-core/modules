type FeedbackType = 'success' | 'danger' | 'warning' | 'info';

/**
 * Props for the Feedback component.
 */
interface AdminPageFeedbackProps {
	/**
	 * feedback style (success, danger, warning, info) - defaults to info
	 */
	variant?: FeedbackType,

	children?: React.ReactNode
}

/**
 * The Feedback React component.
 * @param props React props.
 */
const AdminPageFeedback: React.FC<React.PropsWithChildren<AdminPageFeedbackProps>> = (props) => {
	const { children } = props;

	const feedbackClasses = ["admin-page__feedback"];
	feedbackClasses.push(`admin-page__feedback--${props.variant || 'info'}`);

	return (
		<div className={feedbackClasses.join(' ')}>
			<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
				className="admin-page__feedback-icon admin-page__feedback-icon--info" viewBox="0 0 24 24">
				<circle cx="12" cy="12" r="10" />
				<path d="M12 16v-4M12 8h0" />
			</svg>
			<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
				className="admin-page__feedback-icon admin-page__feedback-icon--warning" viewBox="0 0 24 24">
				<path d="M21.7 18l-8-14a2 2 0 00-3.4 0l-8 14A2 2 0 004 21h16a2 2 0 001.7-3M12 9v4M12 17h0" />
			</svg>
			<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
				className="admin-page__feedback-icon admin-page__feedback-icon--success" viewBox="0 0 24 24">
				<path d="M20 6L9 17l-5-5" />
			</svg>
			<svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
				className="admin-page__feedback-icon admin-page__feedback-icon--danger" viewBox="0 0 24 24">
				<path d="M18 6L6 18M6 6l12 12" />
			</svg>
			<div className="admin-page__feedback-internal">
				{children}
			</div>
		</div>
	);
}

// required for preact (without preact/compat layer) support
AdminPageFeedback.displayName = "AdminPageFeedback";

export default AdminPageFeedback;
