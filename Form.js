import submitForm from 'UI/Functions/SubmitForm';
import mapUrl from 'UI/Functions/MapUrl';
import omit from 'UI/Functions/Omit';

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
				{...(omit(this.props, ['action', 'method', 'onSuccess', 'onFailed', 'onValues', 'children']))}
			>
				{this.props.children}
			</form>
		);
	}
	
}