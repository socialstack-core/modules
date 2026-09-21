import * as React from 'react';
import { useState, useEffect, useRef } from 'react';
import Button from 'UI/Button';
import Input from 'UI/Input';
import Tabs from 'UI/Tabs';
import getContentTypes from 'UI/Functions/GetContentTypes';
import {
	CodeModuleMeta,
	PropTypeMeta,
	TypeMeta,
	isJsx,
	getConstantUnion,
	getContentPropType,
	isNumericPropType,
	isBooleanPropType,
	isRefPropType,
	getTypeName,
	getArrayElementType,
	isFunctionPropType,
	CodeModuleType,
	CodeModuleTypeField
} from 'Admin/Functions/GetPropTypes';
import { rekeyRootsForArray } from '../Utils';

interface PropEditorProps {
	optionsVisibleFor?: ContentNode | null;
	links?: Record<string, LinkConfig>;
	onLinksChange?: (links: Record<string, LinkConfig>) => void;
}

interface ContentNode {
	typeName?: string;
	type?: string;
	typePropTypes?: CodeModuleMeta;
	typeMeta?: TypeMeta;
	props?: Record<string, unknown>;
	links?: Record<string, unknown>;
	/**
	 * The react-component DOM element this node represents, used to inspect rendered root content.
	 */
	element?: HTMLElement;
	_doFieldUpdate?: (fieldInfo: FieldInfo, value: unknown) => void;
}

interface FieldInfo {
	name: string;
	propType: PropTypeMeta;
	codeModuleMeta?: CodeModuleMeta;
	defaultValue?: unknown;
	value?: unknown;
}

interface ContentListItem {
	id: number;
	name?: string;
	title?: string;
	firstName?: string;
	[key: string]: unknown;
}

interface PropTypeExtra {
	label?: string;
	placeholder?: string;
	help?: string;
	helpPosition?: 'above' | 'below';
	disabledBy?: string;
	enabledBy?: string;
}

const DEFAULT_PROP_GROUP = `General`;

/**
 * Returns a shallow clone of obj with the value at the (possibly dot-pathed) field name set.
 * Arrays are cloned as arrays. Missing segments are created as arrays when the following segment
 * is numeric, or objects otherwise. Used so nested/array fields can share one bind path
 * (e.g. "items.0.content") no matter how many array levels deep they are.
 */
const setValueAtDotPath = (obj: any, fieldName: string, value: unknown): any => {
	if (fieldName.indexOf('.') == -1) {
		if (Array.isArray(obj)) {
			const next = [...obj];
			next[Number(fieldName)] = value;
			return next;
		}

		return { ...obj, [fieldName]: value };
	}

	var parts = fieldName.split('.');
	var next: any = Array.isArray(obj) ? [...obj] : { ...obj };
	var current = next;

	for (var i = 0; i < parts.length - 1; i++) {
		var part = parts[i];

		if (current[part] == undefined) {
			current[part] = /^\d+$/.test(parts[i + 1]) ? [] : {};
		} else if (Array.isArray(current[part])) {
			current[part] = [...current[part]];
		} else {
			current[part] = { ...current[part] };
		}

		current = current[part];
	}

	current[parts[parts.length - 1]] = value;

	return next;
};

/**
 * Gets the tab group a prop belongs to, based on its `@group` JSDoc tag.
 * Props without a group tag fall into the default group.
 */
const getPropGroup = (fieldInfo: FieldInfo): string => {
	const group = fieldInfo.propType?.meta?.group;
	const value = Array.isArray(group) ? group[0] : group;
	return value && value.length ? value : DEFAULT_PROP_GROUP;
};

/**
 * Partitions fields into tab groups in order of first appearance.
 * The default (General) group is moved to the front when other groups exist.
 */
const groupDataFields = (dataFields: Record<string, FieldInfo>): { group: string; fields: Record<string, FieldInfo> }[] => {
	const groups: { group: string; fields: Record<string, FieldInfo> }[] = [];
	const indexByGroup: Record<string, number> = {};

	Object.keys(dataFields).forEach(fieldName => {
		const fieldInfo = dataFields[fieldName];
		const group = getPropGroup(fieldInfo);

		if (indexByGroup[group] === undefined) {
			indexByGroup[group] = groups.length;
			groups.push({ group, fields: {} });
		}

		groups[indexByGroup[group]].fields[fieldName] = fieldInfo;
	});

	if (groups.length > 1) {
		const defaultGroupIndex = groups.findIndex(g => g.group == DEFAULT_PROP_GROUP);

		if (defaultGroupIndex > 0) {
			const [defaultGroup] = groups.splice(defaultGroupIndex, 1);
			groups.unshift(defaultGroup);
		}
	}

	return groups;
};

