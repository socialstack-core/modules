import submitForm from 'UI/Functions/SubmitForm';
import mapUrl from 'UI/Functions/MapUrl';
import omit from 'UI/Functions/Omit';
import Spacer from 'UI/Spacer';
import Alert from 'UI/Alert';
import Loading from 'UI/Loading';
import Input from 'UI/Input';

/**
 * Wraps <form> in order to automatically manage setting up default values.
 * You can also directly use form and the Functions/SubmitForm method if you want - use of this component is optional.
 * This component is best used with UI/Input.
 */
export default class Form extends React.Component {
	
	constructor(props){
		super(props);
		this.state={};
		this.onSubmit=this.onSubmit.bind(this);
	}
	
	onSubmit(e){
		if(!e){
			e={
				target:this.formEle,
				preventDefault:()=>{}
			};
		}
		
		var {
			locale,
			onValues,
			onSuccess,
			onFailed,
			requestOpts,
			action
		} = this.props;
		
		if(!requestOpts){
			requestOpts = {};
		}
		
		if(locale){
			// Forcing a particular locale:
			requestOpts.locale=locale;
		}
		
		return submitForm(e, {
			onValues: (values, evt) => {
				if(!action){
					values.setAction(null);
				}
				
				this.setState({
					loading: true
				});
				return onValues ? onValues(values, evt) : values;
			},
			onFailed: (r,v,evt) => {
				this.setState({
					loading: undefined,
					failed: true,
					failure: r
				});
				onFailed && onFailed(r,v,evt);
			},
			onSuccess: (r, v, evt) => {

				if (this.props.resetOnSubmit) {
					this.formEle.reset();
				}

				this.setState({
					loading: undefined,
					failed: false,
					success: true
				});
				onSuccess && onSuccess(r,v,evt);
			},
			requestOpts
		});
	}
	
	render() {
		var {
			action,
			loadingMessage,
			submitLabel,
			failedMessage,
			successMessage
		} = this.props;
		
		var showFormResponse = !!(loadingMessage || submitLabel || failedMessage);
		
		return (
			<form
				onSubmit={this.onSubmit}
				ref={f=>{
					this.formEle=f;
					if(f){
						f.submit = this.onSubmit;
					}
					this.props.formRef && this.props.formRef(f);
				}}
				action={mapUrl(action)}
				method={this.props.method || "post"}
				{...(omit(this.props, ['action', 'method', 'onSuccess', 'onFailed', 'onValues', 'children', 'locale']))}
			>
				{this.props.children}
				{showFormResponse && (
					<div className="form-response">
						<Spacer />
						{
							this.state.failed && failedMessage && (
								<div className="form-failed">
									<Alert type="error">
										{typeof failedMessage == "function" ? failedMessage(this.state.failure) : (
											(this.state.failure && this.state.failure.message) || failedMessage
										)}
									</Alert>
									<Spacer />
								</div>
							)
						}
						{
							this.state.success && successMessage && (
								<div className="form-success">
									<Alert type="success">
										{typeof successMessage == "function" ? successMessage() : successMessage}
									</Alert>
									<Spacer />
								</div>
							)
						}
						{
							submitLabel && <Input type="submit" label={submitLabel} disabled={this.state.loading} />
						}
						{
							this.state.loading && loadingMessage && (
								<div className="form-loading">
									<Spacer />
									<Loading message={loadingMessage}/>
								</div>
							)
						}
					</div>
				)}
			</form>
		);
	}
	
}

Form.propTypes={
	action: 'string'
};

Form.icon = 'question';