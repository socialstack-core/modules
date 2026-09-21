import { useState, useEffect } from 'react';
import Input from 'UI/Input';
import Loading from 'UI/Loading';
import ContentSelectAny, { ContentSelectAnyProps } from 'Admin/ContentSelect/Any';
import { getAllContentTypes } from 'Admin/Functions/GetAutoForm';
import { Content } from 'Api/Database';
import { AutoFormInfo } from 'Api/AutoForms';

interface TypeInfo {
	name: string;
	searchField?: string;
}

const SEARCH_FIELD_PRIORITY = ['fullname', 'username', 'firstname', 'title', 'name', 'url', 'metatitle'];

function findSearchField(form: AutoFormInfo): string | undefined {
	if (!form?.fields) {
		return undefined;
	}

	for (const priority of SEARCH_FIELD_PRIORITY) {
		const field = form.fields.find(f => (f.data?.name as string)?.toLowerCase() === priority);
		if (field) {
			return field.data.name as string;
		}
	}

	return undefined;
}

interface ParsedTarget {
	type: string;
	id: number;
}

function parseTarget(target?: string): ParsedTarget | null {
	if (!target || target === '404') {
		return null;
	}

	var value = target.startsWith('primary:') ? target.substring('primary:'.length) : target;
	var separator = value.lastIndexOf(':');

	if (separator <= 0 || separator === value.length - 1) {
		return null;
	}

	var type = value.substring(0, separator);
	var id = parseInt(value.substring(separator + 1), 10);

	if (!type || isNaN(id)) {
		return null;
	}

	return { type, id };
}

function findType(name: string, list: string[]): string | null {
	var search = name.toLowerCase();
	return list.find(type => type.toLowerCase() === search) || null;
}

function buildTarget(type: string | null, id: number | null): string {
	if (!type || !id) {
		return '404';
	}

	var prefix = type.toLowerCase() === 'page' ? '' : 'primary:';
	return prefix + type.toLowerCase() + ':' + id;
}

export type TargetProps<T extends Content<uint>> = Omit<ContentSelectAnyProps<T>, 'name' | 'value' | 'defaultValue' | 'onChange'> & {
	/** Field name the serialised target is submitted under. */
	name?: string;
	/** The current permalink target, e.g. "page:42", "primary:safari:120" or "404". */
	value?: string;
	defaultValue?: string;
	/** Called with the serialised target string whenever the selection changes. */
	onChange?: (e: { target: { value: string; name?: string } }) => void;
};

export default function Target<T extends Content<uint>>(props: TargetProps<T>) {
	const { name, types: allowedTypes, value, defaultValue, onChange, search: searchProp, _isAdminSearch, label, ...restProps } = props;
	const target = value || defaultValue;

	const [types, setTypes] = useState<TypeInfo[] | null>(null);
	const [type, setType] = useState<string | null>(null);
	const [contentId, setContentId] = useState<number | null>(null);
	const [ready, setReady] = useState(false);

	useEffect(() => {
		if (allowedTypes) {
			setTypes(allowedTypes.map(name => ({ name })));
			return;
		}

		getAllContentTypes().then(forms => {
			setTypes(Object.values(forms).map(entry => ({
				name: entry.name,
				searchField: findSearchField(entry.form)
			})));
		});
	}, [allowedTypes]);

	// Rehydrate the current selection from the target string once the type list is available.
	useEffect(() => {
		if (!types) {
			return;
		}

		var names = types.map(t => t.name);
		var parsed = parseTarget(target);

		if (parsed) {
			var matched = findType(parsed.type, names);

			if (matched) {
				setType(matched);
				setContentId(parsed.id);
			}
		} else {
			setContentId(null);
		}

		setReady(true);
	}, [types, target]);

	function handleTypeChange(nextType: string | null) {
		setType(nextType);
		setContentId(null);

		if (onChange) {
			onChange({ target: { value: buildTarget(nextType, null), name } });
		}
	}

	function handleContentChange(e: { target: { value: number | null } }) {
		var id = e.target.value;
		setContentId(id);

		if (onChange) {
			onChange({ target: { value: buildTarget(type, id), name } });
		}
	}

	if (_isAdminSearch) {
		return <Input type="text" name={name} label={label} />;
	}

	if (!ready) {
		return <Loading />;
	}

	var typeNames = types?.map(t => t.name);
	var searchField = type ? types?.find(t => t.name === type)?.searchField : undefined;

	return (
		<div className="permalink-target">
			{name && (
				<input type="hidden" name={name} value={buildTarget(type, contentId)} readOnly />
			)}
			<ContentSelectAny
				{...restProps}
				types={typeNames}
				defaultType={type}
				onTypeChange={handleTypeChange}
				value={contentId}
				search={searchProp ? searchField : undefined}
				defaultValue={contentId}
				onChange={handleContentChange}
			/>
		</div>
	);
}