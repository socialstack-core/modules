import { useRef, useEffect, useState } from 'react';
import Button from 'UI/Button';
import Link from 'UI/Link';
import Image from 'UI/Image';
import * as fileRef from 'UI/FileRef';
import { ApiList } from 'UI/Functions/WebRequest';
import ContentSelectAny from 'Admin/ContentSelect/Any';
import { MixedContentApi, ContentListRequest } from 'Api/MixedContents';

export type MixedSelectRef = {
	type: string;
	id: number;
};

export type MixedSelectEntry = {
	type: string;
	id: number;
	content?: Record<string, any> | null;
	[key: string]: any;
};

type MultiMixedSelectChangeEvent = {
	target: { value: MixedSelectRef[]; name?: string };
	fullValue: MixedSelectEntry[];
};

export type MultiMixedSelectProps = {
	/** Current value — refs ({type, id}) and/or full content objects. */
	value?: MixedSelectEntry[];
	/** Initial value used when `value` is not provided. */
	defaultValue?: MixedSelectEntry[];
	/** Hidden input name used for form submission. */
	name?: string;
	/** Label text shown above the multi-select. */
	label?: string;
	/** If true the label is hidden. */
	hideLabel?: boolean;
	/** Optional help text. */
	help?: string;
	/** Content types to show in the type dropdown. If not provided, all content types are loaded. */
	types?: string[];
	/** Field name used by the child content selector when rendering in search mode. */
	search?: string;
	/** If true (the default), the "New {content type}" link in the content selector is hidden. */
	hideCreateNew?: boolean;
	/** Maximum number of entries that can be selected. */
	max?: number;
	/** Custom render function for each selected entry. */
	renderEntry?: (entry: MixedSelectEntry) => React.ReactNode;
	/** Whether to show per-entry action buttons (edit). */
	showEntryActions?: boolean;
	/** Called when the edit button on an entry is clicked. */
	onEditEntry?: (entry: MixedSelectEntry) => void;
	/** Called when the "New" button is clicked, with the currently selected content type. */
	onCreateEntry?: (type: string | null) => void;
	/** Called when the selection changes. */
	onChange?: (e: MultiMixedSelectChangeEvent) => void;
	/** Called when the selection changes (raw variant). */
	onRawChange?: (e: MultiMixedSelectChangeEvent) => void;
};

function isFull(entry: any): boolean {
	var keys = Object.keys(entry || {}).filter(k => !['type', 'id', 'contentType'].includes(k));
	return keys.length > 0;
}

function normalize(vals: any[] | undefined): MixedSelectEntry[] {
	return (vals || []).filter(v => v != null).map(v => {
		var type = v.type || v.contentType || '';
		return { ...v, type };
	});
}

/**
 * A general use "multi-selection" which allows selecting multiple pieces of
 * content, of potentially different content types, in a single value.
 */
