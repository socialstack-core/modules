import React, { useState, useRef, useEffect } from 'react';
import TinyMce from 'UI/TinyMce';
import Loading from 'UI/Loading';
import Dialog from 'UI/Dialog';
import Canvas from 'UI/Canvas';
import Form from 'UI/Form';
import Button from 'UI/Button';
import pageApi from 'Api/Page';
import { useSession, sessionCtx } from 'UI/Session';
import getBuildDate from 'UI/Functions/GetBuildDate';
import { expandIncludes } from 'UI/Functions/WebRequest';
// @ts-ignore
import ModuleSelector from 'Admin/CanvasEditor/ModuleSelector';
import PropEditor from 'Admin/CanvasEditor/PropEditor';
import { canvasJsonToHtml, htmlToCanvasJson, searchForContentRoots } from './CanvasSaveLoad';
import { getRootInfo, RootProp } from './Utils';
import { getAll as getAllPropTypes, TypeMeta, CodeModuleMeta, getEditableTemplates } from 'Admin/Functions/GetPropTypes';
import { routerCtx } from 'UI/Router/RouterCtx';
import { useRouter } from 'UI/Router';
import { EditorProvider } from 'UI/TinyMce/EditorContext';

interface CanvasEditorProps {
	value?: any;
	defaultValue?: any;
	name?: string;
	[key: string]: any;
}

type RootContentProps = {
	name?: string;
}

declare module 'react' {
	namespace JSX {
		interface IntrinsicElements {
			'root-content': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & {
				'data-name'?: string;
				'data-placeholder'?: string;
				contentEditable?: boolean | "true" | "false";
			};
			'react-component': React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> & {
				'data-name'?: string;
				'data-props'?: string;
				'data-links'?: string;
				'data-mounted'?: boolean;
				contentEditable?: boolean | "true" | "false";
			};
		}
	}
}

function RootContent(props : React.PropsWithChildren<RootContentProps>) {
	const { name, children } = props;

	return <root-content data-name={name} contentEditable={true} data-placeholder={`Add content to ${name}..`} className="root-content mce-content-body">
		{children}
	</root-content>;
}

type EditableTemplate = {
	/**
	 * The loaded info about roots in this template (they come from slots).
	 */
	rootInfo: RootProp[]
};

type EditableTypeMeta = TypeMeta & {
	/**
	 * Maps template key to an EditableTemplate
	 */
	editableTemplates: Record<string, EditableTemplate>;
};

type TemplateWrapper = {
	/**
	 * The props (d) from the Admin/Template node, e.g. { templateKey }.
	 */
	props: Record<string, any>;
	/**
	 * Which key in r holds the editable page body content.
	 */
	bodyKey: string;
	/**
	 * All root content from r, so non-body roots are preserved on save.
	 */
	allRoots: Record<string, any>;
	/**
	 * True if the template was wrapped in { c: { t: "Admin/Template", ... } }.
	 */
	isWrapped: boolean;
};


const resolveDotField = (obj: any, field: string): any => {
	if (!obj || !field) return undefined;
	// a.b.c|a.b.d
	var optionals = field.split('|');
	var first = undefined;
	for(var i=0;i<optionals.length;i++){
		var value = optionals[i].split('.').reduce((current, key) => current?.[key], obj);
		if(value){
			return value;
		}
		
		if(i == 0){
			first = value;
		}
	}
	
	return first;
};

const rootContainerExists = (data: any, parts: string[]) => {
	var current: any = data;

	for (var i = 0; i < parts.length - 1; i++) {
		if (current == null) {
			return false;
		}

		current = current[parts[i]];
	}

	return current != null;
};


