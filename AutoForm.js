import Form from 'UI/Form';
import Input from 'UI/Input';
import Canvas from 'UI/Canvas';
import isNumeric from 'UI/Functions/IsNumeric';
import getUrl from 'UI/Functions/Url';
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
	
	startDelete() {
		this.setState({
			confirmDelete: true
		});
	}
	
	cancelDelete() {
		this.setState({
			confirmDelete: false
		});
	}
	
	confirmDelete() {
		this.setState({
			confirmDelete: false,
			deleting: true
		});
		
		webRequest(this.props.endpoint + '/' + this.props.id, {method: 'delete'}).then(response => {
			
			// Go to root parent page:
			var state = global.pageRouter.state;
			var parts = state.page.url.split('/'); // e.g. ['en-admin', 'pages', '1']
			parts = parts.slice(0, 2); // e.g. ['en-admin', 'pages']. will always go to the root.
			global.pageRouter.go('/' + parts.join('/'));
			
		}).catch(e => {
			console.error(e);
			this.setState({
				deleting: false,
				deleteFailed: true
			});
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
		
		if(!this.state.fields || (isEdit && !this.state.fieldData) || this.state.deleting){
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
						this.setState({submitting: false});
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
						var data = contentNode.data;
						var content = this.state.fieldData;
						
						if(data.type == 'canvas' && content.pageId){
							// Ref the content:
							data.contentType = content.type;
							data.contentId = content.id;
							data.onPageUrl = getUrl(content);
						}
						
						data.autoComplete = 'off';
						data.onChange=(e) => {
							// Input field has changed. Update the content object so any redraws are reflected.
							content[data.name] = e.target.value;
						};
						
						var value = content[data.name];
						if(value){
							if(data.name == "createdUtc"){
								data.defaultValue = formatTime(value);
							} else {
								data.defaultValue = value;
							}
						}
					}
				}}>
					{this.state.fields}
				</Canvas>
				{isEdit && (
					this.state.confirmDelete ? (
						<div style={{float: 'right'}}>
							Are you sure you want to delete this?
							<div>
								<Input inline type="button" className="btn btn-danger" onClick={() => this.confirmDelete()}>Yes, delete it</Input>
								<Input inline type="button" className="btn btn-secondary" style={{marginLeft: '10px'}} onClick={() => this.cancelDelete()}>Cancel</Input>
							</div>
						</div>
					) : (
						<Input type="button" className="btn btn-danger" style={{float: 'right'}} onClick={() => this.startDelete()}>Delete</Input>
					)
				)}
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
				{
					this.state.deleteFailure && (
						<div className="alert alert-danger">
							Something went wrong whilst trying to delete this - your device might be offline, so check your internet connection and try again.
						</div>
					)
				}
			</Form>
		);
	}
	
}