export default function MultiMixedSelect(props: MultiMixedSelectProps) {

	var { hideCreateNew = true } = props;

	var initVal = normalize(props.value || props.defaultValue);
	var initMustLoad = initVal.some(v => !isFull(v));

	const [value, setValue] = useState<MixedSelectEntry[]>(initVal);
	const [mustLoad, setMustLoad] = useState<boolean>(initMustLoad);
	const inputRef = useRef<HTMLInputElement>(null);
	const [pickerKey, setPickerKey] = useState(0);
	const [pickerType, setPickerType] = useState<string | null>(initVal.length ? initVal[0].type || null : null);

	useEffect(() => {
		if (!mustLoad) {
			return;
		}

		var partials = value.filter(v => !isFull(v));

		if (!partials.length) {
			setMustLoad(false);
			return;
		}

		var items = partials.map(v => ({ contentType: v.type, id: v.id }));

		MixedContentApi.getContentList({ items } as ContentListRequest, ['content'])
			.then((response: ApiList<any>) => {
				var results = response.results || [];
				var idLookup: Record<string, MixedSelectEntry> = {};

				results.forEach(r => {
					var type = r.contentType || r.type || '';
					idLookup[type + ':' + r.id] = {
						type,
						id: r.id,
						content: r.content || null
					};
				});

				setMustLoad(false);
				setValue(value.map(v => {
					if (isFull(v)) {
						return v;
					}

					return idLookup[(v.type || '') + ':' + v.id] || v;
				}));
			})
			.catch(() => {
				setMustLoad(false);
			});
	}, [mustLoad, value]);

	useEffect(() => {
		if (props.value) {
			setValue(normalize(props.value));
		}
	}, [props.value]);

	function remove(entry: MixedSelectEntry) {
		var newValue = value.filter(t => t != entry && t != null);
		runChange(newValue);
	}

	function runChange(newValue: MixedSelectEntry[]) {
		setValue(newValue);
		var e = { target: { value: newValue.map(v => ({ type: v.type, id: v.id })), name: props.name }, fullValue: newValue };
		props.onRawChange && props.onRawChange(e);
		props.onChange && props.onChange(e);
	}

	function handleAdd(e: any) {
		var id = e && e.target ? parseInt(e.target.value, 10) : 0;

		if (!id || id <= 0 || !pickerType) {
			return;
		}

		if (value.some(v => v.id === id && (v.type || '') === pickerType)) {
			return;
		}

		var full = (e && e.fullValue) || null;

		runChange([...value, { type: pickerType, id, content: full || null }]);
		setPickerKey(k => k + 1);
	}

	function getDisplayObject(entry: MixedSelectEntry): Record<string, any> {
		if (entry && entry.content && typeof entry.content === 'object') {
			return entry.content as Record<string, any>;
		}

		return entry as Record<string, any>;
	}

	function getDisplayTitle(entry: MixedSelectEntry): React.ReactNode {
		if (props.renderEntry) {
			return props.renderEntry(entry);
		}

		var obj = getDisplayObject(entry);

		if (!isFull(entry)) {
			return `${entry.type || 'Content'} #${entry.id}`;
		}

		var strings = Object.keys(obj).filter(k => typeof obj[k] === 'string' && obj[k] && !['type', 'id', 'contentType'].includes(k) && !k.endsWith('Ref'));

		if (strings.length) {
			var pref = ['name', 'title', 'description', 'summary', 'firstName', 'username', 'url', 'email'];

			for (var check of pref) {
				if (strings.includes(check)) {
					return obj[check];
				}
			}

			return obj[strings[0]];
		}

		return `${entry.type || 'Content'} #${entry.id}`;
	}

	function getMediaRefField(obj: Record<string, any>): string {
		var fieldName = '';

		Object.keys(obj).every(key => {
			if (!obj[key]) {
				return true;
			}

			if (fileRef.isRef(obj[key].toString())) {
				fieldName = key;
				return false;
			}

			return true;
		});

		return fieldName;
	}

	var atMax = false;

	if (props.max && props.max > 0) {
		atMax = (value.length >= props.max);
	}

	return <>
		<div className="multi-mixed-select mb-3">
			{props.label && !props.hideLabel && (
				<label className="form-label multi-mixed-select__label">
					{props.label}
				</label>
			)}
			{props.help &&
				<div className="form-text ui-form-text">
					{props.help}
				</div>
			}
			<ul className="multi-mixed-select__entries">
				{
					value.map(entry => {
						var obj = getDisplayObject(entry);
						var mediaRefField = getMediaRefField(obj);

						return (
							<li key={entry.type + ':' + entry.id} className="multi-mixed-select__entry">
								<div className="multi-mixed-select__entry-main">
									{entry.type && <span className="multi-mixed-select__type">{entry.type}</span>}
									<div className="multi-mixed-select__entry-display">{getDisplayTitle(entry)}</div>
								</div>

								<div className="multi-mixed-select__entry-options">
									{mediaRefField && mediaRefField.length > 0 &&
										<div className="multi-mixed-select__avatar">
											{fileRef.isImage(obj[mediaRefField], false) && <>
												<Image fileRef={obj[mediaRefField]} size={32} />
											</>}
											{fileRef.isVideo(obj[mediaRefField], false) && <>
												<i className="fa fa-2x far-file"></i>
											</>}
										</div>
									}

									{entry.id && entry.type && (
										<Link variant="primary" outlined sm
											href={'/en-admin/' + entry.type.toLowerCase() + '/' + entry.id}
											target="_blank"
											className="btn-entry-select-action btn-view-entry"
											title={`View`}>
											<i className="fal fa-fw fa-edit"></i> <span className="sr-only">{`View`}</span>
										</Link>
									)}

									{props.showEntryActions && props.onEditEntry && (
										<Button sm outlined className="btn-entry-select-action btn-edit-entry" title={`Edit`}
											onClick={e => {
												e.preventDefault();
												props.onEditEntry!(entry);
											}}>
											<i className="fal fa-fw fa-edit"></i> <span className="sr-only">{`Edit`}</span>
										</Button>
									)}

									<Button sm outlined variant="danger" className="btn-entry-select-action btn-remove-entry" title={`Remove`}
										onClick={e => {
											remove(entry);
											e.preventDefault();
										}}>
										<i className="fal fa-fw fa-times"></i> <span className="sr-only">{`Remove`}</span>
									</Button>
								</div>
							</li>
						);
					})
				}
			</ul>
			<input type="hidden" name={props.name} ref={ele => {
				inputRef.current = ele;

				if (ele != null) {
					// @ts-ignore
					ele.onGetValue = (v, input, e) => {

						if (input != inputRef.current) {
							return v;
						}

						return value.map(entry => ({ type: entry.type, id: entry.id }));
					}
				}
			}} />
			<footer className="multi-mixed-select__footer">
				{props.onCreateEntry && (
					<Button sm outlined className="btn-new-entry"
						disabled={atMax || !pickerType ? true : undefined}
						onClick={e => {
							e.preventDefault();
							props.onCreateEntry!(pickerType);
						}}
					>
						<i className="fal fa-fw fa-plus"></i> {`New`}
					</Button>
				)}
				<div className="multi-mixed-select__picker">
					{atMax ?
						<span className="multi-mixed-select__max">
							<i>{`Max of ${props.max} added`}</i>
						</span> :
						<ContentSelectAny
							key={pickerKey}
							types={props.types}
							defaultType={pickerType}
							onTypeChange={setPickerType}
							onChange={handleAdd}
							search={props.search}
							hideCreateNew={hideCreateNew}
						/>
					}
				</div>
			</footer>
		</div>
	</>;
}