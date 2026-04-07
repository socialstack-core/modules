import Dialog from 'UI/Dialog';

type ConfirmDialogProps = React.PropsWithChildren<{
	/**
	 * dialog title
	 */
	title?: string,

	/**
	 * set true to display modal dialog
	 */
	isOpen: boolean,

	/**
	 * set true to prevent dialog from closing automatically after confirmation method has completed
	 */
	keepOpen?: boolean,

	/**
	 * method to call when closing dialog
	 */
	onClose: () => void,

	/**
	 * optional additional classnames
	 */
	className?: string,

	children?: React.ReactNode,

	variant?: string,
	confirmVariant?: string,
	confirmCallback: () => void,
	cancelCallback?: () => void,
	confirmText?: string,
	cancelText?: string
}>;

const ConfirmDialog: React.FC<ConfirmDialogProps> = (props) => {
	const {
		title, isOpen, keepOpen, onClose, className, children,
		confirmCallback, cancelCallback,
		confirmText, cancelText } = props;
	const variant = props.confirmVariant ?? props.variant;

	return <>
		<Dialog confirm={true} title={title} isOpen={isOpen} keepOpen={keepOpen} onClose={onClose} noClose={true} className={className}
			confirmCallback={confirmCallback} cancelCallback={cancelCallback}
			confirmVariant={variant} confirmText={confirmText} cancelText={cancelText}>
			{children}
		</Dialog>
	</>;

};

export default ConfirmDialog;