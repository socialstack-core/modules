import submitForm from 'UI/Functions/SubmitForm';
import mapUrl from 'UI/Functions/MapUrl';

/**
 * Wraps <form> in order to automatically manage setting up default values.
 * You can also directly use form and the Functions/SubmitForm method if you want - use of this component is optional.
 * This component is best used with UI/Input.
 */
export default class Form extends React.Component {
	
	render() {
		if(!this.props.action){
			throw new Error("<Form> component requires action prop");
		}
		return (
			<form
				onSubmit={e=>submitForm(e, {
					onValues: this.props.onValues,
					onFailed: (e) => {
						this.props.onFailed && this.props.onFailed(e);
					},
					onSuccess: this.props.onSuccess
				})}
				action={mapUrl(this.props.action)}
				method={this.props.method || "post"}
				className={this.props.className}
				id={this.props.id}
				name={this.props.name}
			>
				{this.props.children}
			</form>
		);
	}
	
}