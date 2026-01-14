import { useState, useEffect } from 'react';
import { AutoApi, ApiIncludes } from 'Api/ApiEndpoints';
import { Content } from 'Api/Content';
import { ApiList } from 'UI/Functions/WebRequest';
import Loading from 'UI/Loading';


export type SelectInputType = React.SelectHTMLAttributes<HTMLSelectElement> & {
	contentType?: string,
	noSelectionValue?: string,
	noSelection?: string,
	clearable?: boolean,
	displayField?: string,
	contentTypeValue?: string,
	filter?: any,
	onDisplay?: (c: Content) => React.ReactNode
}

// Registering 'select' as being available
declare global {
	interface InputPropsRegistry {
		'select': SelectInputType;
	}
}

const Select: React.FC<CustomInputTypeProps<"select">> = (props) => {
	const [selectValue, setSelectValue] = useState<string | undefined>();
	const [options, setOptions] = useState<ApiList<Content> | null>(null);

	const { field, validationFailure, onInputRef } = props;
	let { onChange, contentType, noSelectionValue, clearable, children,
		noSelection, className, defaultValue, value,
		displayField, contentTypeValue, onDisplay, filter, ...attribs } = field;

	const onSelectChange = (e: React.FormEvent<HTMLSelectElement>) => {
		setSelectValue((e.target as HTMLSelectElement).value);
		onChange && onChange(e as React.ChangeEvent<HTMLSelectElement>);
	};

	useEffect(() => {
		if (!contentType) {
			return;
		}

		var module = require('Api/' + contentType);

		if (!module) {
			return;
		}

		var api = module.default as AutoApi<Content, ApiIncludes>;
		api.list(filter).then(setOptions);

	}, [contentType, filter]);

	var selectDefaultValue = typeof selectValue === 'undefined' ? defaultValue : selectValue;

	var selectClass = className || "form-select ui-form-select" + (validationFailure ? ' is-invalid' : '');

	if (value !== undefined) {
		selectDefaultValue = value;
	}

	if (selectDefaultValue == undefined) {
		selectClass += " no-selection";
	}

	if (noSelectionValue === undefined) {
		noSelectionValue = '';
	}

	if (noSelection === undefined) {
		noSelection = `Please select`;
	}

	if (contentType) {

		if (!options) {
			return <Loading />;
		}

		return <select id={props.id}
			ref={(el: HTMLSelectElement) => onInputRef && onInputRef(el)}
			onInput={(e) => {
				const select = e.target as HTMLSelectElement;
				var content = options && (options as any)[select.selectedIndex - 1];
				(e as any).content = content;
				onSelectChange(e);
			}}
			onChange={e => {
				const select = e.target as HTMLSelectElement;
				var content = options && (options as any)[select.selectedIndex - 1];
				(e as any).content = content;
				onSelectChange(e);
			}}
			value={selectDefaultValue}
			className={selectClass}
			{...attribs}
		>
			{clearable && <option
				selected={value == noSelectionValue || undefined ? true : undefined} value={noSelectionValue}>
				{noSelection}
			</option>}
			{options.results.map((entry : any) => <option
				value={contentTypeValue
					? entry[contentTypeValue]
					: entry.id}
				selected={contentTypeValue
					? entry[contentTypeValue] == selectDefaultValue ? true : undefined
					: entry.id == selectDefaultValue ? true : undefined
				}
			>
				{
					onDisplay ? onDisplay(entry) : entry[displayField || 'name']
				}
			</option>)}
		</select>;
	}

	return (
		<select id={props.id}
			ref={(el: HTMLSelectElement) => onInputRef && onInputRef(el)}
			onInput={onSelectChange}
			value={selectDefaultValue}
			className={selectClass}
			onChange={onSelectChange}
			{...attribs}
		>
			{clearable && <option disabled={clearable ? undefined : true}
				selected={value == noSelectionValue || undefined ? true : undefined} value={noSelectionValue}>
				{noSelection}
			</option>}
			{children}
		</select>
	);

}

window.inputTypes['select'] = Select;
export default Select;