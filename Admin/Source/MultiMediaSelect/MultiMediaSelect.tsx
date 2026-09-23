import Link from 'UI/Link';
import Icon from 'UI/Icon';
import Loop from 'UI/Loop';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Image from 'UI/Image';
import Dialog from 'UI/Dialog';
import Button from 'UI/Button';
import Uploader from 'UI/Uploader';
import Search from 'UI/Search';
import Input from 'UI/Input';
import Alert from 'UI/Alert';
import * as fileRef from 'UI/FileRef';
import contentChange from 'UI/Functions/ContentChange';
import useApi from 'UI/Functions/UseApi';
import { ApiList, ApiInclude } from 'UI/Functions/WebRequest';
import { ListFilter } from 'Api/Startup';
import uploadApi, { Upload } from 'Api/Upload';
import { Tag } from 'Api/Tag';
import { useCallback, useEffect, useState, useRef } from 'react';

let lastId = 0;

const CLOSEST_MULTIPLE = 2;
const PREVIEW_SIZE = 512 as int;

// Ghost drag feedback sits just below/right of the pointer so the entry it's over stays visible.
const GHOST_OFFSET_X = 14;
const GHOST_OFFSET_Y = 10;

var searchFields = ['originalName', 'alt', 'author', 'id'];

type ModalFileType = "all" | "img" | "vid" | "audio" | "doc" | "other";

export type MultiMediaSelectChangeEvent = {
	target: { value: number[] };
	fullValue: Upload[];
};

export type MultiMediaSelectProps = {
	/** Current value — array of upload objects (with `id`) or plain ID numbers. */
	value?: Upload[];
	/** Initial value used when `value` is not provided. */
	defaultValue?: Upload[];
	/** Hidden input name used for form submission. */
	name?: string;
	/** Label text shown above the multi-media-select. */
	label?: string;
	/** If true the label is hidden. */
	hideLabel?: boolean;
	/** Optional help text. */
	help?: string;
	/** Maximum number of uploads that can be selected. */
	max?: number;
	/** Whether to show per-entry action buttons (edit). */
	showEntryActions?: boolean;
	/** Custom render function for each selected entry. */
	renderEntry?: (entry: Upload) => React.ReactNode;
	/** Custom render function for search result items. */
	renderSearchResult?: (entry: Upload) => React.ReactNode;
	/** Called when the edit button on an entry is clicked. */
	onEditEntry?: (entry: Upload) => void;
	/** Called when the "New" button is clicked. */
	onCreateEntry?: () => void;
	/** Called to customise the search query. */
	onQuery?: (filter: ListFilter, query: string) => void;
	/** Called when the selection changes. */
	onChange?: (e: MultiMediaSelectChangeEvent) => void;
	/** Called when the selection changes (raw variant). */
	onRawChange?: (e: MultiMediaSelectChangeEvent) => void;
	/** Upload private flag, passed through to the dialog Uploader. */
	isPrivate?: boolean;
	/** Compact mode, passed through to the dialog Uploader. */
	compact?: boolean;
	/** Maximum upload size, passed through to the dialog Uploader. */
	maxSize?: number;
	/** Custom upload URL, passed through to the dialog Uploader. */
	url?: string;
	/** Extra upload request options, passed through to the dialog Uploader. */
	requestOpts?: { headers?: Record<string, string> };
};

type EditedRefData = {
	alt: string;
	focalX: number;
	focalY: number;
	author: string;
};

type TagFilterProps = {
	combinedFilter: ListFilter,
	filterTagId: uint | null,
	setFilterTagId: (tagId: uint | null) => void
};

