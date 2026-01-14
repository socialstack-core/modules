# UI/Dialog

```tsx
import Dialog from 'UI/Dialog';
```

No description has been added for this component.

# Usage Examples

## Default

```tsx
	{/* basic dialog */}
	const [dialogOpen, setDialogOpen] = useState(false);
	<Dialog title={`Basic Dialog Test`} isOpen={dialogOpen} onClose={() => setDialogOpen(false)}>
		Dialog test content
	</Dialog>

	{/* custom dialog (directly set header and / or footer content) */}
	const [customDialogOpen, setCustomDialogOpen] = useState(false);
	<Dialog isOpen={customDialogOpen} onClose={() => setCustomDialogOpen(false)}>
		<Dialog.Header>
			{`Custom Dialog Test`}
		</Dialog.Header>
		Custom dialog content
		<Dialog.Footer>
			<Button onClick={() => setCustomDialogOpen(false)}>
				{`Close`}
			</Button>
		</Dialog.Footer>
	</Dialog>

	{/* confirmation dialog
	  * NB: the following props are optional:
	  * - confirmVariant
	  * - cancelCallback
	  * - confirmText
	  * - cancelText
	  */}
	const [confirmDialogOpen, setConfirmDialogOpen] = useState(false);
	<Dialog confirm title={`Confirm Dialog Test`} isOpen={confirmDialogOpen} onClose={() => setConfirmDialogOpen(false)}
		confirmCallback={() => {
			// code to run when action confirmed (required)
			//...
		}}
		cancelCallback={() => {
			// code to run when action cancelled (optional)
			//...
		}}
		confirmVariant="primary" confirmText={`Do the thing`} cancelText={`Cancel the thing`}>
		<p>
			This will lorem ipsum dolor sit amet.
		</p>
		<p>
			Do you wish to continue?
		</p>
	</Dialog>

```