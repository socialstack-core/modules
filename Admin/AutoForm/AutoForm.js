import Form from 'UI/Form';
import Input from 'UI/Input';
import Canvas from 'UI/Canvas';
import Loading from 'UI/Loading';
import Modal from 'UI/Modal';
import ConfirmModal from 'UI/Modal/ConfirmModal';
import isNumeric from 'UI/Functions/IsNumeric';
import getAutoForm from 'Admin/Functions/GetAutoForm';
import webRequest from 'UI/Functions/WebRequest';
import formatTime from "UI/Functions/FormatTime";
import CanvasEditor from "Admin/CanvasEditor";
import getBuildDate from 'UI/Functions/GetBuildDate';
import { useSession, RouterConsumer, Router } from 'UI/Session';

var locales = null;
var defaultLocale = 1;

/**
 * Used to automatically generate forms used by the admin area based on fields from your entity declarations in the API.
 * To use this, use AutoService/ AutoController.
 * Most modules do this, so check any existing one for some examples.
 */


export default function AutoForm(props) {
	var { session, setSession } = useSession();
	return <AutoFormInternal {...props} session={session} setSession={setSession} />;
}

class AutoFormInternal extends React.Component {

	constructor(props) {
		super(props);

		var locale = defaultLocale.toString(); // Always force EN locale if it's not specified.
		var context = null;

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

			if (query.context) {
				context = query.context;
			}
		}

		this.state = {
			submitting: false,
			locale,
			context,
			updateCount: 0
		};