const TagFilter: React.FC<TagFilterProps> = (props) => {
	const { combinedFilter, filterTagId, setFilterTagId } = props;

	const [tags] = useApi(() => uploadApi.list(combinedFilter, [uploadApi.includes.tags]).then(uploads => {

		const tagids: uint[] = [];
		const tags: Tag[] = [];

		uploads.results?.map(media => {
			media.tags?.map(tag => {
				if (!tagids.includes(tag.id)) {
					tagids.push(tag.id);
					tags.push(tag);
				}
			});
		});

		return tags;
	}), []);

	const renderTag = (tag: Tag) => {

		if (!tag || !tag.name || tag.name.length == 0) {
			return;
		}

		return (
			<li className="file-selector__tag multi-media-select__tag">
				<Button xs outlined={filterTagId != tag.id} onClick={() => {
					if (filterTagId && filterTagId == tag.id) {
						setFilterTagId(null);
					} else {
						setFilterTagId(tag.id);
					}
				}}>
					<i className={filterTagId == tag.id ? "fas fa-fw fa-tag" : "fal fa-fw fa-tag"}></i>
					{tag.name}
				</Button>
			</li>
		);
	}

	return (
		<ul className='file-selector__tags multi-media-select__tags'>
			{!!tags && tags.map(renderTag)}
		</ul>
	)
}

/**
 * Select multiple files from a users available uploads, outputting an array of upload ids.
 * The input/output contract mirrors Admin/MultiSelect while the browse experience
 * is based on UI/FileSelector (each tile toggles the upload in the selection).
 */
