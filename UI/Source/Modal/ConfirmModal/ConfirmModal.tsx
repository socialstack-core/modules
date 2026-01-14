import { useState, useEffect } from 'react'
import Modal from 'UI/Modal';
import Loading from 'UI/Loading';
import Button from 'UI/Button';
import Alert from "UI/Alert";

export type ConfirmModalProps = {
	title?: string,
	confirmCallback: () => Promise<any> | undefined,
	confirmText?: string,
	confirmVariant?: string,
	cancelCallback: (callbackValue?: boolean) => void,
	cancelText?: string,
	cancelVariant?: string
}

const ConfirmModal: React.FC<React.PropsWithChildren<ConfirmModalProps>> = (props: React.PropsWithChildren<ConfirmModalProps>): React.ReactNode => {
	const {
		title,
		confirmCallback, confirmText, confirmVariant,
		cancelCallback, cancelText, cancelVariant
	} = props;

	const [error, setError] = useState<PublicError>();
	const [loading, setLoading] = useState<boolean>(false);

	return (
		<Modal visible className="confirm-modal" title={title || `Please confirm`} onClose={() => cancelCallback()}>
			{error && (
				<Alert variant={'danger'}>{error.message}</Alert>
			)}
			{loading ? <Loading />: <>
				{props.children}
				<footer className="confirm-modal__footer">
					<Button outlined variant={cancelVariant} onClick={() => cancelCallback(false)}>
						{cancelText || `Cancel`}
					</Button>
					<Button variant={confirmVariant} onClick={() => {
						var possiblePromise = confirmCallback();
						if (!possiblePromise) {
							cancelCallback();
							return;
						}

						// Display a loader and wait for the promise.
						setLoading(true);

						possiblePromise.then(() => {
							cancelCallback();
							setLoading(false);
							setError(undefined);
						})
							.catch((err) => {
								setError(err);
								setLoading(false);
							})
					}}>
						{confirmText || `Yes`}
					</Button>
				</footer>
			</>}
		</Modal>
	);
}

export default ConfirmModal;