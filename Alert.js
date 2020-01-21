/*
	A bubble like block message. Not a popup or toast message (although it can be used as either via nesting).
*/

export default (props) => {
	
	switch(props.type){
		case "fail":
		case "failure":
		case "failed":
		case "error":
			return <div className="alert alert-danger">{props.children}</div>;
		case "warn":
		case "warning":
			return <div className="alert alert-warning">{props.children}</div>;
		default:
		case "success":
		case "successful":
		case "ok":
		case "good":
			return <div className="alert alert-success">{props.children}</div>;
		break;
	}
}