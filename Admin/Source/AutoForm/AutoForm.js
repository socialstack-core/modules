import Form from 'UI/Form';
import Input from 'UI/Input';
import Canvas from 'UI/Canvas';
import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import SubHeader from 'Admin/SubHeader';
import Modal from 'UI/Modal';
import Html from 'UI/Html';
import ConfirmModal from 'UI/Modal/ConfirmModal';
import getAutoForm from 'Admin/Functions/GetAutoForm';
import formatTime from "Admin/Functions/FormatTime";
import CanvasEditor from "Admin/CanvasEditor";
import getBuildDate from 'UI/Functions/GetBuildDate';
import { useSession } from 'UI/Session';
import { useRouter, routerCtx } from 'UI/Router';
import pageApi from 'Api/Page';
import localeApi from 'Api/Locale';
import AutoFormExtensions from "Admin/AutoForm/AutoFormExtensions";
import Link from "UI/Link";
import { TabsWrapper, TabsLinksWrapper, TabsLinkWrapper, TabsPanelsWrapper, TabsPanelWrapper } from "UI/Tabs";
import { useState, useEffect, useRef, useCallback } from 'react';

/**
 * Used to automatically generate forms used by the admin area based on fields from your entity declarations in the API.
 * To use this, use AutoService/ AutoController.
 * Most modules do this, so check any existing one for some examples.
 */