		this.beforeUnload = this.beforeUnload.bind(this);

	}

	beforeUnload(e) {
		if (this.unsavedChanges) {
			e.preventDefault();
			return e.returnValue = 'Unsaved changes - are you sure you want to exit?';
		}
	}

	componentWillReceiveProps(props) {
		this.load(props);
	}

	componentDidMount() {
		this.load(this.props);

		// Add unload handler such that e.g. when the websocket requests a refresh 
		// we'll check if there are any unsaved changes.
		global.window.addEventListener('beforeunload', this.beforeUnload);
	}

	componentWillUnmount() {

		if (!window.SERVER) {
			var html = document.querySelector("html");

			if (html) {
				html.style.setProperty('--admin-feedback-height', '0px');
			}

        }

		global.window.removeEventListener('beforeunload', this.beforeUnload);
	}

	loadField(props) {
		var value = {};

		try {
			value = JSON.parse(props.defaultValue || props.value);
		} catch (e) {
			console.log(e);
		}

		getAutoForm(props.formType || 'content', (props.formName || '').toLowerCase()).then(formData => {
			this.setState({ value, fields: formData ? formData.canvas : { content: 'Form load error - the form type was not found.' } });
		});
	}

	load(props) {
		if (!props.endpoint) {
			if (props.name) {
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

		if (this.state.context) {
			webRequest("page/state", {
				url: this.state.context,
				version: getBuildDate().timestamp
			}).then(res => {
				this.setState({ pageState: { url: this.state.context, ...res.json } });
			});
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
						if (!userLocaleIds.includes(locale.id)) {
							userLocaleIds.push(locale.id);
						}
					});
					if (userLocaleIds.length > 0 && !userLocaleIds.includes(defaultLocale)) {
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

			if (!formData) {
				this.setState({ failed: true });
				return;
			}

			// The fields of this content type are..
			var fields = JSON.parse(formData.canvas);

			// If there is exactly 1 canvas in the field set, or one is marked as the main canvas with [Data("main", "true")]
			// then we'll use the full size panelled canvas editor layout.
			var mainCanvas = null;

			if (Array.isArray(fields)) {
				fields = { content: fields };
			}

			var c = fields.c || fields.content;

			if (Array.isArray(c)) {
				var currentCanvas = null;
				var canvasFields = [];

				for (var i = 0; i < c.length; i++) {
					var field = c[i];
					var data = field.d || field.data;

					if (!data) {
						continue;
					}

					if (data.type == 'canvas' && !data.contentType) {
						field.textonly = data.textonly;
						canvasFields.push(field);
						currentCanvas = field;

						if (data.main) {
							// Definitely the main canvas.
							canvasCount = 1;
							break;
						}
					}
				}

				if (canvasFields.length == 1 && !canvasFields[0].textonly) {
					mainCanvas = currentCanvas;
					this.tryPopulateMainCanvas(this.state.fieldData, mainCanvas);
				}
			}

			// Store in the state:
			this.setState({ fields, isLocalized, supportsRevisions, mainCanvas });
		});

		if (this.state.fieldData) {
			this.setState({ fieldData: null });
		}
		var isEdit = isNumeric(props.id);
		var fieldData = undefined;

		if (isEdit) {
			// We always force locale:
			var opts = { locale, includes: '*,primaryUrl' };

			// Get the values we're editing:
			var url = revisionId ? props.endpoint + '/revision/' + revisionId : props.endpoint + '/' + props.id;
			webRequest(url, null, opts).then(response => {
				var content = response.json;
				var hasPrimaryUrl = false;

				this.tryPopulateMainCanvas(content, this.state.mainCanvas);

				if (content && content.primaryUrl) {
					hasPrimaryUrl = true;
				}

				this.setState({ fieldData: content, createSuccess, hasPrimaryUrl });

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

	applyDefaults(fields, values) {

		var c = fields.c || fields.content;

		if (Array.isArray(c)) {

			for (var i = 0; i < c.length; i++) {
				var field = c[i];
				if (!field) {
					continue;
				}
				var data = field.d || field.data;

				if (!data || !data.name) {
					continue;
				}

				data.defaultValue = values[data.name];
			}
		}

	}

	getCanvasContext(content) {
		var canvasContext = this.props.canvasContext ? this.props.canvasContext : [];
		
		if (canvasContext.length == 0) {
			if (this.state.fields && this.state.fields.content && this.state.fields.content.length > 0) {
				var fields = this.state.fields.content;

				for(let i=0;i<fields.length;i++){
					var field = fields[i];
					var data = field ? field.data : null;

					if (!field || !data) {
						continue;
					}

					if (field.tokeniseable === false || data.contentType || data.type == "canvas") {
						continue;
					}

					var value = (content && content[data.name]) ? content[data.name] : data.defaultValue;
					var label = data.label

					if (label && typeof label != 'string' && label.length && label.length > 0) {
						label = label[0];
					}

					canvasContext.push({
						name: data.name,
						label: label,
						value: value,
						isPrice: data.isPrice
					});
				}
			}
		}

		return canvasContext;
	}

	tryPopulateMainCanvas(content, mainCanvas) {
		if (!content || !mainCanvas) {
			return;
		}
		// Ensure the main canvas node gets populated.
		var data = mainCanvas.data;
		data.currentContent = content;
		data.canvasContext = this.getCanvasContext(content);

		data.onChange = (e) => {
			// Input field has changed. Update the content object so any redraws are reflected.
			var val = e.target.value;
			content[data.name] = e.json;
		};

		data.defaultValue = content[data.name];
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
			if (this.props.onActionComplete) {
				this.props.onActionComplete(null);
				return;
			}

			var parts = window.location.pathname.split('/');

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

	renderConfirmDelete(pageState, setPage) {
		return <>
			<ConfirmModal
				confirmCallback={() => this.confirmDelete(pageState, setPage)} confirmVariant="danger" confirmText={`Yes, delete it`}
				cancelCallback={() => this.cancelDelete()}>
				<p>
					{`Are you sure you wish to delete this?`}
				</p>
			</ConfirmModal>
		</>;
	}

	startSelectParam(param) {
		this.setState({
			selectParam: param
		});
	}

	cancelSelectParam() {
		this.setState({
			selectParam: false
		});
	}

	getParamValue(param) {

		if (!this.state.pageState || !this.state.pageState.tokenNames || !this.state.pageState.tokens) {
			return '';
        }

		var tokenIndex = this.state.pageState.tokenNames.findIndex(name => name == param);

		if (tokenIndex == -1) {
			return '';
		}

		return this.state.pageState.tokens.length > tokenIndex ? this.state.pageState.tokens[tokenIndex] : '';
	}

	updateParamValue(param, value) {
		var tokenIndex = this.state.pageState.tokenNames.findIndex(name => name == param);
		var updatedTokens = this.state.pageState.tokens.map((token, i) => {
			return i === tokenIndex ? value : token;
		});
		var newPageState = this.state.pageState;
		newPageState.tokens = updatedTokens;

		this.setState({
			pageState: newPageState
		});

    }

	renderSelectParam() {
		/* TODO: provide a dropdown list of valid options
		var options = [];

		webRequest(this.state.selectParam + '/list').then(resp => {
			resp.json.results.map((result) => {
				options.push(result.slug);
			});
		});
		 */

		return <>
			<Modal visible onClose={() => this.cancelSelectParam()} title={`Select Parameter`} className="admin-page__select-param-modal">
				{/*
				<label htmlFor="param_options" className="form-label">
					{`Please select a value for `}
					<code>{this.state.selectParam}</code>:
				</label>
				<Input type="select" id="param_options">
					{options.map(option => <option value={option}>{option}</option>)}
				</Input>
				*/}

				<label htmlFor="param_options" className="form-label">
					{`Please enter a value for `}
					<code>{this.state.selectParam}</code>:
				</label>
				<Input id="param_options" defaultValue={this.getParamValue(this.state.selectParam)} />
				<div className="internal-footer">
					<button type="button" className="btn btn-primary" onClick={() => {
						this.updateParamValue(this.state.selectParam, document.getElementById("param_options").value);
						this.cancelSelectParam();
					}}>
						{`Save`}
					</button>
					<button type="button" className="btn btn-outline-primary" onClick={() => this.cancelSelectParam()}>
						{`Cancel`}
					</button>
				</div>
			</Modal>
		</>;
	}

	capitalise(name) {
		return name && name.length ? name.charAt(0).toUpperCase() + name.slice(1) : "";
	}

	render() {
		if (this.props.name) {
			// Render as an input within some other form.
			return <div>
				<Input type='hidden' label={this.props.label} name={this.props.name} inputRef={ir => {
					this.ir = ir;
					if (ir) {
						ir.onGetValue = (val, ref) => {
							if (ref != this.ir) {
								return;
							}
							return JSON.stringify(this.state.value);
						};
					}
				}} />
				{this.renderFormFields()}
			</div>
		}

		if (this.state.context && !this.state.pageState) {
			// Loading page state for the context.
			return <Loading />;
		}

		return <RouterConsumer>{(pageState, setPage) => {
			return <Router.Provider
				value={{
					canGoBack: () => false,
					pageState: this.state.pageState || {url: ''},
					setPage
				}}
			>
				{this.renderIntl(pageState, setPage)}
			</Router.Provider>;
		}}</RouterConsumer>;
	}

	renderFormFields() {
		const { locale, mainCanvas } = this.state;

		return <Canvas key={this.state.updateCount} onContentNode={contentNode => {
			var content = this.state.value || this.state.fieldData;

			// setup the hint prompts even if no data 
			if (contentNode.data && contentNode.data.name) {
				var data = contentNode.data;

				if (data.hint) {
					var hint = <i className='fa fa-lg fa-question-circle hint-field-label' title={data.hint} />;

					if (Array.isArray(data.label)) {
						data.label.push(hint);
					} else {
						data.label = [(data.label || ''), hint];
					}
				}
			}

			if (mainCanvas && contentNode && contentNode.data && mainCanvas.data.name == contentNode.data.name) {
				// Omit the main canvas from the rest of the fields.
				return null;
			}

			if (!contentNode.data || !contentNode.data.name || !content || contentNode.data.autoComplete == 'off') {
				return;
			}

			var data = contentNode.data;

			if (!data.localized && locale != '1') {
				// Only default locale can show non-localised fields. Returning a null will ignore the contentNode.
				return null;
			}

			// Show translation globe icon alongside the label when we have data 
			if (data.localized) {
				var localised = <i className='fa fa-lg fa-globe-europe localized-field-label' />

				if (Array.isArray(data.label)) {
					data.label.splice(1, 0, localised);
				} else {
					data.label = [(data.label || ''), localised];
				}
			}

			data.currentContent = content;
			data.canvasContext = this.getCanvasContext(content);
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
				this.unsavedChanges = true;

				var newFields = [...this.state.fields.content];

				newFields.forEach(field => {
					field.data.currentContent = {...content};
					field.data.canvasContext = this.getCanvasContext(content)
				});

				this.setState({fields: { ...this.state.fields, content: newFields }});
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

		const { revisionId, locale, mainCanvas } = this.state;

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

		var feedback = <>
			{
				this.state.editFailure && (
					<div className="alert alert-danger">
						{`Something went wrong whilst trying to save your changes - your device might be offline, so check your internet connection and try again.`}
					</div>
				)
			}
			{
				this.state.editSuccess && (
					<div className="alert alert-success">
						{`Your changes have been saved`}
					</div>
				)
			}
			{
				this.state.createSuccess && (
					<div className="alert alert-success">
						{`Created successfully`}
					</div>
				)
			}
			{
				this.state.deleteFailure && (
					<div className="alert alert-danger">
						{`Something went wrong whilst trying to delete this - your device might be offline, so check your internet connection and try again.`}
					</div>
				)
			}
		</>;

		var controls = <>
			{isEdit && <>
				<button className="btn btn-danger" type="button" onClick={e => {
					e.preventDefault();
					this.startDelete();
				}}>
					{`Delete`}
				</button>
			</>}
			{this.state.supportsRevisions && (
				<Input inline type="button" className="btn btn-outline-primary createDraft" onClick={() => {
					this.draftBtn = true;
					this.form.submit();
				}} disabled={this.state.submitting}>
					{isEdit ? `Save Draft` : `Create Draft`}
				</Input>
			)}
			<Input inline type="button" disabled={this.state.submitting} onClick={() => {
				this.draftBtn = false;
				this.form.submit();
			}}>
				{isEdit ? `Save and Publish` : `Create`}
			</Input>
		</>;

		if (this.props.modalCancelCallback) {
			controls = <>
				<button className="btn btn-outline-primary" onClick={this.props.modalCancelCallback}>
					{`Cancel`}
				</button>
				<Input inline type="button" disabled={this.state.submitting} onClick={() => {
					this.draftBtn = false;
					this.form.submit();
				}}>
					{`Save`}
				</Input>
			</>;
		}

		var onValues = values => {
			if (this.props.values && typeof this.props.values === 'object' && !Array.isArray(this.props.values) && this.props.values !== null) {
				values = { ...values, ...this.props.values };
			}

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
		};

		var onFailed = response => {
			this.setState({ editFailure: true, createSuccess: false, submitting: false });
		};

		var onSuccess = response => {
			var state = pageState;
			var { locale } = this.state;

			this.unsavedChanges = false;

			if (this._timeout) {
				clearTimeout(this._timeout);
			}

			this._timeout = setTimeout(() => {
				this.setState({ editSuccess: false, createSuccess: false });
			}, 3000);

			if (isEdit) {
				// Recreate fields set such that the field canvas will re-render and apply any updated default values.
				var fields = { ...this.state.fields };
				this.applyDefaults(fields, response);

				this.setState({ editFailure: false, editSuccess: true, createSuccess: false, submitting: false, fields, fieldData: response, updateCount: this.state.updateCount + 1 });

				if (this.props.onActionComplete) {
					this.props.onActionComplete(response);
					return;
				} else if (window && window.location && window.location.pathname) {
					var parts = window.location.pathname.split('/')
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

				// Recreate fields set such that the field canvas will re-render and apply any updated default values.
				var fields = { ...this.state.fields };
				this.applyDefaults(fields, response);

				this.setState({ editFailure: false, submitting: false, fields, fieldData: response, updateCount: this.state.updateCount + 1 });

				if (this.props.onActionComplete) {
					this.props.onActionComplete(response);
					return;
				} else if (window && window.location && window.location.pathname) {
					var parts = window.location.pathname.split('/');
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
		};

		var html = window.SERVER ? undefined : document.querySelector("html");

		if (mainCanvas) {
			// Check for a field called url on the object:
			var pageUrl = this.state.fieldData && this.state.fieldData.url;
			if (pageUrl) {
				pageUrl = '/' + pageUrl.replace(/^\/|\/$/g, '');
			}

			var pageSections = pageUrl ? pageUrl.split("/") : [];
			var hasParameter = pageUrl ? pageUrl.match(/{([^}]+)}/g) : false;

			var hasFeedback = this.state.editFailure || this.state.editSuccess || this.state.createSuccess || this.state.deleteFailure;

			if (html) {
				html.style.setProperty('--admin-feedback-height', hasFeedback ? 'var(--fallback__admin-feedback-height)' : '0px');
			}

			var breadcrumbs = <>
				<li>
					<a href={'/en-admin/'}>
						{`Admin`}
					</a>
				</li>
				<li>
					<a href={'/en-admin/' + this.props.endpoint}>
						{this.capitalise(this.props.plural)}
					</a>
				</li>
				<li>
					{isEdit ? <>
						{`Editing ${this.props.singular}`}

						&nbsp;

						{pageUrl && hasParameter && <>
							<code className="admin-page__breadcrumbs-url">
								/{pageSections.map((section, i) => {
									var suffix = i < pageSections.length - 1 ? '/' : '';

									if (section.length) {

										// parameter?
										if (section.length >= 2 && section[0] == '{' && section[section.length - 1] == '}') {
											var param = section.substring(1, section.length - 1);
											var value = this.getParamValue(param);
											var tokenIndex = (this.state.pageState && this.state.pageState.tokenNames) ?
												this.state.pageState.tokenNames.findIndex(name => name == param) : -1;

											if (tokenIndex > -1 && this.state.pageState.tokens.length > tokenIndex) {
												return <>
													<button type="button" className="btn btn-link" onClick={() => this.startSelectParam(param)}>
														{value == '' ? param : value}
													</button>{suffix}
												</>;

                                            }

										}

										return <>
											{section}{suffix}
										</>;
									}
								})}
							</code>

							&nbsp;

							{/* TODO: build complete URL from selected parameters */}
							{/*
							<a href={pageUrl} target="_blank" rel="noopener noreferrer" className="btn btn-sm btn-outline-secondary panelled-editor__external-link">
								<i className="fa fa-fw fa-external-link"></i>
								{`Launch`}
							</a>
							 */}

						</>}

						{pageUrl && !hasParameter && <>
							<a href={pageUrl} target="_blank" rel="noopener noreferrer" className="panelled-editor__external-link">
								<code>
									{pageUrl}
								</code>
								<i className="fa fa-fw fa-external-link"></i>
							</a>
						</>}

						{!pageUrl && <>
							{'#' + this.state.id}
						</>}

						&nbsp;

						{(this.state.fieldData && this.state.fieldData.isDraft) && (
							<span className="badge bg-danger is-draft">
								{`Draft`}
							</span>
						)}
					</> : `Add new ${this.props.singular}`}
				</li>
			</>;

			var url = this.state.fieldData && this.state.fieldData.url;
			var primary = null;

			if (url) {
				var lastBracket = url.lastIndexOf('{');

				if (lastBracket != -1) {
					url = url.substring(lastBracket + 1);

					var nextBracket = url.indexOf('}');

					if (nextBracket != -1) {
						primary = url.substring(0, nextBracket);

						var primaryField = primary.indexOf('.');

						if (primaryField != -1) {
							primary = primary.substring(0, primaryField);
						}
					}
				}
			}

			return <>
				<Form formRef={r => this.form = r} autoComplete="off" locale={locale} action={endpoint}
					onValues={onValues} onFailed={onFailed} onSuccess={onSuccess}>
					<CanvasEditor
						fullscreen
						{...mainCanvas.data}
						controls={controls}
						primary={primary}
						feedback={feedback}
						breadcrumbs={breadcrumbs}
						additionalFields={() => this.props.renderFormFields ? this.props.renderFormFields(this.state) : this.renderFormFields()}
					/>
				</Form>
				{this.state.confirmDelete && this.renderConfirmDelete(pageState, setPage)}
				{this.state.selectParam && this.renderSelectParam()}
			</>;
		}

		if (html) {
			html.style.setProperty('--admin-feedback-height', '0px');
		}

		var title = isEdit ? `Edit ${this.props.singular}` : `Create New ${this.props.singular}`;

		if (isEdit && this.state.fieldData && this.state.fieldData.name && this.state.fieldData.name.trim().length) {
			title = `Edit ${this.props.singular} "${this.state.fieldData.name}"`;
		}

		var pageUrl = this.state.fieldData ? this.state.fieldData.primaryUrl : null;

		if (pageUrl && pageUrl.length && pageUrl.length > 0 && pageUrl.charAt(0) != "/") {
			pageUrl = "/" + pageUrl;
		}

		let qualifiedUrl = pageUrl ? (window.location.origin + pageUrl).toLowerCase() : '';

		if (qualifiedUrl.endsWith("//")) {
			qualifiedUrl = qualifiedUrl.slice(0, -1);
		}

		return <>
			<div className="admin-page">
				<header className="admin-page__subheader">
					<div className="admin-page__subheader-info">
						<h1 className="admin-page__title">
							{title}
						</h1>
						<ul className="admin-page__breadcrumbs">
							<li>
								<a href={'/en-admin/'}>
									{`Admin`}
								</a>
							</li>
							{this.props.previousPageUrl && this.props.previousPageName && <>
								<li>
									<a href={this.props.previousPageUrl}>
										{this.props.previousPageName}
									</a>
								</li>
							</>}
							{!this.props.hideEndpointUrl &&
								<li>
									<a href={'/en-admin/' + this.props.endpoint}>{this.capitalise(this.props.plural)}</a>
								</li>
							}
							<li>
								{isEdit ? <span>
									{`Editing ${this.props.singular} #` + this.state.id + ' '}
									{(this.state.fieldData && this.state.fieldData.isDraft) && (
										<span className="is-draft">
											{`(Draft)`}
										</span>
									)}
								</span> : `Add new`}
							</li>
						</ul>
					</div>

					{this.state.hasPrimaryUrl &&
						<div className="admin-page__url">
							<p>{`Page URL: `}</p>
							{pageUrl && pageUrl.length && pageUrl.length > 0
								? <a href={qualifiedUrl} target="_blank">
									{qualifiedUrl}
								</a>
								: <em>{`refresh the page to see the new URL`}</em>
							}
						</div>
					}
				</header>
				<div className="admin-page__content">
					<div className="admin-page__internal">
						{
							isEdit && this.state.isLocalized && locales.length > 1 && <div>
								<Input label={`Select Locale`} type="select" name="locale" value={locale} onChange={
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
										if (this.state.revisionId) {
											url += '&revision=' + this.state.revisionId;
										}

										if (!this.props.onActionComplete) {
											setPage(url);
										} else {
											this.setState({
												locale: e.target.value,
												endpoint: null,
												fields: null
											}, () => {
												// Load now:
												this.load(this.props);
											});
										}
									}
								}>
									{locales.map(loc => <option value={loc.id} selected={loc.id == locale}>{loc.name + (loc.id == '1' ? ' (Default)' : '')}</option>)}
								</Input>
							</div>
						}
						<Form formRef={r => this.form = r} autoComplete="off" locale={locale} action={endpoint}
							onValues={onValues} onFailed={onFailed} onSuccess={onSuccess}>
							{this.props.renderFormFields ? this.props.renderFormFields(this.state) : this.renderFormFields()}
						</Form>
					</div>
					{feedback && <>
						<footer className="admin-page__feedback">
							{feedback}
						</footer>
					</>}
					<footer className="admin-page__footer">
						{controls}
					</footer>
				</div>
			</div>
			{this.state.confirmDelete && this.renderConfirmDelete(pageState, setPage)}
		</>;
	}

}