/**
 * True if the given root-content element contains any real content (ignoring TinyMCE's bogus filler elements).
 */
const rootHasContent = (rootEl: HTMLElement | null): boolean => {
	if (!rootEl) {
		return false;
	}

	let hasContent = false;

	rootEl.childNodes.forEach(child => {
		if (hasContent) {
			return;
		}

		if (child.nodeType == Node.TEXT_NODE) {
			hasContent = !!(child.textContent && child.textContent.trim().length);
		} else if (child.nodeType == Node.ELEMENT_NODE) {
			const el = child as HTMLElement;

			if (el.tagName.toLowerCase() == 'br' && el.getAttribute('data-mce-bogus')) {
				return;
			}

			if (el.tagName.toLowerCase() == 'img' || el.tagName.toLowerCase() == 'hr') {
				hasContent = true;
				return;
			}

			hasContent = rootHasContent(el);
		}
	});

	return hasContent;
};

const getDefaults = (typeName: string) => {
	let defaults = null;

	try {
		const module = require(typeName).default;
		defaults = module?.defaultProps;
	} catch (e) {
		console.warn(e);
	}

	return defaults;
};

interface LinkConfig {
	primary: boolean;
	field: string;
}

type RenderOptionSetFn = (
	dataFields: Record<string, FieldInfo>,
	targetNode: ContentNode,
	onFieldChange?: (fieldName: string, value: unknown) => void,
	linksForSet?: Record<string, LinkConfig>,
	onLinksChangeForSet?: (links: Record<string, LinkConfig>) => void,
	fieldPrefix?: string
) => React.ReactNode[];

interface ArrayPropEditorProps {
	label: string;
	fieldInfo: FieldInfo;
	targetNode: ContentNode;
	typeMeta: TypeMeta | undefined;
	locatedInterface: CodeModuleType;
	linksForSet?: Record<string, LinkConfig>;
	onLinksChangeForSet?: (links: Record<string, LinkConfig>) => void;
	onFieldChange?: (fieldName: string, value: unknown) => void;
	renderOptionSet: RenderOptionSetFn;
	interfaceFieldsToFieldInfo: (fields: CodeModuleTypeField[] | undefined, dataValues: any, cmm: CodeModuleMeta | undefined) => Record<string, FieldInfo>;
	niceName: (label: string, override?: string) => string;
}

/**
 * Edits a prop which is an array of interface-shaped items, including the case where
 * items are themselves arrays (arrays nested inside arrays). It is held at module scope
 * so its identity survives re-renders of the prop editor - defining it inline recreated
 * the component type every render which made React remount it, losing the local items.
 */
