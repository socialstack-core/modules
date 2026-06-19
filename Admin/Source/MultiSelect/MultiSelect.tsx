import Search from 'UI/Search';
import Button from 'UI/Button';
import Canvas from 'UI/Canvas';
import Image from 'UI/Image';
import Link from 'UI/Link';
import Icon from 'UI/Icon';
import { Content } from 'Api/Database';
import { AutoController } from 'Api/Startup';
import * as fileRef from 'UI/FileRef';
import { isoConvert } from 'UI/Functions/DateTools';
import { ApiList, ApiIncludes } from 'UI/Functions/WebRequest';
import { useRef, useEffect, useState } from 'react';
import { ListFilter } from 'Api/Startup';

type MultiSelectChangeEvent = {
	target: { value: number[] };
	fullValue: Record<string, any>[];
};

export type MultiSelectProps<T extends Content<uint>> = {
	/** API content type name, e.g. "Tag" or "Category". */
	contentType: string;
	/** Current value — array of objects (with `id`) or plain ID numbers. */
	value?: T[];
	/** Initial value used when `value` is not provided. */
	defaultValue?: T[];
	/** Hidden input name used for form submission. */
	name?: string;
	/** Label text shown above the multi-select. */
	label?: string;
	/** If true the label is hidden. */
	hideLabel?: boolean;
	/** Field name used for searching. Falls back to a heuristic then "name". */
	field?: string;
	/** Field name used for display. Falls back to `field`. */
	displayField?: string;
	/** Maximum number of entries that can be selected. */
	max?: number;
	/** SocialStack includes passed to API list calls. */
	includes?: ApiIncludes[];
	/** Whether to show per-entry action buttons (edit). */
	showEntryActions?: boolean;
	/** Custom render function for each selected entry. */
	renderEntry?: (entry: T) => React.ReactNode;
	/** Custom render function for search result items. */
	renderSearchResult?: (entry: T) => React.ReactNode;
	/** Called when the edit button on an entry is clicked. */
	onEditEntry?: (entry: T) => void;
	/** Called when the "New" button is clicked. */
	onCreateEntry?: () => void;
	/** Called when the selection changes. */
	onChange?: (e: MultiSelectChangeEvent) => void;
	/** Called when the selection changes (raw variant). */
	onRawChange?: (e: MultiSelectChangeEvent) => void;
	/** Called to customise the search query. */
	onQuery?: (filter: ListFilter, query: string) => void;
};

/**
 * A general use "multi-selection"; primarily used for tags and categories.
 */