export default function CanvasEditor(props: CanvasEditorProps) {
	const [selectOpenFor, setSelectOpenFor] = useState<boolean | null>(null);
	const [selectComponentGroups, setSelectComponentGroups] = useState<string[] | undefined>(undefined);
	const [initialHtml, setInitialHtml] = useState<string | null>(null);
	const [propTypes, setPropTypes] = useState<EditableTypeMeta | null>(null);
	const [propsOpenFor, setPropsOpenFor] = useState<any>(null);
	const [propLinks, setPropLinks] = useState<Record<string, any>>({});
	const [templateWrapper, setTemplateWrapper] = useState<TemplateWrapper | null>(null);
	const [localPageState, setLocalPageState] = useState<any>(null);
	const editorRef = useRef<any>(null);
	const session = useSession();
	const { role } = session.session;
	const urlQueryParams = new URLSearchParams(window.location?.search || '');
	
	const setEmptyPageState = () => {
		setLocalPageState({
			url: '',
			primaryContentIncludes: null,
			oldVersion: false,
			query: new URLSearchParams(),
			redirect: null,
			description: null,
			title: null,
			po: undefined,
			tokenNames: [],
			tokens: []
		});
	};
	
	useEffect(() => {
		let url = urlQueryParams?.get("context");
		
		if(url){
			pageApi.pageState({
				url,
				version: getBuildDate().timestamp
			}).then(res => {
				if(res.oldVersion || res.redirect){
					setEmptyPageState();
				}else{
					if (res.po) {
						(res as any).po = expandIncludes(res.po);
					}

					setLocalPageState({ url, ...res, query: new URLSearchParams('')});
				}
			});
		}else{
			setEmptyPageState();
		}
	}, [
		urlQueryParams.context
	]);
	
	const getComponentGroupsForCursor = (editor: any): string[] | undefined => {
		const node = editor.selection.getNode();
		if (!node) {
			return undefined;
		}

		const rootContent = editor.dom.getParent(node, 'root-content');
		if (!rootContent) {
			return undefined;
		}

		const rootName = rootContent.getAttribute('data-name');
		if (!rootName) {
			return undefined;
		}

		const reactComponent = editor.dom.getParent(rootContent, 'react-component');
		if (!reactComponent) {
			return undefined;
		}

		const compName = reactComponent.getAttribute('data-name');
		const compPropsStr = reactComponent.getAttribute('data-props');
		const compProps = compPropsStr ? JSON.parse(compPropsStr) : {};

		let rootInfo: RootProp[] | undefined;

		if (compName === 'Admin/Template') {
			const templateName = compProps?.templateKey;
			rootInfo = propTypes?.editableTemplates?.[templateName]?.rootInfo;
		} else {
			rootInfo = getRootInfo(propTypes?.codeModules?.[compName]);
		}

		const matchingRoot = rootInfo?.find(r => r.name === rootName);
		return matchingRoot?.componentGroups;
	};

	// Convert incoming JSON to HTML once on mount
	useEffect(() => {
		// Load all prop types incl template ones:
		Promise.all([
			getAllPropTypes(),
			getEditableTemplates()
		])
			.then(loadedTemplates => {
				const propTypeMeta = loadedTemplates[0] as EditableTypeMeta;
				propTypeMeta.editableTemplates = loadedTemplates[1];

				let rawValue = props.value || props.defaultValue;

				if (typeof rawValue === 'string') {
					try {
						rawValue = JSON.parse(rawValue);
					} catch (e) {}
				}

				let innerValue = rawValue;
				let extractedWrapper: TemplateWrapper | null = null;

				const extractTemplate = (node: any): TemplateWrapper | null => {
					if (node && node.t === 'Admin/Template') {
						const rootKeys = Object.keys(node.r || {});
						if (rootKeys.length > 0) {
							const bodyKey = rootKeys.includes('body') ? 'body' : rootKeys[0];
							return {
								props: { ...(node.d || {}) },
								bodyKey,
								allRoots: { ...(node.r || {}) },
								isWrapped: false
							};
						}
					}
					return null;
				};

				// Direct node: {"t":"Admin/Template", ...}
				if (rawValue && typeof rawValue === 'object') {
					extractedWrapper = extractTemplate(rawValue);
					if (extractedWrapper) {
						innerValue = rawValue.r?.[extractedWrapper.bodyKey];
					} else {
						// Wrapped: {"c":{"t":"Admin/Template",...}}
						const wrapped = rawValue.c && typeof rawValue.c === 'object' ? rawValue.c : null;
						extractedWrapper = extractTemplate(wrapped);
						if (extractedWrapper) {
							extractedWrapper.isWrapped = true;
							innerValue = wrapped.r?.[extractedWrapper.bodyKey];
						}
					}
				}

				setTemplateWrapper(extractedWrapper);

				const html = canvasJsonToHtml(innerValue);
				setPropTypes(propTypeMeta);
				setInitialHtml(html);
			});
	}, []);

	if (initialHtml === null || !propTypes || !localPageState) {
		return null; // or a loading spinner
	}

	var getRootPropTypes = (componentName: string, props?: any) => {
		if (componentName == 'Admin/Template') {
			// Special case if it is a template. These props are virtual.
			var templateName = props?.templateKey;

			if (!templateName) {
				return undefined;
			}

			return propTypes?.editableTemplates?.[templateName]?.rootInfo;
		}

		return getRootInfo(propTypes?.codeModules?.[componentName], props, propTypes?.codeModules);
	};

	// Converts HTML elements -> react nodes.
	const loadRootValue = (el?: HTMLElement) => {
		if (!el || !el.childNodes.length) {
			return null;
		}

		var result: React.ReactNode[] = [];

		el.childNodes.forEach((childNode) => {

			if (childNode.nodeType == Node.ELEMENT_NODE) {
				const childEl = childNode as HTMLElement;
				var convertedComp: React.ReactNode = null;

				if (childEl.nodeName.toLowerCase() == 'react-component') {
					const loadedContent = loadReact(childEl);
					const compName = childEl.getAttribute('data-name');
					const propValues = childEl.getAttribute('data-props');
					const linkValues = childEl.getAttribute('data-links');
					// @ts-ignore
					convertedComp = <react-component data-name={compName} data-props={propValues} data-links={linkValues} data-mounted={true} contentEditable={false}>
						{loadedContent}
					</react-component>;
				} else {
					// All other html elements - bold etc.
					const Comp = childEl.nodeName;
					const propAttribs : Record<string, string> = {};

					const allAttribs = childEl.getAttributeNames();

					allAttribs.forEach(attribName => {
						const value = childEl.getAttribute(attribName);
						if (value !== null) {
							propAttribs[attribName] = value;
						}
					});

					// @ts-ignore
					convertedComp = <Comp {...propAttribs}>
						{loadRootValue(childEl)}
					</Comp>;
				}

				if (convertedComp) {
					result.push(convertedComp);
				}

			} else if (childNode.nodeType == Node.TEXT_NODE) {
				// Push as-is
				result.push(childNode.textContent);
			}

		});

		if (!result.length) {
			return null;
		}

		if (result.length == 1) {
			return result[0];
		}

		return result;
	};

	const loadReact = (el:HTMLElement) => {
		const componentName = el.getAttribute('data-name');
		if (!componentName) {
			return null;
		}

		// Socialstack's require for dynamic components
		let Component: any;
		try {
			Component = require(componentName).default;
		} catch (e) {
			// Component not found
		}

		// If component doesn't exist, clear the content
		if (!Component) {
			return null;
		}

		const componentData = JSON.parse(el.getAttribute('data-props') || '{}');
		const componentLinks = JSON.parse(el.getAttribute('data-links') || '{}');
		
		const moduleRootPropNames = getRootPropTypes(componentName, componentData);

		const roots: Record<string, React.ReactNode> = {
		};

		var currentRootLookup: Record<string, HTMLElement> = {};

		if (el.childNodes.length) {
			el.childNodes.forEach(child => {
				if (child.nodeType != Node.ELEMENT_NODE) {
					return;
				}

				const childEl = child as HTMLElement;
				const nodeName = childEl.nodeName.toLowerCase();

				if (nodeName != 'root-content') {
					return;
				}

				const rootName = childEl.getAttribute('data-name');

				if (!rootName) {
					return;
				}

				currentRootLookup[rootName] = childEl;
			});
		}

		if (moduleRootPropNames) {
			moduleRootPropNames.forEach(rootName => {
				const rootValue = currentRootLookup[rootName.name];
				roots[rootName.name] = <RootContent name={rootName.name}>{loadRootValue(rootValue)}</RootContent>;
			});
		}

		// Bind any dotted roots found in the DOM which weren't covered by the prop-type
		// enumeration (e.g. legacy content from before arrays were enumerated). These are
		// only bound when their container path already exists in the data, so stale array
		// index names can't grow the arrays back.
		for (var extraRootKey in currentRootLookup) {
			if (extraRootKey.indexOf('.') != -1 && !roots[extraRootKey] && rootContainerExists(componentData, extraRootKey.split('.'))) {
				const rootValue = currentRootLookup[extraRootKey];
				roots[extraRootKey] = <RootContent name={extraRootKey}>{loadRootValue(rootValue)}</RootContent>;
			}
		}
		
		if (componentLinks) {
			for (var k in componentLinks) {
				var link = componentLinks[k];
				var val;
				if (link.primary) {
					val = link.field ? resolveDotField(localPageState.po, link.field) : localPageState.po;
				} else {
					val = null;
				}
				
				if(link.parse){
					val = val ? JSON.parse(val) : null;
				}
				
				componentData[k] = val;
			}
		}
		
		const componentProps: Record<string, any> = {
			...componentData
		};

		for (var rootKey in roots) {
			if (rootKey.indexOf('.') != -1) {
				var parts = rootKey.split('.');
				var current: any = componentProps;

				for (var i = 0; i < parts.length - 1; i++) {
					if (current[parts[i]] == undefined) {
						current[parts[i]] = /^\d+$/.test(parts[i + 1]) ? [] : {};
					}

					current = current[parts[i]];
				}

				current[parts[parts.length - 1]] = roots[rootKey];
			} else {
				componentProps[rootKey] = roots[rootKey];
			}
		}

		return <Component {...componentProps} />;
	};

	const isEmailTemplate = props.templateType === 2 || String(props.primary || '').toLowerCase() === 'emailtemplate';

	return (
		<div className="canvas-editor">
			<TinyMce
				allowsReact
				contextMenu='link insertWidgetMenu'
				defaultValue={initialHtml}
				templateType={isEmailTemplate ? 2 : props.templateType}
				toolbar='bold italic underline | alignleft aligncenter alignright alignjustify | outdent indent | numlist bullist | grid_insert | insertWidget'
				allowFullscreen={props.fullscreen}
				dockHeader={props.fullscreen}
				onSetup={editor => {
					editorRef.current = editor;

					editor.ui.registry.addButton('insertWidget', {
						text: `Add Component`,
						icon: 'plus',
						onAction: () => {
							// Open the module selector with slot-specific componentGroups if available
							const slotComponentGroups = getComponentGroupsForCursor(editor);
							setSelectComponentGroups(slotComponentGroups);
							setSelectOpenFor(true);
						}
					});

					editor.ui.registry.addMenuItem('insertWidgetMenu', {
						text: `Add Component...`,
						icon: 'plus',
						onAction: () => {
							// Open the module selector with slot-specific componentGroups if available
							const slotComponentGroups = getComponentGroupsForCursor(editor);
							setSelectComponentGroups(slotComponentGroups);
							setSelectOpenFor(true);
						}
					});

					editor.on('SetContent', () => {
						const doc = editor.getDoc();
						if (!doc) return;

						var findReactNodes = (el: HTMLElement) => {
							if (el.nodeName.toLowerCase() == 'react-component' && !el.getAttribute('data-mounted')) {
								const componentName = el.getAttribute('data-name');
								const loadedComponent = loadReact(el);
								el.setAttribute('data-mounted', 'true');
								el.setAttribute('contenteditable', 'false');
								el.setAttribute('data-empty-message', `${componentName} - Click to configure this component`);
								el.innerHTML = '';
								// @ts-ignore
								try{
									React.render(
										<EditorProvider isEditing isAdmin>
											<sessionCtx.Provider value={session}>
												<routerCtx.Provider value={{
													canGoBack: () => false,
													pageState: localPageState,
													setPage: () => { },
													changeQuery: () => { },
													getPageIncludes: () => {
														return '';
													},
													updateQuery: (update: Record<string, string | number | boolean | (string | number | boolean)[] | null | undefined>) => { },
													removeQueryItems: (items: string[]) => { },
													setPrimaryObject: (obj: any) => { }
												}}>
													{loadedComponent}
												</routerCtx.Provider>
											</sessionCtx.Provider>
										</EditorProvider>,
										el
									);
								}catch(e){
									// Allow continuing
									console.error(e);
								}
								
							} else {
								// Iterate the children. 
								// Don't do this on a *non-mounted* react-component as it hydrates its full tree.
								el.childNodes.forEach(child => {
									if (child.nodeType == Node.ELEMENT_NODE) {
										findReactNodes(child as HTMLElement);
									}
								});
							}
						};

						findReactNodes(doc.body);
					});

					editor.on('GetContent', (e : any) => {
						const doc = new DOMParser().parseFromString(e.content, 'text/html');
						// Find all components in the editor body

						const components = doc.querySelectorAll('react-component');

						components.forEach((comp:Element) => {
							// If the user deleted the 'data-mounted' attribute in the HTML editor,
							// or it's a brand new node, we treat it as unmounted.
							if (comp.getAttribute('data-mounted')) {
								// Remove mounted & editable:
								comp.removeAttribute('data-mounted');
								comp.removeAttribute('contenteditable');

								// Reduce its content down to just its root-contents:
								var roots : HTMLElement[] = [];
								searchForContentRoots(comp, (el: HTMLElement, rootName: string) => {
									roots.push(el);
								});
								comp.innerHTML = '';
								roots.forEach(root => {
									root.removeAttribute('contenteditable');
									root.removeAttribute('class');
									root.removeAttribute('data-placeholder');
									comp.appendChild(root);
								});
							}
						});

						e.content = doc.body.innerHTML;
					});

					editor.ui.registry.addButton('editProps', {
						text: `Edit component ...`,
						icon: 'settings',
						onAction: () => {
							const element = editor.selection.getNode();
							const comp = editor.dom.getParent(element, 'react-component') as HTMLElement;
							const eleName = comp.getAttribute("data-name");
							const propValues = JSON.parse(comp.getAttribute("data-props") || '{}') || {};
							const linkValues = JSON.parse(comp.getAttribute("data-links") || '{}') || {};
							setPropLinks(linkValues);

							setPropsOpenFor({
								element: comp,
								node: {
									type: eleName,
									typePropTypes: eleName ? propTypes?.codeModules?.[eleName] : undefined,
									typeMeta: propTypes,
									props: propValues,
									links: linkValues,
									element: comp
								}
							});
						}
					});

					// Heading showing the selected component name. Non-interactive, so it
					// acts as a title for the component config popup.
					let componentNameApi: any = null;
					const updateComponentName = () => {
						if (!componentNameApi) {
							return;
						}
						const element = editor.selection.getNode();
						const comp = editor.dom.getParent(element, 'react-component') as HTMLElement;
						componentNameApi.setText(comp?.getAttribute('data-name') || '');
					};
					editor.on('NodeChange', updateComponentName);

					editor.ui.registry.addButton('componentName', {
						text: ``,
						onAction: () => { },
						onSetup: (api: any) => {
							componentNameApi = api;
							updateComponentName();
							api.setEnabled(false);
							return () => {
								componentNameApi = null;
							};
						}
					});

					editor.ui.registry.addButton('duplicateComponent', {
						text: `Duplicate component`,
						icon: 'copy',
						onAction: () => {
							const element = editor.selection.getNode();
							const comp = editor.dom.getParent(element, 'react-component') as HTMLElement;
							if (!comp) {
								return;
							}
							const clone = comp.cloneNode(true) as HTMLElement;
							editor.dom.insertAfter(clone, comp);
							// Triggers GetContent (reduces mounted components to their roots)
							// and SetContent (re-mounts all components incl the new clone).
							const current = editor.getContent();
							editor.setContent(current);
						}
					});

					editor.ui.registry.addButton('removeComponent', {
						text: `Remove component`,
						icon: 'remove',
						onAction: () => {
							const element = editor.selection.getNode();
							const comp = editor.dom.getParent(element, 'react-component') as HTMLElement;
							if (!comp) {
								return;
							}
							editor.dom.remove(comp);
							const current = editor.getContent();
							editor.setContent(current);
						}
					});


					editor.ui.registry.addContextToolbar('componentConfig', {
						predicate: (node: Element) => {
							const isMatch = !propsOpenFor && !!editor.dom.getParent(node, 'react-component');
							return isMatch;
						},
						items: 'componentName | editProps duplicateComponent removeComponent',
						position: 'node',
						scope: 'node'
					});

					editor.on('keydown', (e) => {
						if (e.keyCode === 8 || e.keyCode === 46) { // Backspace or Delete
							const range = editor.selection.getRng();
							const container = range.startContainer;

							// Find if we are inside a root-content element
							const rootNode = editor.dom.getParent(container, 'root-content') as HTMLElement;

							if (rootNode) {
								// If there's only one character left or it's nearly empty
								if (rootNode.innerText.length <= 1) {
									rootNode.innerHTML = '<br data-mce-bogus="1" />';
									e.preventDefault();
									return false;
								}
							}
						}
					});
				}}
			/>

			{/* The module selector modal */}
			{selectOpenFor && <ModuleSelector
				componentGroups={selectComponentGroups ?? props.componentGroups ?? (role?.key == 'developer' ? undefined : ["admin_authoring"])}
				selectOpenFor={selectOpenFor}
				onClose={() => {
					setSelectComponentGroups(undefined);
					setSelectOpenFor(false);
				}}
				onSelected={(module: any) => {
					if (editorRef.current) {
						// triggers SetContent above which in turn calls loadReact.
						// That causes any default roots to be added as necessary.

						editorRef.current.insertContent(
							`<react-component data-name="${module.publicName}" data-props="{}" contenteditable="false"></react-component>`
						);
					}
				}}
			/>}

			<Form
				// @ts-ignore
				action={(values) => {
					// Update the props:
					const ele = propsOpenFor.element;
					ele.setAttribute('data-props', JSON.stringify(values));
					ele.setAttribute('data-links', JSON.stringify(propLinks));

					// Load & set back:
					const current = editorRef.current.getContent();
					editorRef.current.setContent(current);

					setPropLinks({});
					setPropsOpenFor(null);
					return Promise.resolve({});
				}}
			>
				<Dialog
					isOpen={!!propsOpenFor}
					onClose={() => setPropsOpenFor(null)}
					title={`Edit component ${propsOpenFor?.node?.type}`}
					className="prop-editor-modal"
				>
					{
						!propsOpenFor ?
							<Loading /> :
							<PropEditor 
								optionsVisibleFor={propsOpenFor.node}
								links={propLinks}
								onLinksChange={setPropLinks}
							/>
					}
					<Dialog.Footer>
						<Button outlined onClick={() => setPropsOpenFor(null)}>
							{`Close`}
						</Button>
						<Button type="submit">
							{`Apply Changes`}
						</Button>
					</Dialog.Footer>
				</Dialog>
			</Form>


			{/* Socialstack's hidden input that captures the form data */}
			<input
				type="hidden"
				name={props.name}
				ref={ref => {
					if (!ref) {
						return;
					}

				// @ts-ignore
					ref.onGetValue = (val, ele) => {
						if (ele === ref && editorRef.current) {
							const currentHtml = editorRef.current.getContent();
							let result = htmlToCanvasJson(currentHtml);

							// Re-wrap with template if we stripped one on load
							if (templateWrapper && result) {
								let innerContent: any;
								try {
									innerContent = typeof result === 'string' ? JSON.parse(result) : result;
								} catch (e) {
									innerContent = result;
								}

								const wrappedRoots = { ...templateWrapper.allRoots };
								wrappedRoots[templateWrapper.bodyKey] = innerContent;

								const wrappedNode = {
									t: 'Admin/Template',
									d: { ...templateWrapper.props },
									r: wrappedRoots
								};

								result = templateWrapper.isWrapped
									? JSON.stringify({ c: wrappedNode })
									: JSON.stringify(wrappedNode);
							}

							return result;
						}
					};
				}}
			/>
		</div>
	);
}

// Map the global component for DataForm compatibility
if ((window as any).inputTypes) {
	(window as any).inputTypes.canvas = function (props: any) {
		const { field } = props;
		return <CanvasEditor
			{...field}
		/>;
	};
}