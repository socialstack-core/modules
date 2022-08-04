import { useToast } from 'UI/Functions/Toast';

export default function ToastTest(props) {

	/* runs only after component initialisation (comparable to legacy componentDidMount lifecycle method)
	useEffect(() => {
		// ...
	}, []);
	*/

	const { pop } = useToast();

	function addToast(title, description, variant) {
		pop({
			title: title,
			description: description,
			duration: 5,
			variant: variant
		});
    }

	var title = 'Update failed';
	var description = `Unable to post the reply, queued for later`;

	return (
		<div className="toast-test">
			<button type="button" className="btn btn-primary" onClick={() => addToast(title, description, "primary")}>
				Primary
			</button>
			<button type="button" className="btn btn-secondary" onClick={() => addToast(title, description, "secondary")}>
				Secondary
			</button>
			<button type="button" className="btn btn-success" onClick={() => addToast(title, description, "success")}>
				Success
			</button>
			<button type="button" className="btn btn-danger" onClick={() => addToast(title, description, "danger")}>
				Danger
			</button>
			<button type="button" className="btn btn-warning" onClick={() => addToast(title, description, "warning")}>
				Warning
			</button>
			<button type="button" className="btn btn-info" onClick={() => addToast(title, description, "info")}>
				Info
			</button>
			<button type="button" className="btn btn-light" onClick={() => addToast(title, description, "light")}>
				Light
			</button>
			<button type="button" className="btn btn-dark" onClick={() => addToast(title, description, "dark")}>
				Dark
			</button>
		</div>
	);
}

ToastTest.propTypes = {
};

ToastTest.defaultProps = {
}

ToastTest.icon='align-center'; // fontawesome icon
