import Form from 'UI/Form';
import Input from 'UI/Input';
import Canvas from 'UI/Canvas';
import Modal from 'UI/Modal';
import isNumeric from 'UI/Functions/IsNumeric';
import getAutoForm from 'Admin/Functions/GetAutoForm';
import webRequest from 'UI/Functions/WebRequest';
import formatTime from "UI/Functions/FormatTime";
import Tile from 'Admin/Tile';
import { useSession, RouterConsumer } from 'UI/Session';

var locales = null;
var defaultLocale = 1;

/**
 * Used to automatically generate forms used by the admin area based on fields from your entity declarations in the API.
 * To use this, use AutoService/ AutoController.
 * Most modules do this, so check any existing one for some examples.
 */


export default function AutoForm(props){
    var {session, setSession} = useSession();
    return <AutoFormInternal {...props} session={session} setSession={setSession} />;
}

class AutoFormInternal extends React.Component {

	constructor(props) {
		super(props);
		
		var locale = defaultLocale.toString(); // Always force EN locale if it's not specified.
		
		var localeFromUrl = false;
		if (location && location.search) {
			var query = {};
			location.search.substring(1).split('&').forEach(piece => {
				var term = piece.split('=');
				query[term[0]] = decodeURIComponent(term[1]);
			});
			
			if (query.lid) {
				locale = query.lid;
				localeFromUrl = true;
			}
		}

		this.state = {
			submitting: false,
			locale,
			updateCount: 0
		};
	}

	componentWillReceiveProps(props) {
		this.load(props);
	}

	componentDidMount() {
		this.load(this.props);
	}
	
	loadField(props) {
		var value = {};
		
		try{
			value = JSON.parse(props.defaultValue || props.value);
		}catch(e){
			console.log(e);
		}
		
		getAutoForm(props.formType || 'content', (props.formName || '').toLowerCase()).then(formData => {
			this.setState({ value, fields: formData ? formData.canvas: {content: 'Form load error - the form type was not found.'} });
		});
	}
	
