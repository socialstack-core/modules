import React, { useState, useRef, useEffect } from 'react';
import TinyMce from 'UI/TinyMce';
import Loading from 'UI/Loading';
import Dialog from 'UI/Dialog';
import Canvas from 'UI/Canvas';
import Form from 'UI/Form';
import Button from 'UI/Button';
import { useSession, sessionCtx } from 'UI/Session';
// @ts-ignore
import ModuleSelector from 'Admin/CanvasEditor/ModuleSelector';
import PropEditor from 'Admin/CanvasEditor/PropEditor';
import { canvasJsonToHtml, htmlToCanvasJson, searchForContentRoots } from './CanvasSaveLoad';
import { getRootInfo, RootProp } from './Utils';
import { getAll as getAllPropTypes, TypeMeta, CodeModuleMeta, getEditableTemplates } from 'Admin/Functions/GetPropTypes';
import { routerCtx } from 'UI/Router/RouterCtx';

interface CanvasEditorProps {
	value?: any;
	defaultValue?: any;
	name?: string;
	[key: string]: any;
}

function RootContent(props) {
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

export default function CanvasEditor(props: CanvasEditorProps) {
	const [selectOpenFor, setSelectOpenFor] = useState<boolean | null>(null);
	const [selectComponentGroups, setSelectComponentGroups] = useState<string[] | undefined>(undefined);
	const [initialHtml, setInitialHtml] = useState<string | null>(null);
	const [propTypes, setPropTypes] = useState<EditableTypeMeta | null>(null);
	const [propsOpenFor, setPropsOpenFor] = useState<any>(null);
	const editorRef = useRef<any>(null);
	const session = useSession();
	const { role } = session.session;

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
				const rawValue = props.value || props.defaultValue;
				const html = canvasJsonToHtml(rawValue);
				setPropTypes(propTypeMeta);
				setInitialHtml(html);
			});
	}, []);

	if (initialHtml === null || !propTypes) {
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

		return getRootInfo(propTypes?.codeModules?.[componentName]);
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
					convertedComp = <react-component data-name={compName} data-props={propValues} data-mounted={true} contentEditable={false}>
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

		const componentProps = {
			...componentData,
			...roots
		};

		return <Component {...componentProps} />;
	};

	return (
		<div className="canvas-editor">
			<TinyMce
				allowsReact
				contextMenu='link insertWidgetMenu'
				defaultValue={initialHtml}
				toolbar='bold italic underline | alignleft aligncenter alignright alignjustify | outdent indent | numlist bullist | grid_insert | insertWidget'
				allowFullscreen={true}
				dockHeader={true}
				onSetup={(editor: any) => {
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

								const loadedComponent = loadReact(el);
								el.setAttribute('data-mounted', 'true');
								el.setAttribute('contenteditable', 'false');
								el.innerHTML = '';
								React.render(
									// @ts-ignore
									<sessionCtx.Provider value={session}>
										<routerCtx.Provider value={{
											canGoBack: () => false,
											pageState: {
												url: '',
												oldVersion: false,
												query: new URLSearchParams()
											},
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
									</sessionCtx.Provider>,
									el
								);
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
						text: `Edit component config...`,
						icon: 'settings',
						onAction: () => {
							const element = editor.selection.getNode();
							const comp = editor.dom.getParent(element, 'react-component') as HTMLElement;
							const eleName = comp.getAttribute("data-name");
							const propValues = JSON.parse(comp.getAttribute("data-props") || '{}') || {};

							setPropsOpenFor({
								element: comp,
								node: {
									type: eleName,
									typePropTypes: eleName ? propTypes?.codeModules?.[eleName] : undefined,
									props: propValues
								}
							});
						}
					});

					editor.ui.registry.addContextToolbar('componentConfig', {
						predicate: (node: Element) => {
							const isMatch = !propsOpenFor && !!editor.dom.getParent(node, 'react-component');
							return isMatch;
						},
						items: 'editProps',
						position: 'node',
						scope: 'node'
					});

					editor.on('keydown', (e) => {
						if (e.keyCode === 8 || e.keyCode === 46) { // Backspace or Delete
							const range = editor.selection.getRng();
							const container = range.startContainer;

							// Find if we are inside a root-content element
							const rootNode = editor.dom.getParent(container, 'root-content');

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

					// Load & set back:
					const current = editorRef.current.getContent();
					editorRef.current.setContent(current);

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
							<PropEditor optionsVisibleFor={
								propsOpenFor.node
							} />
					}
					<Dialog.Footer>
						<Button type="button" onClick={() => setPropsOpenFor(null)}>
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
							const result = htmlToCanvasJson(currentHtml);
							// console.log(result);
							// throw new Error(result);
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