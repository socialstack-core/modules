import Modal from 'UI/Modal';

export default function ConfirmModal(props) {
	const {
		title,
		confirmCallback, confirmText, confirmVariant,
		cancelCallback, cancelText, cancelVariant
	} = props;

	var confirmClass = 'btn btn-' + (confirmVariant || 'primary');
	var cancelClass = 'btn btn-' + (cancelVariant || 'outline-primary');
	
	return (
		<Modal visible className="confirm-modal" title={title || `Please confirm`} onClose={() => cancelCallback()}>
			{this.props.children}
			<footer className="confirm-modal__footer">
				<button type="button" className={cancelClass} onClick={() => cancelCallback(false)}>
					{cancelText || `Cancel`}
				</button>
				<button type="button" className={confirmClass} onClick={() => {
					confirmCallback();
					cancelCallback();
				}}>
					{confirmText || `Yes`}
				</button>
			</footer>
		</Modal>
	);
}