const ArrayPropEditor: React.FC<ArrayPropEditorProps> = ({ label, fieldInfo, targetNode, typeMeta, locatedInterface, linksForSet, onLinksChangeForSet, onFieldChange, renderOptionSet, interfaceFieldsToFieldInfo, niceName }) => {
	const [items, setItems] = useState<any[]>(() =>
		Array.isArray(fieldInfo.value)
			? fieldInfo.value.map((item: any) => ({ ...item, _key: Math.random().toString(36).substr(2) }))
			: []
	);
	const hiddenRef = useRef<HTMLInputElement>(null);
	const skipPropagate = useRef(true);
	const targetNodeRef = useRef(targetNode);
	targetNodeRef.current = targetNode;
	const onFieldChangeRef = useRef(onFieldChange);
	onFieldChangeRef.current = onFieldChange;

	useEffect(() => {
		if (hiddenRef.current) {
			(hiddenRef.current as any).onGetValue = () => items.map(({ _key, ...rest }) => rest);
		}

		if (skipPropagate.current) {
			skipPropagate.current = false;
			return;
		}

		// Reflect the (possibly nested) array back in to the node's working props so the
		// change accumulates in the parent data itself. Without this the new object only
		// lives in this editor's local state and is lost if anything re-renders the prop
		// editor. Nested arrays then bubble up via onFieldChange until they reach the top.
		const value = items.map(({ _key, ...rest }) => rest);

		if (targetNodeRef.current?.props) {
			targetNodeRef.current.props = setValueAtDotPath(targetNodeRef.current.props, fieldInfo.name, value) as Record<string, unknown>;
		}

		onFieldChangeRef.current?.(fieldInfo.name, value);
	}, [items, fieldInfo.name]);

	const addItem = () => {
		setItems([...items, { _key: Math.random().toString(36).substr(2) }]);
	};

	const removeItem = (index: number) => {
		// Shifting array layout must re-key the root content in the live DOM so existing
		// content follows items to their new positions (removed item roots are deleted).
		if (targetNode?.element) {
			rekeyRootsForArray(targetNode.element, fieldInfo.name, { removedAt: index });
		}

		setItems(items.filter((_, i) => i !== index));
	};

	return (
		<div className="array-builder">
			<label className="array-builder__label">{niceName(label)}</label>
			<div className="array-rows">
				{items.map((item, index) => (
					<details key={item._key} className="array-row" open>
						<summary>
							{`${niceName(label)} #${index + 1}`}
							<Button xs outlined variant="danger" onClick={() => removeItem(index)} className="array-row__remove">
								<i className="fal fa-fw fa-trash"></i>
								<span>
									{`Remove`}
								</span>
							</Button>
						</summary>
						<div className="array-row__fields">
							{renderOptionSet(
								interfaceFieldsToFieldInfo(locatedInterface.fields, item, fieldInfo.codeModuleMeta),
								targetNode,
								(fieldName, value) => {
									setItems(prev => setValueAtDotPath(prev, fieldName.slice((fieldInfo.name + '.').length), value));
								},
								linksForSet,
								onLinksChangeForSet,
								fieldInfo.name + '.' + index + '.'
							)}
						</div>
					</details>
				))}
			</div>
			<Button className="add-row-btn" onClick={addItem}>
				{`Add Item`}
			</Button>
			<input type="hidden" name={onFieldChange ? undefined : fieldInfo.name} ref={hiddenRef} />
		</div>
	);
};

