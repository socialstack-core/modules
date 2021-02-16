/*
	A bubble like block message. Not a popup or toast message (although it can be used as either via nesting).
*/

export default Alert = () => {
	let alertClass = "";
	let alertIcon = "";

	switch (props.type) {
		case "info":
			alertClass = 'alert alert-info';
			alertIcon = 'fas fa-info-circle';
			break;

		case "fail":
		case "failure":
		case "failed":
		case "error":
			alertClass = 'alert alert-danger';
			alertIcon = 'fas fa-times-circle';
			break;

		case "warn":
		case "warning":
			alertClass = 'alert alert-warning';
			alertIcon = 'fas fa-exclamation-triangle';
			break;

		default:
		case "success":
		case "successful":
		case "ok":
		case "good":
			alertClass = 'alert alert-success';
			alertIcon = 'fas fa-check-circle';
			break;
	}

	return (
		<div className={alertClass}>
			<i className={alertIcon} />
			{props.children}
		</div>
	);
}

Alert.propTypes = {
    children: {default: 'My New Alert'},
	type: ['error', 'warning', 'success', 'info']
};

Alert.icon='exclamation-circle';