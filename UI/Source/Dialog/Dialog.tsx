import { useEffect, useState, useRef } from 'react';
import Button from 'UI/Button';

type DialogProps = React.PropsWithChildren<{
	/**
	 * dialog title
	 */
	title?: string,

	/**
	 * set true to display modal dialog
	 */
	isOpen: boolean,

	/**
	 * method to call when closing dialog
	 */
	onClose: () => void,

	/**
	 * optionally prevent dialog from being closed (enforce response)
	 */
	noClose?: boolean,

	/**
	 * optional additional classnames
	 */
	className?: string,

	children?: React.ReactNode
}> & (
		| {
			/**
			 * set true to use ConfirmDialog defaults
			 */
			confirm: true;
			confirmVariant?: string;
			confirmCallback: () => void;
			cancelCallback?: () => void;
			confirmText?: string;
			cancelText?: string;
		}
		| {
			confirm?: false;
			confirmVariant?: never;
			confirmCallback?: never;
			cancelCallback?: never;
			confirmText?: never;
			cancelText?: never;
		}
	);

type DialogHeaderProps = React.PropsWithChildren<{
	children?: React.ReactNode
}>;

type DialogFooterProps = React.PropsWithChildren<{
	children?: React.ReactNode
}>;

function DialogRoot(props: DialogProps) {
	const { isOpen, onClose, className, children,
		confirm, confirmVariant, confirmCallback, cancelCallback, confirmText, cancelText,
		...attribs } = props;
	const [error, setError] = useState<PublicError>();
	const [loading, setLoading] = useState<boolean>(false);
	const dialogRef = useRef<HTMLDialogElement>(null);
	let headerNode: React.ReactNode = null;
	let footerNode: React.ReactNode = null;

	const baseClass = 'ui-dialog';
	let classNames = [baseClass];

	if (confirm) {
		classNames.push(`${baseClass}--confirm`);
	}

	const defaultTitle = confirm ? `Please Confirm` : '';
	const title = props.title?.trim().length ? props.title : defaultTitle;
	const noClose = confirm ? true : props.noClose;

	if (noClose) {
		classNames.push(`${baseClass}--no-close`);
	}

	if (className?.trim().length) {
		classNames.push(className);
	}

	// enable animation for open/close
	useEffect(() => {
		const dialog = dialogRef.current;

		if (!dialog) {
			return;
		}

		const originalShow = dialog.show;
		const originalShowModal = dialog.showModal;
		const originalClose = dialog.close;

		dialog.show = () => {
			originalShow.call(dialog);
			dialog.dataset.show = "true";
		}

		dialog.showModal = () => {
			originalShowModal.call(dialog);
			dialog.dataset.show = "true";
		}

		dialog.close = () => {
			dialog.dataset.show = "false";
		}

		const handleCancel = (e) => {
			e.preventDefault();
			dialog.close();
		};

		function transitionEndHandler(e) {

			if (e.target == dialog) {

				if (dialog.dataset.show == "true") {
					// opened
					delete dialog.dataset.show;
				}

				if (dialog.dataset.show == "false") {
					// closed
					originalClose.call(dialog);
				}

			}

		}

		dialog.addEventListener('transitionend', transitionEndHandler);
		dialog.addEventListener('cancel', handleCancel);

		return () => {
			dialog.removeEventListener('cancel', handleCancel);
			dialog.removeEventListener('transitionend', transitionEndHandler);

			// Restore original behavior
			dialog.show = originalShow;
			dialog.showModal = originalShowModal;
			dialog.close = originalClose;
		};

	}, []);

	// keep isOpen flag in sync
	useEffect(() => {
		const dialog = dialogRef.current;

		if (!dialog) {
			return;
		}

		if (isOpen) {

			if (!dialog.open) {
				dialog.showModal();
			}

		} else {

			if (dialog.open) {
				dialog.close();
			}
		}

	}, [isOpen]);

	const flattenChildren = (children: React.ReactNode): any[] => {
		if (children == null) return [];
		return Array.isArray(children) ? children.flat() : [children];
	};

	const isType = (child: any, component: any) => {
		return child?.type === component || child?.type?.displayName === component.displayName;
	};

	const allChildren = flattenChildren(children);

	allChildren.forEach(child => {
		if (child && typeof child === 'object') {
			if (isType(child, DialogHeader)) {
				headerNode = child.props.children;
			} else if (isType(child, DialogFooter)) {
				footerNode = child.props.children;
			}
		}
	});

	// generate header if none provided
	if (headerNode == null) {
		headerNode = <>
			<h2 className="ui-dialog__title">
				{title}
			</h2>
			{!noClose && <>
				<Button sm outlined onClick={onClose} className="ui-dialog__close">
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M18 6 6 18" />
						<path d="m6 6 12 12" />
					</svg>
				</Button>
			</>}
		</>;
	}

	// generate footer if none provided (confirm dialog only)
	if (footerNode == null && confirm) {
		footerNode = <>
			{/* cancel */}
			<Button outlined variant={confirmVariant || 'danger'} onClick={() => {

				if (cancelCallback) {
					cancelCallback();
				}

				onClose();
			}}>
				{cancelText || `Cancel`}
			</Button>

			{/* confirm */}
			<Button variant={confirmVariant || 'success'} onClick={() => {
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
				{confirmText || `Continue`}
			</Button>
		</>;
	}

	// filter header / footer provided within children
	const filteredChildren = allChildren.filter(child => {
		return !(child && typeof child === 'object' && (isType(child, DialogHeader) || isType(child, DialogFooter)));
	});

	return <>
		{/* NB: only use [open] attribute here to render non-modal dialogs (not recommended - try UI/Popover) */}
		<dialog className={classNames.join(' ')} closedby={noClose ? 'none' : 'closerequest'}
			ref={dialogRef}
			onClose={onClose}
			onCancel={(e) => {
				// Prevents "Esc" from closing it without updating React state
				e.preventDefault();
				onClose();
			}}>

			{error &&
				<Alert variant="danger">
					{error.message}
				</Alert>
			}

			{loading &&
				<Loading />
			}

			{!loading && <>
				<header className="ui-dialog__header">
					{headerNode}
				</header>
				<div className="ui-dialog__content">
					{filteredChildren}
				</div>
				{footerNode && <>
					<footer className="ui-dialog__footer">
						{footerNode}
					</footer>
				</>}
			</>}

		</dialog>
	</>;
}

function DialogHeader(props: DialogHeaderProps) {
	const { children } = props;

	return children;
}

function DialogFooter(props: DialogFooterProps) {
	const { children } = props;

	return children;
}

// required for preact (without preact/compat layer) support
DialogHeader.displayName = "DialogHeader";
DialogFooter.displayName = "DialogFooter";

DialogRoot.Header = DialogHeader;
DialogRoot.Footer = DialogFooter;

const Dialog = DialogRoot;
export default Dialog;