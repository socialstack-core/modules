/*
	A bubble like block message. Not a popup or toast message (although it can be used as either via nesting).
*/

export default class Alert extends React.Component {
	
	render(){
		switch(this.props.type){
			case "fail":
			case "failure":
			case "failed":
			case "error":
				return <div className="alert alert-danger">{this.props.children}</div>;
			case "warn":
			case "warning":
				return <div className="alert alert-warning">{this.props.children}</div>;
			default:
			case "success":
			case "successful":
			case "ok":
			case "good":
				return <div className="alert alert-success">{this.props.children}</div>;
			break;
		}
	}
}

Alert.propTypes = {
	type: ['error', 'warning', 'success'],
	children: true
};

Alert.icon='exclamation-circle';