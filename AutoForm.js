import Form from 'UI/Form';
import Input from 'UI/Input';
import Canvas from 'UI/Canvas';
import isNumeric from 'UI/Functions/IsNumeric';
import getUrl from 'UI/Functions/Url';
import getAutoForm from 'Admin/Functions/GetAutoForm';
import webRequest from 'UI/Functions/WebRequest';
import formatTime from "UI/Functions/FormatTime";

var locales = null;

/**
 * Used to automatically generate forms used by the admin area based on fields from your AutoForm declarations in the API.
 * To use this, your endpoints must have add/ update and must also accept an AutoForm<> model. 
 * Most modules do this, so check any existing one for some examples.
 */
export default class AutoForm extends React.Component {
	
	constructor(props){
		super(props);
		
		this.state = {
			submitting: false,
			locale: '1' // Always force EN locale if it's not specified.
		};
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
		
		var createSuccess = global.location && global.location.search == '?created=1';
		
		getAutoForm(props.endpoint).then(formData => {
			
			var isLocalized = formData.form && formData.form.fields && formData.form.fields.find(fld => fld.data.localized);
			
			if(isLocalized && !locales){
				locales = [];
				webRequest('locale/list').then(resp => {
					locales = resp.json.results;
					console.log(locales);
					this.setState({});
				})
			}
			
			if(formData){
				// Slight remap to canvas friendly structure:
				this.setState({fields: formData.canvas, isLocalized});
			}else{
				this.setState({failed: true});
			}
		});
		
		var isEdit = isNumeric(props.id);
		
		if(isEdit){
			
			// We always force locale:
			var opts = {locale: this.state.locale};
			
			// Get the values we're editing:
			webRequest(props.endpoint + '/' + props.id, null, opts).then(response => {
				
				this.setState({fieldData: response.json, createSuccess});
				
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
		
		webRequest(this.props.endpoint + '/' + this.props.id, null, {method: 'delete'}).then(response => {
			
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
			<div className="auto-form">
				{
					this.state.isLocalized && locales.length > 1 && <div>
						<Input label ="Select Locale" type="select" name="locale" onChange={
							e => {
								// Set the locale and clear the fields/ endpoint so we can load the localized info instead:
								this.setState({
									locale: e.target.value,
									endpoint: null,
									fields: null
								}, () => {
									// Load now:
									this.load(this.props);
								});
							}
						}>
							{locales.map(locale => <option value={locale.id} selected={locale.id == this.state.locale}>{locale.name}</option>)}
						</Input>
					</div>
				}
				<Form autoComplete="off" locale={this.state.locale} action={this.props.endpoint + "/" + (isEdit ? this.props.id : "")}
				onValues={values => {
					this.setState({editSuccess: false, createSuccess: false, submitting: true});
					return values;
				}}
				
				onFailure={
					response => {
						this.setState({editFailure: true, createSuccess: false, submitting: false});
					}
				}
				
				onSuccess={
					response => {
						if(isEdit){
							this.setState({editSuccess: true, createSuccess: false, submitting: false});
						}else{
							this.setState({submitting: false});
							var state = global.pageRouter.state;
							if(state && state.page && state.page.url){
								var parts = state.page.url.split('/');
								parts.pop();
								parts.push(response.id);
								global.pageRouter.go('/' + parts.join('/') + '?created=1');
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
							
							if(data.localized){
								// Show globe icon alongside the label:
								data.label = [(data.label || ''), <i className='fa fa-globe-europe localized-field-label' />];
							}
							
							data.autoComplete = 'off';
							data.onChange=(e) => {
								// Input field has changed. Update the content object so any redraws are reflected.
								var t = e.target.type;
								content[data.name] = (t == 'checkbox' || t == 'radio') ? e.target.checked : e.target.value;
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
						this.state.createSuccess && (
							<div className="alert alert-success">
								Created successfully
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
			</div>
		);
	}
	
}