/**
 * Props for the Notice component.
 */
interface AdminPageNoticeProps {
	title?: string,
	notice?: string,
	children?: React.ReactNode
}

/**
 * The Notice React component.
 * @param props React props.
 */
const AdminPageNotice: React.FC<React.PropsWithChildren<AdminPageNoticeProps>> = (props) => {
	const { title, notice, children } = props;
	const hasTitle = title?.length;
	const hasNotice = notice?.length;

	if (!hasTitle && !hasNotice && !children) {
		return null;
	}

	return (
		<div className="admin-page__notice">
			{hasTitle && <>
				<strong className="admin-page__notice-title">
					{title}
				</strong>
				{hasNotice && <>
					<p className="admin-page__notice-text">
						{notice}
					</p>
				</>}
				{children}
			</>}
		</div>
	);
}

// required for preact (without preact/compat layer) support
AdminPageNotice.displayName = "AdminPageNotice";

export default AdminPageNotice;
