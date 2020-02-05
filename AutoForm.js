import Form from 'UI/Form';
import Input from 'UI/Input';
import Canvas from 'UI/Canvas';
import isNumeric from 'UI/Functions/IsNumeric';
import getAutoForm from 'Admin/Functions/GetAutoForm';
import webRequest from 'UI/Functions/WebRequest';
import formatTime from "UI/Functions/FormatTime";

/**
 * Used to automatically generate forms used by the admin area based on fields from your AutoForm declarations in the API.
 * To use this, your endpoints must have add/ update and must also accept an AutoForm<> model. 
 * Most modules do this, so check any existing one for some examples.
 */
export default class AutoForm extends React.Component {
	
	constructor(props){
		super(props);
		
		this.state = {submitting: false};
	}
	
	componentWillReceiveProps(props){
		this.load(props);
	}
	
	componentDidMount(){
		this.load(this.props);
	}
	
	load(props){
		if(!props.endpoint || (props.endpoint == this.state.endpoint && props.id == this.state.id)){
			return;
		}
		
		getAutoForm(props.endpoint).then(formData => {
			
			if(formData){
				// Slight remap to canvas friendly structure:
				this.setState({fields: formData.canvas});
			}else{
				this.setState({failed: true});
			}
		});
		
		var isEdit = isNumeric(props.id);
		
		if(isEdit){
			// Get the values we're editing:
			webRequest(props.endpoint + '/' + props.id).then(response => {
				
				this.setState({fieldData: response.json});
				
			});
		}
		
		this.setState({
			endpoint: props.endpoint,
			id: props.id
		});
	}
	
	render() {
		var isEdit = isNumeric(this.props.id);
		
		if(this.state.failed){
			return (
				<div className="alert alert-danger">
					Oh no! It Looks like this type doesn't support autoform. It must have an endpoint method called Create which accepts a parameter marked with [FromBody] that inherits AutoForm.
				</div>
			);
		}
		
		if(!this.state.fields || (isEdit && !this.state.fieldData)){
			return "Loading..";
		}
		
		return (
			<Form autoComplete="off" action={this.props.endpoint + "/" + (isEdit ? this.props.id : "")}
			onValues={values => {
				this.setState({editSuccess: false, submitting: true});
				return values;
			}}
			
			onFailure={
				response => {
					this.setState({editFailure: true, submitting: false});
				}
			}
			
			onSuccess={
				response => {
					if(isEdit){
						this.setState({editSuccess: true, submitting: false});
					}else{
						var state = global.pageRouter.state;
						if(state && state.page && state.page.url){
							var parts = state.page.url.split('/');
							parts.pop();
							parts.push(response.id);
							global.pageRouter.go('/' + parts.join('/'));
						}
					}
				}
			}
			>
				<Canvas onContentNode={contentNode => {
					if(isEdit && contentNode.data && contentNode.data.name){
						contentNode.data.autoComplete = 'off';
						var value = this.state.fieldData[contentNode.data.name];
						if(value){
							if(contentNode.data.name == "createdUtc"){
								contentNode.data.defaultValue = formatTime(value);
							}
							else {
								contentNode.data.defaultValue = value;
							}
						}
					}
				}}>
					{this.state.fields}
				</Canvas>
				<Input type="submit" disabled={this.state.submitting}>{isEdit ? "Save Changes" : "Create"}</Input>
				{
					this.state.editFailure && (
						<div className="alert alert-danger">
							Something went wrong whilst trying to save your changes - your device might be offline, so check your internet connection and try again.
						</div>
					)
				}
				{
					this.state.editSuccess && (
						<div className="alert alert-success">
							Your changes have been saved
						</div>
					)
				}
			</Form>
		);
	}
	
}