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
import MultiMediaSelect, { MultiMediaSelectProps } from 'Admin/MultiMediaSelect';

// Ghost drag feedback sits just below/right of the pointer so the entry it's over stays visible.
const GHOST_OFFSET_X = 14;
const GHOST_OFFSET_Y = 10;

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
	/** Optional help text. */
	help?: string;
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
	/** Builds the search filter used by the built-in search box, allowing multiple fields to be searched. */
	searchQuery?: (query: string) => ListFilter;
};

/**
 * A general use "multi-selection"; primarily used for tags and categories.
 */
export default function MultiSelect<T extends Content<uint>>(props: MultiSelectProps<T>) {
	
	const isUpload = (props.contentType || '').toLowerCase() == 'upload';
	var initVal = (props.value || props.defaultValue || []).filter(t => t != null);
	var initMustLoad = false;

	if (initVal.length && typeof initVal[0] == 'number') {
		initVal = ((initVal as any) as int[]).map(id => { return { id } as T });
		initMustLoad = true;
	}

	const [value, setValue] = useState<T[]>(initVal);
	const [mustLoad, setMustLoad] = useState<boolean>(initMustLoad);
	const inputRef = useRef<HTMLInputElement>(null);
	const [dragId, setDragId] = useState<string | null>(null);
	const [dragOverId, setDragOverId] = useState<string | null>(null);
	const [dragSnapshot, setDragSnapshot] = useState<string | null>(null);
	// Latest selection so drag release commits against current state, not the drag-start closure.
	const valueRef = useRef<T[]>(value);
	valueRef.current = value;
	// The entry being dragged, plus the hovered drop target. Refs avoid relying on the HTML5
	// drag and drop data store, which isn't delivered reliably in this application.
	const dragIdRef = useRef<string | null>(null);
	const dragOverIdRef = useRef<string | null>(null);
	// Ghost drag feedback: a snapshot of the dragged entry that follows the pointer,
	// positioned imperatively to avoid re-rendering the editor on every move.
	const dragStartRef = useRef<{ x: number; y: number } | null>(null);
	const ghostRef = useRef<HTMLDivElement>(null);

	// Load full objects when initial value was just IDs (replaces componentDidMount)
	useEffect(() => {
		if (!mustLoad || isUpload) {
			return;
		}

		var filter = {
			query: "Id=[?]",
			args: [value.map(e => e.id)]
		};

		var contentType = props.contentType || '';

		var api = require('Api/' + contentType).default;

		api.list(filter, props.includes).then((response: ApiList<T>) => {

			// Loading the values and preserving order:
			var idLookup: Record<string, T> = {};
			response.results.forEach(r => { idLookup[r.id + ''] = r; });

			setMustLoad(false);
			setValue(value.map(e => idLookup[e.id + '']).filter(t => t != null));

		});
	}, [mustLoad, isUpload, value, props.contentType, props.includes]);

	// Sync value from props (replaces componentWillReceiveProps)
	useEffect(() => {
		if (props.value && !isUpload) {
			setValue(props.value.filter(t => t != null));
		}
	}, [props.value, isUpload]);

	function remove(entry: T) {
		var newValue = value.filter(t => t != entry && t != null);
		runChange(newValue);
	}

	function runChange(newValue: T[]) {
		setValue(newValue);
		var e = { target: { value: newValue.map(e => e.id) }, fullValue: newValue };
		props.onRawChange && props.onRawChange(e);
		props.onChange && props.onChange(e);
	}

	const moveEntry = (fromId: string, toId: string, list: T[]): T[] => {
		const fromIndex = list.findIndex(entry => entry.id + '' === fromId);
		const toIndex = list.findIndex(entry => entry.id + '' === toId);

		if (fromIndex !== -1 && toIndex !== -1) {
			const newList = [...list];
			const [moved] = newList.splice(fromIndex, 1);
			newList.splice(toIndex, 0, moved);
			return newList;
		}

		return list;
	};

	const runChangeRef = useRef(runChange);
	runChangeRef.current = runChange;
	const moveEntryRef = useRef(moveEntry);
	moveEntryRef.current = moveEntry;

	const startDrag = (e: React.PointerEvent, id: string) => {
		if (e.button !== 0) {
			return;
		}
		e.preventDefault();
		dragIdRef.current = id;
		dragOverIdRef.current = null;
		dragStartRef.current = {
			x: e.clientX + GHOST_OFFSET_X,
			y: e.clientY + GHOST_OFFSET_Y
		};
		// Snapshot the dragged entry so a compact, width-matched "ghost" of it can follow
		// the pointer, minus its buttons, handle and avatar actions.
		const entryElement = (e.currentTarget as HTMLElement).closest('.admin-multiselect__entry') as HTMLElement | null;
		if (entryElement) {
			const width = entryElement.getBoundingClientRect().width;
			const clone = entryElement.cloneNode(true) as HTMLElement;
			clone.removeAttribute('style');
			clone.style.width = `${width}px`;
			// Hide anything interactive (buttons, the handle) from the ghost but keep the avatar.
			clone.querySelectorAll('.admin-multiselect__drag-handle, button, input, select, textarea').forEach(node => node.remove());
			clone.querySelectorAll('.admin-multiselect__entry-options').forEach(node => {
				if (!node.hasChildNodes()) {
					node.remove();
				}
			});
			clone.classList.add('admin-multiselect__ghost-entry');
			clone.classList.remove('dragging', 'drag-over');
			setDragSnapshot(clone.outerHTML);
		} else {
			setDragSnapshot(null);
		}
		setDragId(id);
		setDragOverId(null);
	};

	// While a drag is in progress, track the hovered entry and finish the move on release.
	useEffect(() => {
		if (!dragId || isUpload) {
			return;
		}

		const positionGhost = (clientX: number, clientY: number) => {
			if (ghostRef.current) {
				// Positioned imperatively so pointer moves don't re-render the editor.
				ghostRef.current.style.transform = `translate(${clientX + GHOST_OFFSET_X}px, ${clientY + GHOST_OFFSET_Y}px)`;
			}
		};

		const entryIdAt = (clientX: number, clientY: number): string | null => {
			if (typeof document === 'undefined') {
				return null;
			}
			const element = document.elementFromPoint(clientX, clientY) as HTMLElement | null;
			const entry = element && element.closest ? element.closest('.admin-multiselect__entry') : null;
			return entry ? entry.getAttribute('data-multiselect-item-id') : null;
		};

		const onPointerMove = (e: PointerEvent) => {
			positionGhost(e.clientX, e.clientY);
			const id = entryIdAt(e.clientX, e.clientY);
			if (id !== dragOverIdRef.current) {
				dragOverIdRef.current = id;
				setDragOverId(id);
			}
		};

		const finishDrag = (e: PointerEvent, commit: boolean) => {
			const fromId = dragIdRef.current;
			const toId = commit ? entryIdAt(e.clientX, e.clientY) : null;
			if (commit && fromId && toId && fromId !== toId) {
				const next = moveEntryRef.current(fromId, toId, valueRef.current);
				if (next !== valueRef.current) {
					runChangeRef.current(next);
				}
			}
			dragIdRef.current = null;
			dragOverIdRef.current = null;
			dragStartRef.current = null;
			setDragId(null);
			setDragOverId(null);
			setDragSnapshot(null);
		};

		const onPointerUp = (e: PointerEvent) => finishDrag(e, true);
		const onPointerCancel = (e: PointerEvent) => finishDrag(e, false);

		document.addEventListener('pointermove', onPointerMove);
		document.addEventListener('pointerup', onPointerUp);
		document.addEventListener('pointercancel', onPointerCancel);

		const startPosition = dragStartRef.current;
		if (startPosition) {
			positionGhost(startPosition.x, startPosition.y);
		}

		return () => {
			document.removeEventListener('pointermove', onPointerMove);
			document.removeEventListener('pointerup', onPointerUp);
			document.removeEventListener('pointercancel', onPointerCancel);
		};
	}, [dragId, isUpload]);

	// Upload content types are handled by the dedicated multi-media selector,
	// which reuses the same value/change-event contract as this component.
	if (isUpload) {
		return <MultiMediaSelect {...(props as unknown as MultiMediaSelectProps)} />;
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
	if (displayFieldName.length) {
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
				if (showmetadataFields.includes(key.toLowerCase())) {
					metadataFields.push(key);
				}
			}
		});
	}

	var contentTypeLower = props.contentType ? props.contentType.toLowerCase() : "";

	var atMax = false;

	if (props.max && props.max > 0) {
		atMax = (value.length >= props.max);
	}

	let excludeIds = value.map(a => a.id);

	var multiSelectClasses = 'admin-multiselect mb-3';
	if (dragId) {
		multiSelectClasses += ' dragging-active';
	}

	return <>
		<div className={multiSelectClasses}>
			{props.label && !props.hideLabel && (
				<label className="form-label admin-multiselect__label">
					{props.label}
					<Link href={'/en-admin/' + contentTypeLower} target='_blank'>
						<Icon type='fa-external-link' />
					</Link>
				</label>
			)}
			{props.help &&
				<div className={`form-text ui-form-text`}>
					{props.help}
				</div>
			}
			<ul className="admin-multiselect__entries">
				{
					value.map((entry, i) => {
						var entryClasses = 'admin-multiselect__entry';
						if (dragId === entry.id + '') {
							entryClasses += ' dragging';
						}
						if (dragOverId === entry.id + '') {
							entryClasses += ' drag-over';
						}

						return (
							<li key={entry.id} className={entryClasses} data-multiselect-item-id={entry.id}>
								<span
									className="admin-multiselect__drag-handle"
									title={`Drag to reorder`}
									onPointerDown={(e) => startDrag(e, entry.id + '')}
								>
									<i className="fal fa-fw fa-grip-vertical"></i>
								</span>

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
										<Button xs outlined className="btn-entry-select-action btn-view-entry" title={`Edit`}
											onClick={e => {
												e.preventDefault();
												props.onEditEntry!(entry);
											}}>
											<i className="fal fa-fw fa-edit"></i> <span className="sr-only">{`Edit`}</span>
										</Button>
									)}

									<Button xs outlined variant="danger" className="btn-entry-select-action btn-remove-entry" title={`Remove`}
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
			{dragId && dragSnapshot && (
				<div
					ref={ghostRef}
					className="admin-multiselect__ghost"
					dangerouslySetInnerHTML={{ __html: dragSnapshot }}
				/>
			)}
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
					<Button xs outlined className="btn-entry-select-action btn-new-entry"
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
						<Search endpoint={query => {

							if (props.searchQuery) {
								var search = props.searchQuery(((query && query.args && query.args[0]) || '') as string);

								if (search && search.query) {
									return api.list({ ...(query || {}), query: search.query, args: search.args });
								}
							}

							return api.list(query);
						}} exclude={excludeIds} includes={props.includes} field={fieldName} limit={5}
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