export default function MultiSelect<T extends Content<uint>>(props: MultiSelectProps<T>) {

	var initVal = (props.value || props.defaultValue || []).filter(t => t!=null);
	var initMustLoad = false;

	if(initVal.length && typeof initVal[0] == 'number'){
		initVal = ((initVal as any) as int[]).map(id => {return {id} as T});
		initMustLoad = true;
	}

	const [value, setValue] = useState<T[]>(initVal);
	const [mustLoad, setMustLoad] = useState<boolean>(initMustLoad);
	const inputRef = useRef<HTMLInputElement>(null);

	// Load full objects when initial value was just IDs (replaces componentDidMount)
	useEffect(() => {
		if (!mustLoad) {
			return;
		}

		var filter = {
			query: "Id=[?]",
			args: [value.map(e => e.id)]
		};

		var contentType = props.contentType || '';

		var api = require('Api/' + contentType).default;

		api.list(filter, props.includes).then((response : ApiList<T>) => {

			// Loading the values and preserving order:
			var idLookup : Record<string, T> = {};
			response.results.forEach(r => {idLookup[r.id+''] = r;});

			setMustLoad(false);
			setValue(value.map(e => idLookup[e.id+'']).filter(t=>t!=null));

		});
	}, []);

	// Sync value from props (replaces componentWillReceiveProps)
	useEffect(() => {
		if(props.value){
			setValue(props.value.filter(t => t!=null));
		}
	}, [props.value]);

	function remove(entry : T) {
		var newValue = value.filter(t => t!=entry && t!=null);
		runChange(newValue);
	}

	function runChange(newValue : T[]) {
		setValue(newValue);
		var e = { target: { value: newValue.map(e => e.id) }, fullValue: newValue };
		props.onRawChange && props.onRawChange(e);
		props.onChange && props.onChange(e);
	}

	var fieldName = props.field;

	if (!fieldName) {

		if (value && value.length) {
			let val = value[0] as any;
			let strings = Object.keys(val).filter(e => typeof val[e] === 'string' && e != 'type' && !e.endsWith('Ref') && e != 'media');

			if (strings.length) {
				var pref = ['name', 'description', 'title', 'summary'];

				pref.every(check => {

					if (strings.includes(check)) {
						fieldName = check;
						return false;
					}

					return true;
				});

			}

		}

	}

	var api = require('Api/' + props.contentType).default as AutoController<T, uint>;

	if (!fieldName) {
		fieldName = 'name';
	}

	var displayFieldName = props.displayField || fieldName;
	if(displayFieldName.length){
		displayFieldName = displayFieldName[0].toLowerCase() + displayFieldName.substring(1);
	}

	// check to see if the object has a media ref
	var mediaRefFieldName = '';

	if (value != undefined && value.length > 0) {
		var tempObject = value[0] as any;

		Object.keys(tempObject).every(key => {

			if (!tempObject[key]) {
				return true;
			}

			let val = tempObject[key].toString();

			if (fileRef.isRef(val)) {
			 	mediaRefFieldName = key;
			 	return false;
			}

			return true;
		});

	}

	// check to see if the object has a date range
	var metadataFields = [] as string[];
	var showmetadataFields = ["startdate", "enddate"];
	if (value != undefined && value.length > 0) {
		var tempObject = value[0] as any;

		Object.keys(tempObject).forEach(function (key, index) {
			if (tempObject[key] != null) {
				if (showmetadataFields.includes(key.toLowerCase())){
					metadataFields.push(key);
				}
			}
		});
	}

	var contentTypeLower = props.contentType ? props.contentType.toLowerCase() : "";

	var atMax = false;

	if (props.max && props.max > 0){
		atMax = (value.length >= props.max);
	}

	let excludeIds = value.map(a => a.id);

	return <>
		<div className="admin-multiselect mb-3">
			{props.label && !props.hideLabel && (
				<label className="form-label admin-multiselect__label">
					{props.label}
					<Link href={'/en-admin/' + contentTypeLower} target='_blank'>
						<Icon type='fa-external-link' />
					</Link>
				</label>
			)}
			<ul className="admin-multiselect__entries">
				{
					value.map((entry, i) => (
						<li key={entry.id} className="admin-multiselect__entry">
							<div>
								{props.renderEntry ? props.renderEntry(entry) : (
									displayFieldName.indexOf("Json") != -1 ? <Canvas>{(entry as any)[displayFieldName]}</Canvas> : (entry as any)[displayFieldName]
								)}
							</div>

							{metadataFields && metadataFields.length > 0 &&
								<div className="admin-multiselect__metadata">
									{metadataFields.map((metadataField) => (
										<div>{isoConvert((entry as any)[metadataField]).toUTCString()}</div>
									))}
								</div>
							}

							<div className="admin-multiselect__entry-options">
								{mediaRefFieldName && mediaRefFieldName.length > 0 &&
									<div className="admin-multiselect__avatar">
										{fileRef.isImage((entry as any)[mediaRefFieldName], false) && <>
											<Image fileRef={(entry as any)[mediaRefFieldName]} size={32} />
										</>}
										{fileRef.isVideo((entry as any)[mediaRefFieldName], false) && <>
											<i className="fa fa-2x far-file"></i>
										</>}
									</div>
								}

								{props.showEntryActions && props.onEditEntry && (
									<Button sm outlined className="btn-entry-select-action btn-view-entry" title={`Edit`}
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
					))
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

						return value.map(entry => entry.id);
					}
				}
			}} />
			<footer className="admin-multiselect__footer">
				{props.onCreateEntry && (
					<Button sm outlined className="btn-entry-select-action btn-new-entry"
						disabled={atMax ? true : undefined}
						onClick={e => {
							e.preventDefault();
							props.onCreateEntry!();
						}}
					>
						<i className="fal fa-fw fa-plus"></i> {`New`}
					</Button>
				)}
				<div className="admin-multiselect__search">
					{atMax ?
						<span className="admin-multiselect__search-max">
							<i>{`Max of ${props.max} added`}</i>
						</span> :
						<Search endpoint={query => api.list(query)} exclude={excludeIds} includes={props.includes} field={fieldName} limit={5}
							placeholder={`Find ${props.label} to add..`} onFind={entry => {
								if (!entry || value.some(entity => entity.id === entry.id)) {
									return;
								}

								runChange([...value, entry]);
							}}
							onQuery={props.onQuery}
							onRender={props.renderSearchResult}
							/>
					}
				</div>
			</footer>
		</div>
	</>;
}