export default function AutoForm(props) {
	const formRef = useRef();
	var { session, setSession } = useSession();
	var { setPage, pageState, updateQuery } = useRouter();
	const { query } = pageState;

	// True if any changes are currently unsaved
	const [unsavedChanges, setUnsavedChanges] = useState(false);
	const [failed, setFailed] = useState(false);
	const [updateCount, setUpdateCount] = useState(0);
	const [editFailure, setEditFailure] = useState(null);
	const [editSuccess, setEditSuccess] = useState(null);
	const [createSuccess, setCreateSuccess] = useState(false);
	const [confirmSaveAs, setConfirmSaveAs] = useState(false);
	const [confirmDelete, setConfirmDelete] = useState(false);
	const [submitting, setSubmitting] = useState(false);
	const [deleting, setDeleting] = useState(false);
	const [deleteFailure, setDeleteFailure] = useState(null);
	
	// The form structure as a set of its fields
	const [formData, setFormData] = useState(null);
	const [tabCanvases, setTabCanvases] = useState(null);

	// The current field values
	const [fieldData, setFieldData] = useState(props.content ? { ...props.content } : {});
	const pci = pageState.primaryContentIncludes;
	const includes = pci ? pci.split(',') : ['*', 'primaryUrl'];
	const currentTab = query?.get("tab");
	const parsedId = props.content?.id;
	var isEdit = !!parsedId;

	const setCurrentTab = (target) => {
		updateQuery({
			tab: target
		});
	};

	const onContentNode = useCallback(contentNode => {
		if (!contentNode || !contentNode.props) {
			return;
		}

		var data = contentNode.props;

		if (!isEdit && data.readonly) {
			// Readonly fields are not present in 'add' mode
			return null;
		}

		// setup the hint prompts even if no data 
		if (data.name) {

			if (data.hint) {
				var hint = <i className='fa fa-lg fa-question-circle hint-field-label' title={data.hint} />;

				if (Array.isArray(data.label)) {
					data.label.push(hint);
				} else {
					data.label = [(data.label || ''), hint];
				}
			}
		}

		if (!data.name || !fieldData) {
			return;
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

		data.currentContent = fieldData;

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

			fieldData[data.name] = val;
			setUnsavedChanges(true);

			// Redraw the fields:
			setUpdateCount(updateCount + 1);
			props.onChange && props.onChange(fieldData);
		};

		var value = fieldData[data.name];

		if (value !== undefined) {
			if (data.name == "createdUtc") {
				data.defaultValue = formatTime(value);
			} else {
				data.defaultValue = value;
			}
		}
	}, [fieldData, props.onChange]);

	// Load the form fields - the initial content itself is provided via props.content
	useEffect(() => {
		setFormData(null);
		getAutoForm('content', props.contentType.toLowerCase())
			.then(formData => {
				const { form } = formData;
				const { fields } = form;

				// Does any field specify a sort order?
				var usesSorting = fields?.find(fld => fld.data.sortOrder);

				if (usesSorting) {
					fields.forEach(fld => {
						fld.sortOrder = parseInt(fld.data?.sortOrder) || 10;
					});

					// inline - updates orig formData implicitly.
					fields.sort((a, b) => a.sortOrder - b.sortOrder);
				}

				// Tabs are always present. Create the default 'details' tab if none are configured.
				let tabs = props.tabs;
				let fallbackTab = 0;

				if (!tabs) {
					tabs = [
						{
							name: `Details`,
							key: `details`
						}
					];
				} else {
					// Fallback tab is whichever has the key 'details', or tab zero.
					for (var i = 0; i < tabs.length; i++) {
						if (tabs[i].key == 'details') {
							fallbackTab = i;
							break;
						}
					}
				}

				// Collect each field in to its associated tab.
				let _tabCanvases = tabs.map((tabInfo, tabIndex) => {

					return {
						...tabInfo,
						canvas: {
							c: formData.form.fields.filter(field => {
								// If the field is not for a specific tab then it is always present on tabIndex 0.
								const tabKey = field.data?.tab;

								if (!tabKey) {
									return tabIndex == fallbackTab;
								}

								// It has a tabKey - is it the one we want?
								return tabKey == tabInfo.key;

							}).map(field => {

								return {
									t: field.module,
									c: field.content,
									__key: 'af_' + field.data.name,
									d: field.data ? { ...field.data } : null
								};

							})
						}
					};

				});

				// remove any empty tabs
				_tabCanvases = _tabCanvases.filter(tab => tab?.canvas?.c?.length > 0);

				if (form.supportsRevisions && !props.isRevision && parsedId) {
					_tabCanvases.push(
						{
							name: `Drafts and history`,
							key: `revisions`,
							canvas: {
								t: 'Admin/Revisions/List',
								__key: 'af_revisions',
								d: {
									contentType: props.contentType,
									id: parsedId
								}
							}
						}
					);
				}

				setTabCanvases(_tabCanvases);
				setFormData(formData);
			})
			.catch(e => {
				console.error(e);
				setFailed(true);
			});
	}, [props.contentType]);

	// Unsaved changes prompt
	useEffect(() => {
		const beforeUnload = (e) => {
			if (unsavedChanges) {
				e.preventDefault();
				return e.returnValue = 'Unsaved changes - are you sure you want to exit?';
			}
		};

		global.window.addEventListener('beforeunload', beforeUnload);

		return () => {
			global.window.removeEventListener('beforeunload', beforeUnload);
		};
	}, [unsavedChanges]);

	if (failed) {
		var ep = props.contentType || '';
		return (
			<Alert type='danger'>
				{'Oh no! It Looks like this type doesn\'t support the admin panel. Ask a developer to make sure the type name ("' + ep + '") is spelt correctly. The value comes from the page config of this page, and the type name should match the name of the entity in the API. Case doesn\'t matter.'}
			</Alert>
		);
	}
	
	if (!formData) {
		return <Loading />;
	}

	const { form } = formData;
	const { supportsRevisions } = form;
	// Get the API handler for this content type:
	var api = require('Api/' + props.contentType).default;

	const submitForm = (submitter) => {
		formRef.current.requestSubmit(submitter);
	}

	const doConfirmDelete = () => {
		setConfirmDelete(false);
		setDeleting(true);

		var prom; // :Promise<WhateverTheContentTypeIs>

		if (supportsRevisions) {
			prom = api.deleteRevision(parsedId);
		} else {
			prom = api.delete(parsedId);
		}

		prom.then(response => {
			if (props.onActionComplete) {
				props.onActionComplete(null);
				return;
			}

			var parts = window.location.pathname.split('/');

			// Go to root parent page:
			var target = props.deletePage;
			if (!target || !target.length) {
				parts = parts.slice(0, 3); // e.g. ['en-admin', 'pages']. will always go to the root.
				target = parts.join('/');
			} else {
				target = '/' + parts[1] + '/' + target;
			}

			setPage(target);

		}).catch(e => {
			console.error(e);
			setDeleting(false);
			setDeleteFailure(true);
		});
	}

	const doConfirmSaveAs = (submitter) => {
		setConfirmSaveAs(false);
		submitForm(submitter);
	}

	const renderConfirmDelete = () => {
		return <>
			<ConfirmModal
				confirmCallback={() => doConfirmDelete()} confirmVariant="danger" confirmText={`Yes, delete the ${props.singular}`}
				cancelCallback={() => setConfirmDelete(false)}>
				<p>
					{`Are you sure you wish to delete this ${props.singular}?`}
				</p>
			</ConfirmModal>
		</>;
	}

	const renderConfirmSaveAs = () => {
		return <>
			<ConfirmModal
				confirmCallback={() => doConfirmSaveAs()} confirmVariant="danger" confirmText={`Yes, save this as a new ${props.singular}`}
				cancelCallback={() => setConfirmSaveAs(false)}>
				<p>
					{`You are about to the save this as a new ${props.singular}. Any changes will be saved in the new ${props.singular} and the existing ${props.singular} will be not be updated. Continue?`}
				</p>
			</ConfirmModal>
		</>;
	}

	const capitalise = (name) => {
		return name && name.length ? name.charAt(0).toUpperCase() + name.slice(1) : "";
	}

	// check for overriding "parent" property
	// can be used to override the default parent breadcrumb link in the event the parent page is not available
	// (e.g. /navmenu lists all nested menus, /navmenuitem/[id] describes a submenu, but /navmenuitem does not exist)
	let parentUrl = props.parent && props.parent.trim().length ?
		`/en-admin/${props.parent.toLowerCase()}` :
		`/en-admin/${props.contentType.toLowerCase()}`;

	var breadcrumbs = [];

	if (props.previousPageUrl && props.previousPageName) {
		breadcrumbs.push({
			url: props.previousPageUrl,
			title: props.previousPageName
		});
	}

	if (!props.hideEndpointUrl) {
		breadcrumbs.push({
			url: parentUrl,
			title: capitalise(props.plural)
		});
	}

	if (isEdit) {
		var editTitle = `Editing ${props.singular} #` + parsedId;

		breadcrumbs.push({ title: editTitle });
	} else {
		breadcrumbs.push({ title: `Add new` });
	}

	var pageUrl = fieldData ? fieldData.primaryUrl : null;

	if (pageUrl && pageUrl.length && pageUrl.length > 0 && pageUrl.charAt(0) != "/") {
		pageUrl = "/" + pageUrl;
	}

	let qualifiedUrl = pageUrl ? (window.location.origin + pageUrl).toLowerCase() : '';

	if (qualifiedUrl.endsWith("//")) {
		qualifiedUrl = qualifiedUrl.slice(0, -1);
	}

	const renderCanvas = (canvas) => {
		return <Canvas forcedUpdate={updateCount}
			onContentNode={onContentNode}
			onRenderNode={node => {
				if (node.typeName == "UI/Input" && (node.props.type == "checkbox" || node.props.type == "radio")) {
					node.props.defaultChecked = node.props.defaultValue;
				}

				if (props.onRenderField) {
					if (props.onRenderField(node, fieldData, isEdit) === false) {
						return null;
					}
				}
			}}
			bodyJson={canvas}
		/>;
	};

	const renderFormTabs = () => {
		return <TabsWrapper fullWidth>
			{/* tab links */}
			<TabsLinksWrapper>
				{tabCanvases.map((tab, i) => {
					const linkId = `tab-link${i + 1}`;
					const panelId = `tab-panel${i + 1}`;
					const selected = (!currentTab && i == 0) || currentTab === tab.key;

					return (
						<TabsLinkWrapper key={linkId}>
							<input
								type="radio"
								name="autoform-tabs"
								id={linkId}
								aria-controls={panelId}
								checked={selected}
								onClick={() => {
									setCurrentTab(tab.key);
								}}
							/>
							<label htmlFor={linkId}>{tab.name}</label>
						</TabsLinkWrapper>
					);
				})}
			</TabsLinksWrapper>

			{/* tab panels */}
			<TabsPanelsWrapper>
				{tabCanvases.map((tab, i) => {
					const panelId = `tab-panel${i + 1}`;
					let tabContent = null;

					if (tab.canvas.c?.length == 1 && tab.canvas.c[0]?.d?.type == 'canvas') {
						// There's exactly 1 canvas node in this tab - render it as a fullscreen editor.
						var editorNode = tab.canvas.c[0];

						tabContent = <CanvasEditor
							fullscreen
							{...editorNode.d}
							defaultValue={fieldData[editorNode.d.name]}
							primary={props.contentType}
							currentContent={fieldData}
							onChange={(e) => {
								// Input field has changed. Update the content object so any redraws are reflected.
								// var val = e.target.value;
								fieldData[editorNode.d.name] = e.json;
							}}
						/>;
					} else if (tab.contentJson) {
						// Extended field set.
						tabContent = <>
							<Canvas>{tab.contentJson}</Canvas>
							{renderCanvas(tab.canvas)}
						</>;
					} else {
						// Normal field set.
						tabContent = renderCanvas(tab.canvas);
					}

					return (
						<TabsPanelWrapper id={panelId} key={panelId}>
							{tabContent}
						</TabsPanelWrapper>
					);
				})}
			</TabsPanelsWrapper>
		</TabsWrapper>;
	}
	
	const renderForm = () => {
		const feedback = <>
			{
				editFailure && (
					<Alert variant='danger'>
						{editFailure.message || `Something went wrong whilst trying to save your changes - your device might be offline, so check your internet connection and try again.`}
					</Alert>
				)
			}
			{
				editSuccess && (
					<Alert variant='success'>
						{`Your changes have been saved`}
					</Alert>
				)
			}
			{
				createSuccess && (
					<Alert variant='success'>
						{`Created successfully`}
					</Alert>
				)
			}
			{
				deleteFailure && (
					<Alert variant='danger'>
						{`Something went wrong whilst trying to delete this - your device might be offline, so check your internet connection and try again.`}
					</Alert>
				)
			}
		</>;

		const extraButtonMapFunc = (button) => {

			if (button.href) {
				return (
					<Link href={button.href}>
						<button
							type={'button'}
							className={button.className}
						>
							{button.label}
						</button>
					</Link>
				)
			}

			return (
				<button
					type={'button'}
					className={button.className}
					onClick={() => button.onClick && button.onClick(props.content, setPage)}
				>
					{button.label}
				</button>
			);
		}

		var controls = <>
			{
				isEdit ?
					AutoFormExtensions.getAutoFormButtons(props.contentType, 'update').map(extraButtonMapFunc) :
					AutoFormExtensions.getAutoFormButtons(props.contentType, 'create').map(extraButtonMapFunc)
			}
			{isEdit && <>
				<button disabled={!!supportsRevisions} title={supportsRevisions ? `Can't delete revisions` : undefined} className="btn ui-btn btn-outline-danger" type="button" onClick={e => {
					e.preventDefault();
					setConfirmDelete(true);
				}}>
					<i className="fal fa-trash"></i> {`Delete this ${props.singular}`}
				</button>
			</>}
			<div className="save-group">
				{supportsRevisions && (
					<Input inline type="submit" name="form_submitMode" value="draft" className="btn ui-btn btn-secondary" onClick={e => {
						submitForm(e.target);
					}} disabled={submitting}>
						{isEdit ? `Save Draft` : `Create Draft`}
					</Input>
				)}

				{/* todo - check for content type and do more ?? */}
				{isEdit &&
					<button className="btn ui-btn btn-secondary" name="form_submitMode" value="copy" type="button" onClick={e => {
						e.preventDefault();
						setConfirmSaveAs(true);
					}}>
						{`Save as a copy..`}
					</button>
				}

				<Input inline type="submit" name="form_submitMode" value="save" disabled={submitting} onClick={e => {
					submitForm(e.target);
				}}>
					{isEdit ? `Save and Publish` : `Create`}
				</Input>
			</div>
		</>;

		var onValues = (values, setAction) => {
			const submitMode = values.form_submitMode;

			if (submitMode == 'copy') {

				// create a copy of the curent entry (as-is)
				values.id = null;
				setAction(api.create);

			} else if (submitMode == 'draft') {
				// Set content ID if there is one already:
				if (isEdit && parsedId) {
					values.id = parsedId;
				}

				// Create a draft:
				setAction(api.createDraft);
			} else if (submitMode == 'save') {
				// Potentially publishing a draft.
				if (props.isRevision) {
					// Use the publish EP, but importantly do so with the raw ID from the URL - not the ID from the content object.
					const revisionId = parseInt(pageState.tokens[0]);
					setAction((fields) => api.publishRevision(revisionId, fields));
				}
			} else {
				// Unknown mode, reject.
				return Promise.reject({ message: 'Unknown form submit mode' });
			}

			setEditSuccess(false);
			setEditFailure(false);
			setCreateSuccess(false);
			setSubmitting(true);
			return values;
		};

		var onFailed = response => {
			setEditFailure(response || true);
			setCreateSuccess(false);
			setSubmitting(false);
		};

		var onSuccess = response => {
			var state = pageState;
			setUnsavedChanges(false);

			setTimeout(() => {
				setEditSuccess(false);
				setCreateSuccess(false);
			}, 3000);
			
			if (isEdit) {
				setEditFailure(false);
				setEditSuccess(true);
				setCreateSuccess(false);
				setSubmitting(false);
				setFieldData(response);
				setUpdateCount(updateCount + 1);

				if (props.onActionComplete) {
					props.onActionComplete(response);
					return;
				} else if (window && window.location && window.location.pathname) {
					var parts = window.location.pathname.substring(1).split('/');

					if (response.type.startsWith('Revision')) {
						// Saved a draft

						if (!props.isRevision || response.id != parsedId) {
							// Not currently on the revisions page. Go to it:
							setPage('/' + parts[0] + '/' + parts[1] + '/revision/' + response.id + '?created=1');
						}

					} else if (props.isRevision || response.id != parsedId) {
						// E.g. published content from a draft. Go there now.
						setPage('/' + parts[0] + '/' + parts[1] + '/' + response.id + '?published=1');
					}
				}
			} else {
				setEditFailure(false);
				setSubmitting(false);
				setFieldData(response);
				setUpdateCount(updateCount + 1);

				if (props.onActionComplete) {
					props.onActionComplete(response);
					return;
				} else if (window && window.location && window.location.pathname) {
					var parts = window.location.pathname.substring(1).split('/');

					if (response.type.startsWith('Revision')) {
						// Created a draft
						setPage('/' + parts[0] + '/' + parts[1] + '/revision/' + response.id + '?created=1');
					} else {
						setPage('/' + parts[0] + '/' + parts[1] + '/' + response.id + '?created=1');
					}
				}
			}
		};
		
		var title = isEdit ? `Edit ${props.singular}` : `Create New ${props.singular}`;

		if (isEdit && fieldData?.name && fieldData.name.trim().length) {
			title = `Edit ${props.singular} "${fieldData.name}"`;
		}
		
		var originalUrl = props.isRevision ? parentUrl + '/' + parsedId : '';
		
		return <Form formRef={formRef} autoComplete="off" action={
			isEdit ? values => api.update(parsedId, values, includes) :
				values => api.create(values, includes)}
			onValues={onValues} onFailed={onFailed} onSuccess={onSuccess}>
				<div className="admin-page">
					<SubHeader title={title} breadcrumbs={breadcrumbs} primaryUrl={qualifiedUrl} />
					{!props.isRevision && currentTab != 'revisions' && props.content?.recentDraft>0 && <Alert type='info'>
						{`There is a more recent draft of this content. Click the drafts tab below to view.`}
					</Alert>}
					{props.isRevision && <Alert type='info'>
						<Html>
						{parsedId ? `You are viewing a revision of <a href='${originalUrl}'>${props.singular} #${parsedId}</a>.` : `You are viewing a draft.`}
						</Html>
					</Alert>}
					<div className="admin-page__content">
						<div className="admin-page__internal">
							{
								props.onBeforeForm && props.onBeforeForm(isEdit)
							}
							{props.renderFormFields ? props.renderFormFields({
								formData,
								tabCanvases,
								formFields
							}, isEdit) : renderFormTabs()}
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
				{confirmDelete && renderConfirmDelete()}
				{confirmSaveAs && renderConfirmSaveAs()}
			</Form>;
	};

	return <routerCtx.Provider
		value={{
			canGoBack: () => false,
			pageState: { url: '', query: new URLSearchParams(''), po: pageState.po },
			setPage
		}}
	>
		{renderForm()}
	</routerCtx.Provider>;
}

class AutoFormInternal extends React.Component {

	constructor(props) {
		super(props);
		this.formId = "autoform-instance-" + formId++;

		this.state = {
			submitting: false,
			updateCount: 0
		};
	}

	applyDefaults(formCanvas, values) {
		var c = formCanvas.c;
		for (var i = 0; i < c.length; i++) {
			var field = c[i];
			if (!field) {
				continue;
			}
			var data = field.d;

			if (!data || !data.name) {
				continue;
			}

			data.defaultValue = values[data.name];
		}
	}
}