const MultiMediaSelect = (props: MultiMediaSelectProps) => {

	var initVal = (props.value || props.defaultValue || []).filter(t => t != null);
	var initMustLoad = false;

	if (initVal.length && typeof initVal[0] == 'number') {
		initVal = ((initVal as any) as int[]).map(id => {
			// Placeholder which gets replaced with the full upload object once loaded.
			return {
				id: id,
				type: 'Upload',
				originalName: null,
				fileType: null,
				variants: null,
				blurhash: null,
				width: null,
				height: null,
				focalX: null,
				focalY: null,
				alt: null,
				author: null,
				usageCount: null,
				isImage: false,
				isPrivate: false,
				isVideo: false,
				isAudio: false,
				transcodeState: 0,
				coverImageRef: null,
				ref: null
			} as Upload;
		});
		initMustLoad = true;
	}

	const [value, setValue] = useState<Upload[]>(initVal);
	const [mustLoad, setMustLoad] = useState<boolean>(initMustLoad);
	const inputRef = useRef<HTMLInputElement>(null);
	const [dragId, setDragId] = useState<string | null>(null);
	const [dragOverId, setDragOverId] = useState<string | null>(null);
	const [dragSnapshot, setDragSnapshot] = useState<string | null>(null);
	// Latest selection so drag release commits against current state, not the drag-start closure.
	const valueRef = useRef<Upload[]>(value);
	valueRef.current = value;
	// The entry being dragged, plus the hovered drop target. Refs avoid relying on the HTML5
	// drag and drop data store, which isn't delivered reliably in this application.
	const dragIdRef = useRef<string | null>(null);
	const dragOverIdRef = useRef<string | null>(null);
	// Ghost drag feedback: a snapshot of the dragged entry that follows the pointer,
	// positioned imperatively to avoid re-rendering the editor on every move.
	const dragStartRef = useRef<{ x: number; y: number } | null>(null);
	const ghostRef = useRef<HTMLDivElement>(null);
	const [showDialog, setShowDialog] = useState<boolean>(false);
	const [fileType, setFileType] = useState<ModalFileType>('all');
	const [searchFilter, setSearchFilter] = useState<string | null>(null);
	const [filterTagId, setFilterTagId] = useState<uint | null>(null);
	const [editedEntry, setEditedEntry] = useState<Upload | null>(null);
	const [editedRefData, setEditedRefData] = useState<EditedRefData | null>(null);
	const [uploaderId] = useState<string>(() => {
		lastId++;
		return `multimedia${lastId}`;
	});

	const searchTimer = useRef<number | null>(null);

	// Debounce the dialog search so the media grid only reloads once typing pauses.
	const onSearchQuery = (where: ListFilter, query: string) => {
		props.onQuery && props.onQuery(where, query);

		if (searchTimer.current) {
			window.clearTimeout(searchTimer.current);
		}

		searchTimer.current = window.setTimeout(() => {
			setSearchFilter(query ? query : null);
		}, 300);
	};

	useEffect(() => {
		return () => {
			if (searchTimer.current) {
				window.clearTimeout(searchTimer.current);
			}
		};
	}, []);

	// Load full objects when initial value was just IDs (replaces componentDidMount)
	useEffect(() => {
		if (!mustLoad) {
			return;
		}

		var filter = {
			query: "Id=[?]",
			args: [value.map(e => e.id)]
		} as ListFilter;

		uploadApi.list(filter).then((response: ApiList<Upload>) => {

			// Loading the values and preserving order:
			var idLookup: Record<string, Upload> = {};
			response.results.forEach(r => { idLookup[r.id + ''] = r; });

			setMustLoad(false);
			setValue(value.map(e => idLookup[e.id + '']).filter(t => t != null));

		});
	}, [mustLoad]);

	// Sync value from props (replaces componentWillReceiveProps)
	useEffect(() => {
		if (props.value) {
			setValue(props.value.filter(t => t != null));
		}
	}, [props.value]);

	function remove(entry: Upload) {
		var newValue = value.filter(t => t != entry && t != null);
		runChange(newValue);
	}

	function runChange(newValue: Upload[]) {
		setValue(newValue);
		var e: MultiMediaSelectChangeEvent = { target: { value: newValue.map(e => e.id) }, fullValue: newValue };
		props.onRawChange && props.onRawChange(e);
		props.onChange && props.onChange(e);
	}

	const moveEntry = (fromId: string, toId: string, list: Upload[]): Upload[] => {
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
		// the pointer, minus its buttons and handle but keeping the thumbnail.
		const entryElement = (e.currentTarget as HTMLElement).closest('.multi-media-select__entry') as HTMLElement | null;
		if (entryElement) {
			const width = entryElement.getBoundingClientRect().width;
			const clone = entryElement.cloneNode(true) as HTMLElement;
			clone.removeAttribute('style');
			clone.style.width = `${width}px`;
			// Hide anything interactive (buttons, the handle); keep the thumbnail preview.
			clone.querySelectorAll('.multi-media-select__drag-handle, button, input, select, textarea').forEach(node => node.remove());
			clone.querySelectorAll('.multi-media-select__entry-options').forEach(node => {
				if (!node.hasChildNodes()) {
					node.remove();
				}
			});
			clone.classList.add('multi-media-select__ghost-entry');
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
		if (!dragId) {
			return;
		}

		const positionGhost = (clientX: number, clientY: number) => {
			if (ghostRef.current) {
				// Positioned imperatively so pointer moves don't re-render the component.
				ghostRef.current.style.transform = `translate(${clientX + GHOST_OFFSET_X}px, ${clientY + GHOST_OFFSET_Y}px)`;
			}
		};

		const entryIdAt = (clientX: number, clientY: number): string | null => {
			if (typeof document === 'undefined') {
				return null;
			}
			const element = document.elementFromPoint(clientX, clientY) as HTMLElement | null;
			const entry = element && element.closest ? element.closest('.multi-media-select__entry') : null;
			return entry ? entry.getAttribute('data-multimedia-item-id') : null;
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
				const next = moveEntry(fromId, toId, valueRef.current);
				if (next !== valueRef.current) {
					runChange(next);
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
	}, [dragId]);

	var atMax = false;

	if (props.max && props.max > 0) {
		atMax = (value.length >= props.max);
	}

	var isSelected = (id: uint) => value.some(v => v.id == id);

	function toggle(entry: Upload) {
		if (isSelected(entry.id)) {
			remove(entry);
		} else if (!atMax) {
			runChange([...value, entry]);
		}
	}

	const onUploaded = (upload: Upload | undefined) => {
		if (!upload) {
			return;
		}
		contentChange(upload, 'upload/create', { deleted: false, updated: false, created: true, added: true });
		if (isSelected(upload.id) || atMax) {
			return;
		}
		runChange([...value, upload]);
	};

	const showRef = (ref: fileRef.FileRefIsh, size: int) => {
		var parsedRef = fileRef.parse(ref);
		size = size || 256 as int;
		var targetSize: number | undefined = size;

		if (!parsedRef) {
			return null;
		}

		// Check if it's an image/ video/ audio file. If yes, a preview is shown. Otherwise it'll be a placeholder icon.
		var canShowImage = parsedRef.isImage();

		if (canShowImage) {
			var argW = parsedRef.getNumericArg('w', 0);
			var argH = parsedRef.getNumericArg('h', 0);
			if ((argW && argW < size) && (argH && argH < size)) {
				targetSize = undefined;
			}
		}

		return canShowImage ?
			<Image fileRef={ref} size={targetSize} /> :
			<span className="fal fa-4x fa-file"></span>;
	}

	const renderThumb = (entry: Upload, size: number) => {
		var ref = entry.ref;

		if (!ref) {
			return <i className="fal fa-2x fa-file multi-media-select__thumb-icon"></i>;
		}

		if (fileRef.isVideo(ref)) {
			return <i className="fal fa-2x fa-video multi-media-select__thumb-icon"></i>;
		}

		if (!fileRef.isImage(ref)) {
			return <i className="fal fa-2x fa-file multi-media-select__thumb-icon"></i>;
		}

		return <Image fileRef={ref} size={size} />;
	}

	const handleEditEntry = (entry: Upload, e: React.MouseEvent) => {
		e.preventDefault();

		if (props.onEditEntry) {
			props.onEditEntry(entry);
			return;
		}

		var refInfo = fileRef.parse(entry.ref || '');

		if (!refInfo) {
			return;
		}

		setEditedEntry(entry);
		setEditedRefData({
			author: refInfo.author,
			alt: refInfo.altText,
			focalX: refInfo.focalX,
			focalY: refInfo.focalY
		});
	}

	const saveUpdates = () => {
		if (!editedEntry || !editedRefData) {
			return;
		}

		var pr = fileRef.parse(editedEntry.ref || '');
		if (!pr) {
			return;
		}

		pr.setNumericArg('fx', editedRefData.focalX);
		pr.setNumericArg('fy', editedRefData.focalY);
		pr.setArg('au', editedRefData.author);
		pr.setArg('al', editedRefData.alt);

		var newRef = pr.toString();
		var newValue = value.map(e => e.id == editedEntry.id ? { ...e, ref: newRef } : e);

		setEditedEntry(null);
		setEditedRefData(null);
		runChange(newValue);
	}

	const closeEditModal = () => {
		setEditedEntry(null);
		setEditedRefData(null);
	}

	const renderEditModal = () => {
		if (!editedEntry || !editedRefData) {
			return null;
		}

		var parsedRef = fileRef.parse(editedEntry.ref || '');

		if (!parsedRef) {
			return null;
		}

		var isImage = parsedRef.isImage();
		var isVideo = parsedRef.isVideo();

		return <>
			<Dialog title={`Edit`} isOpen={!!editedRefData} onClose={() => closeEditModal()} className="media-center__upload-dialog">
				<Row>
					<Col sizeMd='8'>
						<Alert type='info'>
							{`Click the image to set its focal point.`}
						</Alert>
						<div className='media-center__preview-wrapper'>
							<div className="media-center__preview"
								onClick={(e) => {
									const anyE = e as any;
									const offsetX = anyE.offsetX as number;
									const offsetY = anyE.offsetY as number;
									var imagePreviewRect = (e.target as HTMLDivElement).getBoundingClientRect();
									setEditedRefData({
										...editedRefData!,
										focalX: CLOSEST_MULTIPLE * Math.round((offsetX / imagePreviewRect.width * 100) / CLOSEST_MULTIPLE),
										focalY: CLOSEST_MULTIPLE * Math.round((offsetY / imagePreviewRect.height * 100) / CLOSEST_MULTIPLE)
									} as EditedRefData);
								}}>
								{showRef(editedEntry.ref || '', PREVIEW_SIZE)}
								{isImage && !isVideo && <>
									<div className="media-center__preview-crosshair" style={{
										left: editedRefData?.focalX + '%',
										top: editedRefData?.focalY + '%'
									}}></div>
								</>}
							</div>
						</div>
					</Col>

					<Col sizeMd='4'>
						<div className="media-center__metadata">

							<div className="form-text media-center__alt">
								<Input type="text" label={`Author/Photographer`} value={editedRefData?.author} onChange={e => {
									setEditedRefData({
										...editedRefData!,
										author: (e.target as HTMLInputElement).value
									} as EditedRefData);
								}} />
							</div>

							<div className="form-text media-center__alt">
								<Input type="text" label={`Alternative Text`} value={editedRefData?.alt} onChange={e => {
									setEditedRefData({
										...editedRefData!,
										alt: (e.target as HTMLInputElement).value
									} as EditedRefData);
								}} />
							</div>

							{isImage && !isVideo &&
								<div className="form-text media-center__focal-point">
									<Button sm variant="secondary" outlined onClick={() => {
										setEditedRefData({
											...editedRefData!,
											focalX: 50,
											focalY: 50
										} as EditedRefData);
									}}>
										<i className="fal fa-fw fa-sync"></i>{`Reset focal point`}
									</Button>
								</div>
							}

						</div>
					</Col>
				</Row>
				<Dialog.Footer>
					<Button outlined onClick={() => closeEditModal()}>
						{`Cancel`}
					</Button>
					<Button onClick={() => saveUpdates()}>
						{`Save`}
					</Button>
				</Dialog.Footer>
			</Dialog>
		</>;
	}

	const renderHeader = () => {

		if (!searchFields) {
			return;
		}

		return <>
			<div className="image-select-dialog__filters">
				<Search className="admin-page__search" placeholder={`Search`}
					onQuery={(where, query) => onSearchQuery(where, query)} />
				<Input type="select"
					label={`File Type`}
					noWrapper
					value={fileType}
					onChange={(e) => setFileType((e.target as HTMLSelectElement).value as ModalFileType)}>
					<option key="all" value="all">
						{`All`}
					</option>
					<option key="img" value="img">
						{`Image`}
					</option>
					<option key="vid" value="vid">
						{`Video`}
					</option>
					<option key="audio" value="audio">
						{`Audio`}
					</option>
					<option key="doc" value="doc">
						{`Document`}
					</option>
					<option key="other" value="other">
						{`Other`}
					</option>
				</Input>
			</div>
		</>;

	}

	const renderEmpty = () => {
		return <div className="multi-media-select__empty">
			<Alert variant="info">
				{combinedFilter.query ? `No matching uploads found` : `No uploads found`}
			</Alert>
		</div>;
	}

	const source: (filter?: ListFilter, includes?: ApiInclude[] | undefined) => Promise<ApiList<Upload>> = useCallback(
		(filter?: ListFilter, includes?: ApiInclude[] | undefined) => filter ? uploadApi.list(filter, includes) : uploadApi.listAll(includes),
		[]
	);

	// do we need to search ?
	var combinedFilter = {
		sort: {
			field: 'CreatedUtc',
			direction: 'desc'
		},
		query: '',
		args: []
	} as ListFilter;

	if (filterTagId) {
		combinedFilter.query = "Tags contains ?"
		combinedFilter.args.push(filterTagId);
	}

	if (fileType?.length && fileType != 'all') {
		if (combinedFilter.query) {
			combinedFilter.query += ` AND FileType ${fileType == 'other' ? "!=" : "="} [?]`;
		} else {
			combinedFilter.query = `FileType ${fileType == 'other' ? "!=" : "="} [?]`;
		}

		switch (fileType) {

			case 'img':
				combinedFilter.args.push(fileRef.allImageTypes);
				break;

			case 'vid':
				combinedFilter.args.push(fileRef.allVideoTypes);
				break;

			case 'audio':
				combinedFilter.args.push(fileRef.allAudioTypes);
				break;

			case 'doc':
				combinedFilter.args.push(fileRef.allDocumentTypes);
				break;

			case 'other':
				combinedFilter.args.push(fileRef.allImageTypes.concat(fileRef.allVideoTypes, fileRef.allAudioTypes, fileRef.allDocumentTypes));
				break;
		}

	}

	if (searchFilter && searchFilter.length > 0 && searchFields) {
		var searchQuery = '';
		var searchQueryArgs = [];
		var searchDelimiter = '';

		for (var i = 0; i < searchFields.length; i++) {

			var field = searchFields[i];
			var fieldNameUcFirst = field.charAt(0).toUpperCase() + field.slice(1);

			if (fieldNameUcFirst == "Id") {
				if (/^\d+$/.test(searchFilter)) {

					searchQuery = searchQuery + searchDelimiter + fieldNameUcFirst + " =?"
					searchQueryArgs.push(searchFilter);
					searchDelimiter = ' OR ';
				}
			} else {
				searchQuery = searchQuery + searchDelimiter + fieldNameUcFirst + " contains ?"
				searchQueryArgs.push(searchFilter);

				searchDelimiter = ' OR ';
			}
		}

		if (searchQuery.length > 0) {
			if (!combinedFilter.query) {
				combinedFilter.query = searchQuery;
				combinedFilter.args = searchQueryArgs;
			} else {
				combinedFilter.query = combinedFilter.query + ' AND (' + searchQuery + ')';
				searchQueryArgs.forEach(arg => combinedFilter.args.push(arg));
			}
		}

	}

	var addButtonLabel = value.length ? `Select uploads` : `Add uploads`;

	var multiMediaClasses = "file-selector multi-media-select";
	if (dragId) {
		multiMediaClasses += ' dragging-active';
	}

	return <>
		<div className={multiMediaClasses}>

			{props.label && !props.hideLabel && (
				<label className="form-label multi-media-select__label">
					{props.label}
					<Link href={'/en-admin/uploads'} target='_blank'>
						<Icon type='fa-external-link' />
					</Link>
				</label>
			)}
			{props.help &&
				<div className={`form-text ui-form-text`}>
					{props.help}
				</div>
			}

			{/* selected uploads */}
			<ul className="multi-media-select__entries">
				{
					value.map(entry => {
						var entryClasses = 'multi-media-select__entry';
						if (dragId === entry.id + '') {
							entryClasses += ' dragging';
						}
						if (dragOverId === entry.id + '') {
							entryClasses += ' drag-over';
						}

						return (
							<li key={entry.id} className={entryClasses} data-multimedia-item-id={entry.id}>
								<span
									className="multi-media-select__drag-handle"
									title={`Drag to reorder`}
									onPointerDown={(e) => startDrag(e, entry.id + '')}
								>
									<i className="fal fa-fw fa-grip-vertical"></i>
								</span>

								<div className="multi-media-select__entry-main">
									{props.renderEntry ? props.renderEntry(entry) : (
										<span className="multi-media-select__entry-display">
											<span className="multi-media-select__thumb">
												{renderThumb(entry, 32)}
											</span>
											<span className="multi-media-select__entry-name">
												{entry.originalName}
											</span>
										</span>
									)}
								</div>

								<div className="multi-media-select__entry-options">
									{props.showEntryActions && (
										<Button sm outlined className="btn-entry-select-action btn-view-entry" title={`Edit`}
											onClick={e => handleEditEntry(entry, e)}>
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
			{dragId && dragSnapshot && (
				<div
					ref={ghostRef}
					className="multi-media-select__ghost"
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

			<footer className="multi-media-select__footer">
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
				<Button className="file-selector__select multi-media-select__add-btn" onClick={() => setShowDialog(true)}>
					<i className="fal fa-fw fa-images"></i> {addButtonLabel}
				</Button>
			</footer>

			{/* upload browser */}
			{showDialog && <Dialog isOpen={showDialog} onClose={() => setShowDialog(false)} className="image-select-dialog multi-media-select__dialog">
				<Dialog.Header>
					<h2 className="ui-dialog__title">
						{`Select Uploads`}
					</h2>
					{renderHeader()}
					<TagFilter combinedFilter={combinedFilter} filterTagId={filterTagId} setFilterTagId={setFilterTagId} />
				</Dialog.Header>

				<div className="file-selector__grid multi-media-select__grid">
					<Loop source={source} filter={combinedFilter} paged
						loader={() => (
							<div className="multi-media-select__loading">
								<i className="fal fa-fw fa-spinner fa-spin"></i> {`Loading`}
							</div>
						)}
						orNone={() => renderEmpty()}>
						{
							entry => {
								// NB: API has been seen to report valid images with isImage=false
								var isImage = fileRef.isImage(entry.ref!);

								// default to 256px preview
								var renderedSize: number | undefined = 256;
								var imageWidth = entry.width || 0;
								var imageHeight = entry.height || 0;
								var previewClass = "file-selector__preview ";

								// render image < 256px if original image size was smaller
								if (!isNaN(imageWidth) && !isNaN(imageHeight) &&
									imageWidth < renderedSize && imageHeight < renderedSize) {
									renderedSize = undefined;
									previewClass += "file-selector__preview--auto";
								}

								var selected = isSelected(entry.id);

								return <>
									<div className="loop-item">
										<Button allowWrap title={entry.originalName ?? undefined} className={"file-selector__item multi-media-select__item" + (selected ? " multi-media-select__item--selected" : "")} onClick={(e) => {
											e.preventDefault();
											toggle(entry);
										}}>
											<div className={previewClass}>
												{isImage && <Image fileRef={entry.ref!} size={renderedSize} />}
												{!isImage && (
													<i className="fal fa-4x fa-file"></i>
												)}
												{selected && <i className="fas fa-check multi-media-select__checked"></i>}
											</div>
											<span className="file-selector__name">
												{entry.originalName}
											</span>
										</Button>
									</div>
								</>;
							}

						}
					</Loop>
				</div>

				<div className="multi-media-select__uploader">
					<Uploader
						multiple
						compact={props.compact}
						id={uploaderId}
						isPrivate={props.isPrivate}
						url={props.url}
						requestOpts={props.requestOpts}
						maxSize={props.maxSize}
						onUploaded={
							file => onUploaded(file.result)
						} />
				</div>

				<Dialog.Footer>
					<span className="multi-media-select__selected-count">
						{atMax ? `Max of ${props.max} added` : (props.max && props.max > 0 ? `${value.length} of ${props.max} selected` : `${value.length} selected`)}
					</span>
					<label htmlFor={uploaderId} className="btn btn-primary file-selector__select multi-media-select__upload-btn">
						<i className="fal fa-fw fa-upload"></i> {`Upload`}
					</label>
					<Button outlined onClick={() => setShowDialog(false)}>
						{`Done`}
					</Button>
				</Dialog.Footer>
			</Dialog>}

			{/* Edit Image Modal */}
			{!!editedRefData && renderEditModal()}

		</div>
	</>;
}

export default MultiMediaSelect;