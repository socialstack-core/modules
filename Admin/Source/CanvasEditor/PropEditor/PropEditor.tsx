import * as React from 'react';
import { useState, useEffect, useRef } from 'react';
import Input from 'UI/Input';
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
	isFunctionPropType
} from 'Admin/Functions/GetPropTypes';

interface PropEditorProps {
	optionsVisibleFor?: ContentNode | null;
}

interface ContentNode {
	typeName?: string;
	type?: string;
	typePropTypes?: CodeModuleMeta;
	typeMeta?: TypeMeta;
	props?: Record<string, unknown>;
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

const PropEditor: React.FC<PropEditorProps> = ({ optionsVisibleFor }) => {
	const [inputKey, setInputKey] = useState(1);
	const [, forceUpdate] = useState({});
	const contentCacheByType = useRef<Record<string, ContentListItem[]>>({});

	useEffect(() => {
		setInputKey(k => k + 1);
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

	const getContentTypeDropdown = (): React.ReactNode => {
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

	const renderOptions = (contentNode: ContentNode): React.ReactNode => {
		if (!contentNode) {
			return null;
		}

		const dataFields: Record<string, FieldInfo> = {};
		let atLeastOneDataField = false;

		const dataValues = { ...contentNode.props };

		const codeModuleMeta = contentNode.typePropTypes;
		const typeMeta = contentNode.typeMeta;
		const pt = codeModuleMeta?.propTypes;

		if (pt) {
			for (const fieldName in pt) {
				if (specialField(fieldName)) {
					continue;
				}

				const propType = pt[fieldName];

				if (isJsx(propType)) {
					continue;
				}

				let value: unknown = null;

				if (dataValues[fieldName]) {
					value = dataValues[fieldName];
				}

				const fieldInfo: FieldInfo = { propType, codeModuleMeta, defaultValue: undefined, value, name: fieldName };
				dataFields[fieldName] = fieldInfo;
				atLeastOneDataField = true;
			}
		}

		if (atLeastOneDataField) {
			return (
				<div>
					{renderOptionSet(dataFields, contentNode)}
				</div>
			);
		}

		return (
			<div>
				{`No other options available`}
			</div>
		);
	};

	const renderOptionSet = (dataFields: Record<string, FieldInfo>, targetNode: ContentNode): React.ReactNode[] => {
		const options: React.ReactNode[] = [];
		const typeMeta = targetNode.typeMeta;

		Object.keys(dataFields).forEach(fieldName => {
			const fieldInfo = dataFields[fieldName];
			fieldInfo.name = fieldName;
			let label = fieldName;
			let inputType: string = 'text';
			let inputContent: React.ReactNode = undefined;
			const propType = fieldInfo.propType;
			const codeModuleMeta = fieldInfo.codeModuleMeta;
			const propTypeExtra = propType as unknown as PropTypeExtra;

			if (isFunctionPropType(propType)) {
				return;
			}

			if (propType.meta?.module) {
				let moduleName = Array.isArray(propType.meta.module) ? propType.meta.module[0] : propType.meta.module;
				let customData: Record<string, any> = {};

				if (propType.meta.data) {
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

				let CustomModuleComponent;
				try {
					CustomModuleComponent = require(moduleName).default;
				} catch(e) {
					console.error("Failed to load generic @module: " + moduleName, e);
				}

				if (CustomModuleComponent) {
					let val = fieldInfo.value;
					if (val && typeof val === 'object' && (val as { type?: string }).type) {
						val = JSON.stringify(val);
					}
					
					options.push(
						<CustomModuleComponent 
							key={inputKey + '_' + fieldName}
							name={fieldName}
							label={niceName(label, propTypeExtra.label)}
							defaultValue={val !== undefined ? val : fieldInfo.defaultValue}
							{...customData}
						/>
					);
					return;
				}
			}

			const constantUnion = getConstantUnion(propType, codeModuleMeta ?? {} as CodeModuleMeta, typeMeta);
			const contentTypeName = getContentPropType(propType);

			if (fieldName.endsWith("Ref") || isRefPropType(propType)) {
				inputType = 'file';
				label = label.substring(0, label.length - 3);
			} else if (constantUnion) {
				inputType = 'select';
				inputContent = constantUnion.map((entry, index) => {
					return (
						<option key={index} value={entry}>{entry}</option>
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

			switch (inputType) {
				case 'color':
				case 'checkbox':
				case 'bool':
				case 'boolean':
				case 'radio':
					if (val == undefined && fieldInfo.defaultValue !== undefined && fieldInfo.defaultValue !== null) {
						val = fieldInfo.defaultValue;
					}
					break;
			}

			options.push(
				<Input
					key={inputKey + '_' + fieldName}
					label={niceName(label, propTypeExtra.label)}
					type={inputType as keyof InputPropsRegistry}
					defaultValue={val as string | number | undefined}
					defaultChecked={inputType == 'checkbox' ? val === true || val === 'true' : undefined}
					placeholder={placeholder as string | undefined}
					help={propTypeExtra.help}
					helpPosition={propTypeExtra.helpPosition}
					name={fieldName}
				>
					{inputContent}
				</Input>
			);
		});

		return options;
	};

	if (!optionsVisibleFor) {
		return null;
	}

	return (
		<div className="prop-editor">
			{renderOptions(optionsVisibleFor)}
		</div>
	);
};

export default PropEditor;
