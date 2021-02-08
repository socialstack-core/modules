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
 * Used to automatically generate forms used by the admin area based on fields from your entity declarations in the API.
 * To use this, use AutoService/ AutoController.
 * Most modules do this, so check any existing one for some examples.
 */
export default class AutoForm extends React.Component {

	constructor(props) {
		super(props);

		this.state = {
			submitting: false,
			locale: '1', // Always force EN locale if it's not specified.
			updateCount: 0
		};
	}

	componentWillReceiveProps(props) {
		this.load(props);
	}

	componentDidMount() {
		this.load(this.props);
	}

	load(props) {
		if (!props.endpoint) {
			return;
		}

		var createSuccess = false;
		var revisionId = 0;
		if (global.location && global.location.search) {
			var query = {};
			global.location.search.substring(1).split('&').forEach(piece => {
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
		}

		if (props.endpoint == this.state.endpoint && props.id == this.state.id && revisionId == this.state.revisionId) {
			return;
		}

		getAutoForm((props.endpoint || '').toLowerCase()).then(formData => {

			var supportsRevisions = formData && formData.form && formData.form.supportsRevisions;
			var isLocalized = formData && formData.form && formData.form.fields && formData.form.fields.find(fld => fld.data.localized);

			if (isLocalized && !locales) {
				locales = [];
				webRequest('locale/list').then(resp => {
					locales = resp.json.results;
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
			var opts = { locale: this.state.locale };

			// Get the values we're editing:
			webRequest(revisionId ? props.endpoint + '/revision/' + revisionId : props.endpoint + '/' + props.id, null, opts).then(response => {

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

	confirmDelete() {
		this.setState({
			confirmDelete: false,
			deleting: true
		});

		webRequest(
			this.state.revisionId ? (this.props.endpoint + '/revision/' + this.state.revisionId) : (this.props.endpoint + '/' + this.props.id),
			null,
			{ method: 'delete' }
		).then(response => {
			var state = global.pageRouter.state;
			var parts = state.page.url.split('/'); // e.g. ['en-admin', 'pages', '1']

			// Go to root parent page:
			var target = this.props.deletePage;
			if (!target || !target.length) {
				parts = parts.slice(0, 2); // e.g. ['en-admin', 'pages']. will always go to the root.
				target = '/' + parts.join('/');
			} else {
				target = '/' + parts[0] + '/' + target;
			}
			global.pageRouter.go(target);

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

		const { revisionId } = this.state;

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
				{(this.state.fieldData && this.state.fieldData.isDraft) && (
					<span className="is-draft">
						Draft
					</span>
				)}
				{
					isEdit && this.state.isLocalized && locales.length > 1 && <div>
						<Input label="Select Locale" type="select" name="locale" onChange={
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
				<Form autoComplete="off" locale={this.state.locale} action={endpoint}
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
							var state = global.pageRouter.state;

							if (isEdit) {
								this.setState({ editFailure: false, editSuccess: true, createSuccess: false, submitting: false, fieldData: response, updateCount: this.state.updateCount + 1 });

								if (state && state.page && state.page.url) {
									var parts = state.page.url.split('/');
									parts.pop();
									parts.push(response.id);

									if (response.revisionId) {
										// Saved a draft

										var newUrl = '/' + parts.join('/') + '?revision=' + response.revisionId;

										if (!this.state.revisionId) {
											newUrl += '&created=1';
										}

										// Go to it now:
										global.pageRouter.go(newUrl);

									} else if (response.id != this.state.id || this.state.revisionId) {
										// Published content from a draft. Go there now.
										global.pageRouter.go('/' + parts.join('/') + '?published=1');
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
										global.pageRouter.go('/' + parts.join('/') + '?created=1&revision=' + response.revisionId);
									} else {
										global.pageRouter.go('/' + parts.join('/') + '?created=1');
									}
								}
							}
						}
					}
				>
					<Canvas key = {this.state.updateCount} onContentNode={contentNode => {
						var content = this.state.fieldData;
						if (!contentNode.data || !contentNode.data.name || !content || contentNode.data.autoComplete == 'off') {
							return;
						}

						var data = contentNode.data;

						if (data.type == 'canvas' && content.pageId) {
							// Ref the content:
							data.contentType = content.type;
							data.contentId = content.id;
							data.onPageUrl = getUrl(content);
						}

						if (data.localized && !Array.isArray(data.label)) {
							// Show globe icon alongside the label:
							data.label = [(data.label || ''), <i className='fa fa-globe-europe localized-field-label' />];
						}
						
						if(data.hint){
							var hint = <i className='fa fa-question-circle hint-field-label' title={data.hint}/>;
							
							if(Array.isArray(data.label)){
								data.label.push(hint);
							}else{
								data.label = [(data.label || ''), hint];
							}
						}
						
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
					{isEdit && (
						this.state.confirmDelete ? (
							<div style={{ float: 'right' }}>
								Are you sure you want to delete this?
								<div>
									<Input inline type="button" className="btn btn-danger" onClick={() => this.confirmDelete()}>Yes, delete it</Input>
									<Input inline type="button" className="btn btn-secondary" style={{ marginLeft: '10px' }} onClick={() => this.cancelDelete()}>Cancel</Input>
								</div>
							</div>
						) : (
								<Input type="button" groupClassName="offset-2 auto-form-footer" className="btn btn-danger" style={{ float: 'right' }} onClick={() => this.startDelete()}>Delete</Input>
							)
					)}
					<div className="auto-form-create-options">
						<Input inline type="submit" disabled={this.state.submitting} onMouseDown={() => { this.draftBtn = false }}>
							{isEdit ? "Save and Publish" : "Create"}
						</Input>
						{this.state.supportsRevisions && (
							<Input inline type="submit" className="btn btn-primary createDraft" onMouseDown={() => { this.draftBtn = true }} disabled={this.state.submitting}>
								{isEdit ? "Save Draft" : "Create Draft"}
							</Input>
						)}
					</div>
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
				</Form>
			</div>
		);
	}

}