	load(props) {
		if (!props.endpoint) {
			if(props.name){
				this.loadField(props);
			}
			return;
		}

		var createSuccess = false;
		var revisionId = 0;
		var locale = this.state.locale;
		
		if (location && location.search) {
			var query = {};
			location.search.substring(1).split('&').forEach(piece => {
				var term = piece.split('=');
				query[term[0]] = decodeURIComponent(term[1]);
			});

			if (query.created) {
				createSuccess = true;
				delete query.created;
			}

			if (query.revision) {
				revisionId = parseInt(query.revision);
				delete query.revision;
			}
			
			if (query.lid) {
				locale = query.lid;
				delete query.lid;
			}
		}

		if (props.endpoint == this.state.endpoint && props.id == this.state.id && revisionId == this.state.revisionId && locale == this.state.locale) {
			return;
		}
		
		getAutoForm(props.formType || 'content', (props.endpoint || '').toLowerCase()).then(formData => {

			var supportsRevisions = formData && formData.form && formData.form.supportsRevisions;
			var isLocalized = formData && formData.form && formData.form.fields && formData.form.fields.find(fld => fld.data.localized);

			// build up master list of locales
			if (isLocalized) {

				// regional admins can be restricted to a sub set of locales
				// todo expand on locales/users in admin to simplify the UX 
				var hasRestrictedLocales = false;
				if (props.session.role && props.session.role.name == 'Member') {
					if (props.session.region && props.session.region.regionLocales && props.session.region.regionLocales.length > 0) {
						hasRestrictedLocales = true;
					}
				}

				var userLocaleIds = [];
				if (hasRestrictedLocales) {
					props.session.region.regionLocales.map(function (locale, i) {
						if (! userLocaleIds.includes(locale.id)) {
							userLocaleIds.push(locale.id);
						}
					});
					if (userLocaleIds.length > 0 && ! userLocaleIds.includes(defaultLocale)) {
						userLocaleIds.push(defaultLocale);
					}
				}				

				locales = [];
 				webRequest('locale/list').then(resp => {
					if (userLocaleIds.length == 0) {
						locales = resp.json.results;
					} else {
						resp.json.results.map(function (locale, i) {
							if (userLocaleIds.includes(locale.id)) {
								locales.push(locale);
							}
						});
					}
					this.setState({});
				})
			}

			if (formData) {
				// Slight remap to canvas friendly structure:
				this.setState({ fields: formData.canvas, isLocalized, supportsRevisions });
			} else {
				this.setState({ failed: true });
			}
		});
		
		if (this.state.fieldData) {
			this.setState({ fieldData: null });
		}
		var isEdit = isNumeric(props.id);
		var fieldData = undefined;

		if (isEdit) {
			// We always force locale:
			var opts = { locale, includes: '*' };

			// Get the values we're editing:
			var url = revisionId ? props.endpoint + '/revision/' + revisionId : props.endpoint + '/' + props.id;
			webRequest(url, null, opts).then(response => {

				this.setState({ fieldData: response.json, createSuccess });

			});
		} else if (query) {

			// Anything else in the query string is the default fieldData:
			fieldData = query;

		}

		this.setState({
			endpoint: props.endpoint,
			id: props.id,
			revisionId,
			locale,
			fieldData
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

	confirmDelete(pageState, setPage) {
		this.setState({
			confirmDelete: false,
			deleting: true
		});

		webRequest(
			this.state.revisionId ? (this.props.endpoint + '/revision/' + this.state.revisionId) : (this.props.endpoint + '/' + this.props.id),
			null,
			{ method: 'delete' }
		).then(response => {
			var parts = pageState.page.url.split('/'); // e.g. ['', 'en-admin', 'pages', '1']

			// Go to root parent page:
			var target = this.props.deletePage;
			if (!target || !target.length) {
				parts = parts.slice(0, 3); // e.g. ['en-admin', 'pages']. will always go to the root.
				target = parts.join('/');
			} else {
				target = '/' + parts[1] + '/' + target;
			}
			
			setPage(target);

		}).catch(e => {
			console.error(e);
			this.setState({
				deleting: false,
				deleteFailed: true
			});
		});
	}
	
	renderConfirmDelete(pageState, setPage){
		return <Modal visible onClose={() => this.cancelDelete()}>
				<p>
					Are you sure you want to delete this?
				</p>
				<div>
					<Input inline type="button" className="btn btn-danger" onClick={() => this.confirmDelete(pageState, setPage)}>Yes, delete it</Input>
					<Input inline type="button" className="btn btn-secondary" style={{ marginLeft: '10px' }} onClick={() => this.cancelDelete()}>Cancel</Input>
				</div>
		</Modal>;
	}
	
	capitalise(name){
		return name && name.length ? name.charAt(0).toUpperCase() + name.slice(1) : "";
	}
	
	render(){
		if(this.props.name){
			// Render as an input within some other form.
			return <div>
				<Input type='hidden' label={this.props.label} name={this.props.name} inputRef={ir => {
					this.ir = ir;
					if(ir){
						ir.onGetValue = (val, ref) => {
							if(ref != this.ir){
								return;
							}
							return JSON.stringify(this.state.value);
						};
					}
				}} />
				{this.renderFormFields()}
			</div>
		}
		
		return <RouterConsumer>{(pageState, setPage) => this.renderIntl(pageState, setPage)}</RouterConsumer>;
	}
	
	renderFormFields() {
		const { locale } = this.state;
		
		return <Canvas key = {this.state.updateCount} onContentNode={contentNode => {
			var content = this.state.value || this.state.fieldData;

			// setup the hint prompts even if no data 
			if (contentNode.data && contentNode.data.name) {
				var data = contentNode.data;
		
				if(data.hint){
					var hint = <i style="color:lightgreen" className='fa fa-lg fa-question-circle hint-field-label' title={data.hint}/>;
					
					if(Array.isArray(data.label)){
						data.label.push(hint);
					}else{
						data.label = [(data.label || ''), hint];
					}
				}
			}

			if (!contentNode.data || !contentNode.data.name || !content || contentNode.data.autoComplete == 'off') {
				return;
			}

			var data = contentNode.data;

			if(!data.localized && locale != '1'){
				// Only default locale can show non-localised fields. Returning a null will ignore the contentNode.
				return null;
			}

			// Show translation globe icon alongside the label when we have data 
			if (data.localized) {
				var localised = <i style="color:blue" className='fa fa-lg fa-globe-europe localized-field-label' />

				if(Array.isArray(data.label)){
					data.label.splice(1,0,localised);
				}else{
					data.label = [(data.label || ''), localised];
				}
			}
		
			data.currentContent = content;
			data.autoComplete = 'off';
			data.onChange = (e) => {
				// Input field has changed. Update the content object so any redraws are reflected.
				var val = e.target.value;
				switch (data.type) {
					case 'checkbox':
					case 'radio':
						val = e.target.checked;
						break;
					case 'canvas':
						val = e.json;
						break;
				}
				content[data.name] = val;
			};

			var value = content[data.name];
			if (value !== undefined) {
				if (data.name == "createdUtc") {
					data.defaultValue = formatTime(value);
				} else {
					data.defaultValue = value;
				}
			}
		}}>
			{this.state.fields}
		</Canvas>
	}
	
	renderIntl(pageState, setPage) {
		var isEdit = isNumeric(this.props.id);

		const { revisionId, locale } = this.state;

		if (this.state.failed) {
			var ep = this.props.endpoint || '';
			return (
				<div className="alert alert-danger">
					{'Oh no! It Looks like this type doesn\'t support the admin panel. Ask a developer to make sure the type name ("' + ep + '") is spelt correctly. The value comes from the page config of this page, and the type name should match the name of the entity in the API. Case doesn\'t matter.'}
				</div>
			);
		}

		if (!this.state.fields || (isEdit && !this.state.fieldData) || this.state.deleting) {
			return "Loading..";
		}

		// Publish actions.
		var endpoint = this.props.endpoint + "/";
		var parsedId = parseInt(this.props.id);

		if (isEdit && parsedId) {
			endpoint += this.props.id;
		}
		
		return (
			<div className="auto-form">
				<Tile className="auto-form-header">
					{isEdit && <button className="btn btn-danger" style={{ float: 'right' }} onClick={() => this.startDelete()}>Delete</button>}
					<Input inline type="button" disabled={this.state.submitting} onClick={() => {
						this.draftBtn = false;
						this.form.submit();
					}}>
						{isEdit ? "Save and Publish" : "Create"}
					</Input>
					{this.state.supportsRevisions && (
						<Input inline type="button" className="btn btn-primary createDraft" onClick={() => {
							this.draftBtn = true;
							this.form.submit();
						}} disabled={this.state.submitting}>
							{isEdit ? "Save Draft" : "Create Draft"}
						</Input>
					)}
					{
						this.state.editFailure && (
							<div className="alert alert-danger mt-3 mb-1">
								Something went wrong whilst trying to save your changes - your device might be offline, so check your internet connection and try again.
							</div>
						)
					}
					{
						this.state.editSuccess && (
							<div className="alert alert-success mt-3 mb-1">
								Your changes have been saved
							</div>
						)
					}
					{
						this.state.createSuccess && (
							<div className="alert alert-success mt-3 mb-1">
								Created successfully
							</div>
						)
					}
					{
						this.state.deleteFailure && (
							<div className="alert alert-danger mt-3 mb-1">
								Something went wrong whilst trying to delete this - your device might be offline, so check your internet connection and try again.
							</div>
						)
					}
				</Tile>
				<Tile>
					<p>
						<a href={'/en-admin/' + this.props.endpoint}>{this.capitalise(this.props.plural)}</a> &gt; {isEdit ? <span>
							{`Editing ${this.props.singular} #` + this.state.id + ' '}
							{(this.state.fieldData && this.state.fieldData.isDraft) && (
								<span className="is-draft">(Draft)</span>
							)}
							</span> : 'Add new'}
					</p>
					{
						isEdit && this.state.isLocalized && locales.length > 1 && <div>
							<Input label="Select Locale" type="select" name="locale" value={locale} onChange={
								e => {
									// Set the locale and clear the fields/ endpoint so we can load the localized info instead:
									/*
									this.setState({
										locale: e.target.value,
										endpoint: null,
										fields: null
									}, () => {
										// Load now:
										this.load(this.props);
									});
									*/
									
									var url = location.pathname + '?lid=' + e.target.value;
									if(this.state.revisionId){
										url+='&revision=' + this.state.revisionId;
									}
									
									setPage(url);
								}
							}>
								{locales.map(loc => <option value={loc.id} selected={loc.id == locale}>{loc.name + (loc.id == '1' ? ' (Default)': '')}</option>)}
							</Input>
						</div>
					}
					<Form formRef={r=>this.form=r} autoComplete="off" locale={locale} action={endpoint}
						onValues={values => {
							if (this.draftBtn) {
								// Set content ID if there is one already:
								if (isEdit && parsedId) {
									values.id = parsedId;
								}

								// Create a draft:
								values.setAction(this.props.endpoint + "/draft");
							} else {
								// Potentially publishing a draft.
								if (this.state.revisionId) {
									// Use the publish EP.
									values.setAction(this.props.endpoint + "/publish/" + this.state.revisionId);
								}
							}

							this.setState({ editSuccess: false, editFailure: false, createSuccess: false, submitting: true });
							return values;
						}}

						onFailed={
							response => {
								this.setState({ editFailure: true, createSuccess: false, submitting: false });
							}
						}

						onSuccess={
							response => {
								var state = pageState;
								var {locale} = this.state;
								
								if(this._timeout){
									clearTimeout(this._timeout);
								}
								
								this._timeout = setTimeout(() => {
									this.setState({editSuccess: false, createSuccess: false});
								}, 3000);
								
								if (isEdit) {
									
									this.setState({ editFailure: false, editSuccess: true, createSuccess: false, submitting: false, fieldData: response, updateCount: this.state.updateCount + 1 });

									if (state && state.page && state.page.url) {
										var parts = state.page.url.split('/');
										parts.pop();
										parts.push(response.id);

										if (response.revisionId) {
											// Saved a draft

											var newUrl = parts.join('/') + '?revision=' + response.revisionId + '&lid=' + locale;

											if (!this.state.revisionId) {
												newUrl += '&created=1';
											}

											// Go to it now:
											setPage(newUrl);

										} else if (response.id != this.state.id || this.state.revisionId) {
											// Published content from a draft. Go there now.
											setPage(parts.join('/') + '?published=1&lid=' + locale);
										}
									}
								} else {
									this.setState({ editFailure: false, submitting: false, fieldData: response, updateCount: this.state.updateCount+1 });
									if (state && state.page && state.page.url) {
										var parts = state.page.url.split('/');
										parts.pop();
										parts.push(response.id);

										if (response.revisionId) {
											// Created a draft
											setPage(parts.join('/') + '?created=1&revision=' + response.revisionId + '&lid=' + locale);
										} else {
											setPage(parts.join('/') + '?created=1&lid=' + locale);
										}
									}
								}
							}
						}
					>
						{this.props.renderFormFields ? this.props.renderFormFields(this.state) : this.renderFormFields()}
					</Form>
				</Tile>
				{this.state.confirmDelete && this.renderConfirmDelete(pageState, setPage)}
			</div>
		);
	}

}