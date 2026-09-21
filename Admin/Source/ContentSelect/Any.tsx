import { useState, useEffect } from 'react';
import ContentSelect, { ContentSelectProps } from 'Admin/ContentSelect';
import Input from 'UI/Input';
import Loading from 'UI/Loading';
import { getAllContentTypes } from 'Admin/Functions/GetAutoForm';
import { Content } from 'Api/Database';

function findType(name: string, list: string[]): string | null {
	var search = name.toLowerCase();
	return list.find(type => type.toLowerCase() === search) || null;
}

export type ContentSelectAnyProps<T extends Content<uint>> = Omit<ContentSelectProps<T>, 'contentType'> & {
	/** Optional list of content type names to show in the dropdown. If not provided, all content types are loaded. */
	types?: string[];
	/** Content type to select initially, e.g. when rehydrating a saved value. */
	defaultType?: string | null;
	/** Called whenever the selected content type changes. */
	onTypeChange?: (type: string | null) => void;
};

export default function ContentSelectAny<T extends Content<uint>>(props: ContentSelectAnyProps<T>) {
	const { types: allowedTypes, defaultType, onTypeChange, ...restProps } = props;
	const [selectedType, setSelectedType] = useState<string | null>(defaultType || null);
	const [availableTypes, setAvailableTypes] = useState<string[] | null>(null);

	useEffect(() => {
		if (allowedTypes) {
			setAvailableTypes([...allowedTypes].sort((a, b) => a.localeCompare(b)));
			return;
		}

		getAllContentTypes().then(forms => {
			setAvailableTypes(Object.keys(forms).sort((a, b) => a.localeCompare(b)));
		});
	}, [allowedTypes]);

	// Apply the default type once the type list is ready, and when it changes.
	useEffect(() => {
		if (!availableTypes) {
			return;
		}

		setSelectedType(defaultType ? findType(defaultType, availableTypes) : null);
	}, [availableTypes, defaultType]);

	if (!availableTypes) {
		return <Loading />;
	}

	return (
		<div className="content-select-any">
			<Input
				type="select"
				label="Content type"
				value={selectedType || ''}
				onChange={e => {
					var val = (e.target as HTMLSelectElement).value;
					setSelectedType(val || null);
					if (onTypeChange) {
						onTypeChange(val || null);
					}
				}}
			>
				<option value="">Select a content type...</option>
				{availableTypes.map(type => (
					<option key={type} value={type}>{type}</option>
				))}
			</Input>
			{selectedType && (
				<ContentSelect key={selectedType} {...restProps as ContentSelectProps<T>} contentType={selectedType} />
			)}
		</div>
	);
}