const PropEditor: React.FC<PropEditorProps> = ({ optionsVisibleFor, links, onLinksChange }) => {
	const [inputKey, setInputKey] = useState(1);
	const [, forceUpdate] = useState({});
	const contentCacheByType = useRef<Record<string, ContentListItem[]>>({});
	const [activePropTab, setActivePropTab] = useState<string | null>(null);
	const [fieldValues, setFieldValues] = useState<Record<string, unknown>>(() => ({
		...optionsVisibleFor?.props
	}));

	useEffect(() => {
		setInputKey(k => k + 1);
		setActivePropTab(null);
		setFieldValues({ ...optionsVisibleFor?.props });
	}, [optionsVisibleFor]);

	const niceName = (label: string, override?: string): string => {
		if (override && override.length) {
			return override;
		}

		label = label.replace(/([^A-Z])([A-Z])/g, '$1 $2');
		return label[0].toUpperCase() + label.substring(1);
	};

	const specialField = (fieldName: string): boolean => {
		return fieldName == 'children' || (fieldName?.length > 0 && fieldName[0] == '_');
	};

	const handleFieldChange = (fieldName: string, value: unknown) => {
		setFieldValues(prev => setValueAtDotPath(prev, fieldName, value));
	};

	const getContentDropdown = (typeName: string, field?: string): React.ReactNode => {
		if (!contentCacheByType.current[typeName]) {
			contentCacheByType.current[typeName] = [];

			const api = require('Api/' + typeName).default;

			if (api != null) {
				api.list().then((response: { results: ContentListItem[] }) => {
					contentCacheByType.current[typeName] = response.results;
					forceUpdate({});
				});
			}
		}

		const set = contentCacheByType.current[typeName];

		return set.map((item, index) => {
			const name = (item.name || item.title || item.firstName || 'Untitled') + ' (#' + item.id + ')';
			const value = String(item[field || 'id'] ?? item.id);

			return (
				<option key={index} value={value}>{name}</option>
			);
		});
	};

	const ContentTypeDropdown: React.FC = () => {
		const [contentTypes, setContentTypes] = useState<{ name: string }[]>([]);

		useEffect(() => {
			getContentTypes().then(types => {
				if (types) {
					setContentTypes(types.filter(t => t.name).map(t => ({ name: t.name as string })));
				}
			});
		}, []);

		return contentTypes.map((item, index) => {
			const name = item.name;

			return (
				<option key={index} value={item.name.toLowerCase()}>{name}</option>
			);
		});
	};

	const typeToRenderableFields = (typeMeta: TypeMeta | undefined, codeModuleMeta: CodeModuleMeta | undefined, pt: Record<string, PropTypeMeta> | undefined, dataValues: any) => {
		const dataFields: Record<string, FieldInfo> = {};

		if (!pt) {
			return dataFields;
		}

		for (const fieldName in pt) {
			if (specialField(fieldName)) {
				continue;
			}

			const propType = pt[fieldName];

			if (isJsx(propType)) {
				continue;
			}

			let value: any = null;

			if (dataValues && dataValues[fieldName]) {
				value = dataValues[fieldName];
			}

			const fieldInfo: FieldInfo = { propType, codeModuleMeta, defaultValue: undefined, value, name: fieldName };
			dataFields[fieldName] = fieldInfo;
		}

		return dataFields;
	};

	const interfaceFieldsToFieldInfo = (fields: CodeModuleTypeField[] | undefined, dataValues: any, cmm: CodeModuleMeta | undefined): Record<string, FieldInfo> => {
		const dataFields: Record<string, FieldInfo> = {};

		if (!fields) {
			return dataFields;
		}

		for (const field of fields) {
			if (specialField(field.name)) {
				continue;
			}

			const propType: PropTypeMeta = {
				type: field.fieldType,
				meta: field.meta
			};

			if (isJsx(propType)) {
				continue;
			}

			let value: any = undefined;

			if (dataValues && dataValues[field.name] !== undefined) {
				value = dataValues[field.name];
			}

			dataFields[field.name] = {
				name: field.name,
				propType,
				codeModuleMeta: cmm,
				defaultValue: undefined,
				value
			};
		}

		return dataFields;
	};

	const renderGroupedOptions = (dataFields: Record<string, FieldInfo>, targetNode: ContentNode, linksForSet?: Record<string, LinkConfig>, onLinksChangeForSet?: (links: Record<string, LinkConfig>) => void): React.ReactNode => {
		const groups = groupDataFields(dataFields);

		if (groups.length <= 1) {
			return renderOptionSet(dataFields, targetNode, undefined, linksForSet, onLinksChangeForSet);
		}

		const currentTab = activePropTab && groups.some(g => g.group == activePropTab) ? activePropTab : groups[0].group;

		return (
			<Tabs
				key={'prop-tabs-' + inputKey}
				name="prop-editor-tabs"
				tabs={groups.map(g => g.group)}
				currentTab={currentTab}
				onChange={(tab) => setActivePropTab(String(tab))}
				renderPanel={(tab: string) => {
					const group = groups.find(g => g.group == tab);
					return group ? renderOptionSet(group.fields, targetNode, undefined, linksForSet, onLinksChangeForSet) : null;
				}}
			/>
		);
	};

	const renderOptions = (contentNode: ContentNode, linksForNode?: Record<string, LinkConfig>, onLinksChangeForNode?: (links: Record<string, LinkConfig>) => void): React.ReactNode => {
		if (!contentNode) {
			return null;
		}

		const moduleType = (contentNode?.type || '');

		const defaults = getDefaults(moduleType) || {};
		const dataValues = { ...defaults, ...contentNode.props };

		const codeModuleMeta = contentNode.typePropTypes;
		const typeMeta = contentNode.typeMeta;
		const pt = codeModuleMeta?.propTypes;
		let interfaceType = null;

		if (!pt) {
			// No explicit propTypes - look for a *Props type.
			var nameParts = moduleType.split('/');
			var baseName = nameParts[nameParts.length - 1];
			var allTypesInFile = codeModuleMeta?.types || [];
			interfaceType = allTypesInFile.find(type => type.instanceName == baseName + 'Props');
		}

		const dataFields = interfaceType ?
			interfaceFieldsToFieldInfo(interfaceType.fields, dataValues, codeModuleMeta) :
			typeToRenderableFields(typeMeta, codeModuleMeta, pt, dataValues);

		if (Object.keys(dataFields).length > 0) {
			return (
				<div>
					{renderGroupedOptions(dataFields, contentNode, linksForNode, onLinksChangeForNode)}
				</div>
			);
		}

		return (
			<div>
				{`No other options available`}
			</div>
		);
	};

	const renderOptionSet = (dataFields: Record<string, FieldInfo>, targetNode: ContentNode, onFieldChange?: (fieldName: string, value: unknown) => void, linksForSet?: Record<string, LinkConfig>, onLinksChangeForSet?: (links: Record<string, LinkConfig>) => void, fieldPrefix: string = ''): React.ReactNode[] => {
		const options: React.ReactNode[] = [];
		const typeMeta = targetNode.typeMeta;

		Object.keys(dataFields).forEach(fieldName => {
			const fieldInfo = dataFields[fieldName];
			var fullFieldName = fieldPrefix + fieldName;
			fieldInfo.name = fullFieldName;
			let label = fieldName;
			let inputType: string = 'text';
			let inputContent: React.ReactNode = undefined;
			const propType = fieldInfo.propType;
			const codeModuleMeta = fieldInfo.codeModuleMeta;
			const propTypeExtra = propType as unknown as PropTypeExtra;

			const depTag = (tag: string | string[] | undefined): string | undefined => {
				const raw = Array.isArray(tag) ? tag[0] : tag;
				return raw && raw.length ? raw : undefined;
			};

			const enabledByTag = depTag(propTypeExtra.enabledBy);

			if (enabledByTag) {
				const spaceIdx = enabledByTag.indexOf(' ');
				const controlField = spaceIdx > 0 ? enabledByTag.substring(0, spaceIdx) : enabledByTag;
				const requiredValue = spaceIdx > 0 ? enabledByTag.substring(spaceIdx + 1) : '';
				const currentValue = fieldValues[controlField];

				if (String(currentValue ?? '') !== requiredValue) {
					return;
				}
			}

			const disabledByTag = depTag(propTypeExtra.disabledBy);

			if (disabledByTag) {
				const spaceIdx = disabledByTag.indexOf(' ');
				const controlField = spaceIdx > 0 ? disabledByTag.substring(0, spaceIdx) : disabledByTag;
				const blockedValue = spaceIdx > 0 ? disabledByTag.substring(spaceIdx + 1) : '';
				const currentValue = fieldValues[controlField];

				if (String(currentValue ?? '') === blockedValue) {
					return;
				}
			}

			const arrayPropType = getArrayElementType(propType.type);

			const propLink = linksForSet?.[fieldName];

			if (propLink?.primary) {
				const linkField = propLink.field || '';

				options.push(
					<div key={inputKey + '_' + fieldName + '_link'} className="prop-editor__link-editor">
						<div className="prop-editor__link-editor-header">
							<span className="prop-editor__link-label">
								<i className="fal fa-link" /> {niceName(label, propTypeExtra.label)}
							</span>
							<Button xs outlined variant="danger"
								className="prop-editor__unlink-btn"
								onClick={() => {
									const updated = { ...linksForSet };
									delete updated[fieldName];
									onLinksChangeForSet?.(updated);
								}}
								title={`Remove link`}
							>
								<i className="fal fa-unlink" /> {`Unlink`}
							</Button>
						</div>
						<label className="prop-editor__link-field-label">
							{`Field path`}
						</label>
						<input
							type="text"
							className="prop-editor__link-field-input"
							value={linkField}
							onChange={(e) => {
								const updated = { ...linksForSet };
								updated[fieldName] = { ...propLink, field: e.target.value };
								onLinksChangeForSet?.(updated);
							}}
							placeholder="e.g. name, creatorUser.firstName"
						/>
					</div>
				);
				return;
			}

			if (arrayPropType && !propType.meta?.module) {
				if (arrayPropType.name == 'identifier') {
					const typeToFind = arrayPropType.instanceName;
					let locatedInterface: CodeModuleType | undefined;

					for (const k in typeMeta?.codeModules) {
						const cmm = typeMeta?.codeModules[k];
						const locatedType = cmm.types.find(type => type.name == 'interface' && type.instanceName == typeToFind);

						if (locatedType) {
							locatedInterface = locatedType;
							break;
						}
					}

					if (locatedInterface) {
						options.push(
							<div key={inputKey + '_' + fieldName} className="prop-editor__field-row">
								<ArrayPropEditor
									label={label}
									fieldInfo={fieldInfo}
									targetNode={targetNode}
									typeMeta={typeMeta}
									locatedInterface={locatedInterface}
									linksForSet={linksForSet}
									onLinksChangeForSet={onLinksChangeForSet}
									onFieldChange={onFieldChange}
									renderOptionSet={renderOptionSet}
									interfaceFieldsToFieldInfo={interfaceFieldsToFieldInfo}
									niceName={niceName}
								/>
								{onLinksChangeForSet && linksForSet !== undefined && (
									<Button xs outlined inline variant="ghost"
										className="prop-editor__link-btn"
										onClick={(e) => {
											e.preventDefault();
											onLinksChangeForSet({ ...linksForSet, [fieldName]: { primary: true, field: '' } });
										}}
										title={`Link to a data field`}
									>
										<i className="fal fa-link" />
									</Button>
								)}
							</div>
						);
					}
				}

				return;
			}

			if (isFunctionPropType(propType)) {
				return;
			}

			let customData: Record<string, any> = {};

			if (propType.meta?.data) {
				let dataTags = Array.isArray(propType.meta.data) ? propType.meta.data : [propType.meta.data];
				dataTags.forEach((tag: string) => {
					let spaceIdx = tag.indexOf(" ");
					if (spaceIdx > 0) {
						let dataKey = tag.substring(0, spaceIdx);
						let dataValStr = tag.substring(spaceIdx + 1);
						let dataVal: any = dataValStr;
						if (dataValStr === "true") dataVal = true;
						else if (dataValStr === "false") dataVal = false;
						else if (!isNaN(Number(dataValStr))) dataVal = Number(dataValStr);
						customData[dataKey] = dataVal;
					}
				});
			}

			var moduleKey = propType.meta?.module;

			if (moduleKey) {

				let moduleName = Array.isArray(moduleKey) ? moduleKey[0] : moduleKey;

				if (moduleName == 'hide' || moduleName == 'hidden') {
					// Like Hide=true on C#
					return;
				}

				let CustomModuleComponent;
				try {
					CustomModuleComponent = require(moduleName).default;
				} catch (e) {
					console.error("Failed to load generic @module: " + moduleName, e);
				}

				if (CustomModuleComponent) {
					let val = fieldInfo.value;
					if (val && typeof val === 'object' && (val as { type?: string }).type) {
						val = JSON.stringify(val);
					}

					options.push(
						<>
							<CustomModuleComponent
								key={inputKey + '_' + fieldName}
								name={fieldName}
								label={niceName(label, propTypeExtra.label)}
								defaultValue={val !== undefined ? val : fieldInfo.defaultValue}
								{...customData}
							/>
							{onLinksChangeForSet && linksForSet !== undefined && (
								<Button xs outlined inline variant="ghost"
									className="prop-editor__link-btn"
									onClick={(e) => {
										e.preventDefault();
										onLinksChangeForSet({ ...linksForSet, [fieldName]: { primary: true, field: '' } });
									}}
									title={`Link to a data field`}
								>
									<i className="fal fa-link" />
								</Button>
							)}
						</>
					);
					return;
				}
			}

			const constantUnion = getConstantUnion(propType, codeModuleMeta ?? {} as CodeModuleMeta, typeMeta);
			const contentTypeName = getContentPropType(propType);

			const isNumericSelect = constantUnion && constantUnion.every(v => !isNaN(Number(v)) && v !== '');
			const modulePropTypes = targetNode?.typePropTypes?.propTypes;

			if (isRefPropType(propType)) {
				inputType = 'file';
			} else if (fieldName.endsWith("Ref")) {
				inputType = 'file';
				label = label.substring(0, label.length - 3);
			} else if (constantUnion) {
				inputType = 'select';
				inputContent = constantUnion.map((entry, index) => {
					return (
						<option key={index} value={entry}>{entry === '' ? `(undefined)` : entry}</option>
					);
				});
				inputContent = (
					<>
						<option disabled value="">{`Pick a value`}</option>
						{inputContent}
					</>
				);
			} else if (contentTypeName) {
				inputType = 'select';
				inputContent = (
					<>
						<option disabled value="">{`Pick some content`}</option>
						{getContentDropdown(contentTypeName, 'id')}
					</>
				);
			} else if (isNumericPropType(propType)) {
				inputType = 'number';
			} else if (isBooleanPropType(propType)) {
				inputType = 'checkbox';
			} else {
				const customType = getTypeName(propType)?.toLowerCase();

				if (customType == 'htmlstring') {
					// Avoids another tinymce editor in the propEditor - it sort of works, but it errors and is generally unhappy about it!
					inputType = 'textarea';
				} else if (customType && (globalThis as unknown as { inputTypes: Record<string, unknown> }).inputTypes?.[customType]) {
					// @ts-ignore
					inputType = customType;
				} else {
					inputType = 'text';
				}
			}

			let val = fieldInfo.value;

			if (val && typeof val === 'object' && (val as { type?: string }).type) {
				val = JSON.stringify(val);
			}

			const placeholder = propTypeExtra.placeholder || (val == "" ? null : fieldInfo.defaultValue);
			let onChange: ((e: React.ChangeEvent) => void) | undefined = (e: React.ChangeEvent) => {
				const target = e.target as HTMLInputElement;
				const newValue = target.type === 'checkbox' ? target.checked : target.value;
				handleFieldChange(fullFieldName, newValue);
				if (targetNode?.props) {
					targetNode.props = setValueAtDotPath(targetNode.props, fullFieldName, newValue) as Record<string, unknown>;
				}
				if (onFieldChange) {
					onFieldChange(fullFieldName, newValue);
				}
			};

			var inputGroup = true;

			switch (inputType) {
				case 'color':
				case 'checkbox':
				case 'bool':
				case 'boolean':
				case 'radio':
					inputGroup = false;

					if (val == undefined && fieldInfo.defaultValue !== undefined && fieldInfo.defaultValue !== null) {
						val = fieldInfo.defaultValue;
					}
					break;

				case 'textarea':
					inputGroup = false;
					break;
			}

			const linked = onLinksChangeForSet && linksForSet !== undefined;
			const fieldId = `${inputKey}_${fieldName}`;

			options.push(
				<div key={fieldId} className="prop-editor__field--row">
					{linked ? <>
						{inputGroup && <>
							<label htmlFor={fieldId} className="form-label ui-form-label">
								{niceName(label, propTypeExtra.label)}
							</label>
							<div className="input-group mb-3">
								<Input
									key={fieldId}
									noWrapper
									type={inputType as keyof InputPropsRegistry}
									defaultValue={val as string | number | undefined}
									defaultChecked={inputType == 'checkbox' ? !!val : undefined}
									placeholder={placeholder as string | undefined}
									name={onFieldChange ? undefined : fieldName}
									onChange={onChange}
									{...customData}
								>
									{inputContent}
								</Input>
								<Button outlined
									className="prop-editor__link-btn"
									onClick={(e) => {
										e.preventDefault();
										onLinksChangeForSet?.({ ...linksForSet, [fieldName]: { primary: true, field: '' } });
									}}
									title={`Link to a data field`}
								>
									<i className="fal fa-link" />
								</Button>
							</div>
						</>}
						{!inputGroup && <>
							<Input
								key={fieldId}
								label={<>
									{niceName(label, propTypeExtra.label)}
									&nbsp;
									<Button xs outlined inline variant="ghost"
										className="prop-editor__link-btn"
										onClick={(e) => {
											e.preventDefault();
											onLinksChangeForSet({ ...linksForSet, [fieldName]: { primary: true, field: '' } });
										}}
										title={`Link to a data field`}
									>
										<i className="fal fa-link" />
									</Button>
								</>}
								type={inputType as keyof InputPropsRegistry}
								defaultValue={val as string | number | undefined}
								defaultChecked={inputType == 'checkbox' ? !!val : undefined}
								placeholder={placeholder as string | undefined}
								help={propTypeExtra.help}
								helpPosition={propTypeExtra.helpPosition}
								name={onFieldChange ? undefined : fieldName}
								onChange={onChange}
								{...customData}
							>
								{inputContent}
							</Input>
						</>}
					</> : <>
						<Input
							key={fieldId}
							label={niceName(label, propTypeExtra.label)}
							type={inputType as keyof InputPropsRegistry}
							defaultValue={val as string | number | undefined}
							defaultChecked={inputType == 'checkbox' ? !!val : undefined}
							placeholder={placeholder as string | undefined}
							help={propTypeExtra.help}
							helpPosition={propTypeExtra.helpPosition}
							name={onFieldChange ? undefined : fieldName}
							onChange={onChange}
							{...customData}
						>
							{inputContent}
						</Input>
					</>}
				</div>
			);
		});

		return options;
	};

	if (!optionsVisibleFor) {
		return null;
	}

	return <>
		<div className="prop-editor">
			{renderOptions(optionsVisibleFor, links, onLinksChange)}
		</div>
	</>;
};

export default PropEditor;