import { useEffect, useRef, useState } from "react";
import Input from 'UI/Input';
import MultiSelect from 'Admin/MultiSelect';
import getContentTypes from 'UI/Functions/GetContentTypes';

// 'json' type is registered by Admin/Source/ThirdParty/AceEditor/AceEditor.tsx

interface GenConfigEditorProps {
	name?: string;
	value?: string;
	defaultValue?: string;
	label?: string;
	hideLabel?: boolean;
	readonly?: boolean;
}

const GenConfigEditor: React.FC<GenConfigEditorProps> = (props) => {
	const readonly = props.readonly || false;
	const inputRef = useRef<HTMLInputElement>(null);

	const parseValue = (): { contentType?: string; filter?: { query?: string; args?: any[] } } | null => {
		var val = props.value || props.defaultValue || '';
		if (!val) return null;
		try {
			return JSON.parse(val);
		} catch {
			return null;
		}
	};

	const initialConfig = parseValue();

	const [enabled, setEnabled] = useState(initialConfig !== null);
	const [contentType, setContentType] = useState(initialConfig?.contentType || '');

	const getInitialFilterType = (): 'all' | 'ids' | 'custom' => {
		if (!initialConfig?.filter?.query) return 'all';
		if (initialConfig.filter.query === 'Id=[?]') return 'ids';
		return 'custom';
	};

	const [filterType, setFilterType] = useState<'all' | 'ids' | 'custom'>(getInitialFilterType());

	const [selectedIds, setSelectedIds] = useState<number[]>(() => {
		if (initialConfig?.filter?.query === 'Id=[?]' && initialConfig.filter.args?.[0]) {
			return initialConfig.filter.args[0];
		}
		return [];
	});

	const [customQuery, setCustomQuery] = useState(() => {
		if (initialConfig?.filter && initialConfig.filter.query !== 'Id=[?]') {
			return initialConfig.filter.query || '';
		}
		return '';
	});

	const [customArgs, setCustomArgs] = useState(() => {
		if (initialConfig?.filter && initialConfig.filter.query !== 'Id=[?]') {
			return JSON.stringify(initialConfig.filter.args);
		}
		return '[]';
	});

	const [multiSelectKey, setMultiSelectKey] = useState(0);
	const [contentTypes, setContentTypes] = useState<{ name: string }[]>([]);
	const isInitialMount = useRef(true);

	useEffect(() => {
		getContentTypes().then(types => {
			if (types) {
				setContentTypes(types.filter((t: any) => t.name && !t.name.startsWith('Revision<')).map((t: any) => ({ name: t.name as string })));
			}
		});
	}, []);

	useEffect(() => {
		const input = inputRef.current;
		if (input) {
			(input as any).onGetValue = (_v: string, _input: HTMLInputElement, _e: any) => {
				if (!enabled || !contentType) return '';

				var config: any = { contentType };

				if (filterType === 'ids' && selectedIds.length > 0) {
					config.filter = { query: 'Id=[?]', args: [selectedIds] };
				} else if (filterType === 'custom' && customQuery) {
					var args: any[];
					try {
						args = JSON.parse(customArgs || '[]');
					} catch {
						args = [];
					}
					config.filter = { query: customQuery, args };
				}

				return JSON.stringify(config);
			};
		}
	}, [enabled, contentType, filterType, selectedIds, customQuery, customArgs]);

	useEffect(() => {
		if (isInitialMount.current) {
			isInitialMount.current = false;
			return;
		}

		setSelectedIds([]);
		setCustomQuery('');
		setCustomArgs('[]');
		setMultiSelectKey(prev => prev + 1);
	}, [contentType]);

	const getSerializedValue = (): string => {
		if (!enabled || !contentType) return '';

		var config: any = { contentType };

		if (filterType === 'ids' && selectedIds.length > 0) {
			config.filter = { query: 'Id=[?]', args: [selectedIds] };
		} else if (filterType === 'custom' && customQuery) {
			var args: any[];
			try {
				args = JSON.parse(customArgs || '[]');
			} catch {
				args = [];
			}
			config.filter = { query: customQuery, args };
		}

		return JSON.stringify(config);
	};

	return (
		<div className="admin-nav-menu-gen-config-editor">
			{props.label && !props.hideLabel && (
				<label className="form-label">
					{props.label}
				</label>
			)}

			{!readonly && (
				<Input
					type="checkbox"
					label="Generate menu from content"
					checked={enabled || false}
					onChange={(e) => setEnabled((e.target as HTMLInputElement).checked)}
				/>
			)}

			{readonly && (
				<div className="gen-config-readonly-indicator">
					{enabled ? `Generative: ${contentType}` : `Not generative`}
				</div>
			)}

			{enabled && (
				<div className="gen-config-fields">
					{readonly ? (
					<Input
						type="text"
						label="Content type"
						value={contentType}
						readOnly
					/>
				) : (
					<Input
						type="select"
						label="Content type"
						value={contentType}
						onChange={(e) => setContentType((e.target as HTMLSelectElement).value)}
					>
						<option value="">Select content type</option>
						{contentTypes.map((ct) => (
							<option key={ct.name} value={ct.name}>{ct.name}</option>
						))}
					</Input>
				)}

					{!readonly && (
						<Input
							type="select"
							label="Filter"
							value={filterType}
							onChange={(e) => setFilterType((e.target as HTMLSelectElement).value as 'all' | 'ids' | 'custom')}
						>
							<option value="all">All content</option>
							<option value="ids">Specific IDs</option>
							<option value="custom">Custom query</option>
						</Input>
					)}

					{filterType === 'ids' && contentType && (
						<MultiSelect
							key={`ms-${contentType}-${multiSelectKey}`}
							contentType={contentType}
							value={selectedIds as any}
							onChange={(e: { target: { value: number[] } }) => setSelectedIds(e.target.value)}
							label="Select items"
						/>
					)}

					{filterType === 'custom' && (
						<>
							<Input
								type="text"
								label="Query"
								value={customQuery}
								onChange={(e) => setCustomQuery((e.target as HTMLInputElement).value)}
								placeholder="e.g. CategoryId=[?]"
								readOnly={readonly}
							/>
							<Input
								type="json"
								label="Args (JSON)"
								value={customArgs}
								onChange={(e) => setCustomArgs((e.target as HTMLInputElement).value)}
								readOnly={readonly}
							/>
						</>
					)}
				</div>
			)}

			<input
				type="hidden"
				name={props.name}
				ref={inputRef}
				value={getSerializedValue()}
			/>
		</div>
	);
};

export default GenConfigEditor;
