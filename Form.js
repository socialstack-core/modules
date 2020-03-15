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
		var {
			action,
			locale,
			onValues,
			onSuccess,
			onFailed
		} = this.props;
		
		if(!action){
			throw new Error("<Form> component requires action prop");
		}
		
		var requestOpts = null;
		
		if(locale){
			// Forcing a particular locale:
			requestOpts = {locale};
		}
		
		return (
			<form
				onSubmit={e=>submitForm(e, {
					onValues,
					onFailed: (e) => {
						onFailed && onFailed(e);
					},
					onSuccess,
					requestOpts
				})}
				action={mapUrl(action)}
				method={this.props.method || "post"}
				{...(omit(this.props, ['action', 'method', 'onSuccess', 'onFailed', 'onValues', 'children', 'locale']))}
			>
				{this.props.children}
			</form>
		);
	}
	
}