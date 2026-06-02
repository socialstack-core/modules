import Loop from 'UI/Loop';
import Row from 'UI/Row';
import Image from 'UI/Image';
import Dialog from 'UI/Dialog';
import Button from 'UI/Button';
import Uploader from 'UI/Uploader';
import * as fileRef from 'UI/FileRef';
import IconSelector from 'UI/FileSelector/IconSelector';
import useApi from 'UI/Functions/UseApi';
import Dropdown, { DropdownItem } from 'UI/Dropdown';
import Alert from 'UI/Alert';
import Col from 'UI/Column';
import Input from 'UI/Input';
import Search from 'UI/Search';
import uploadApi, { Upload } from 'Api/Upload';
import { Tag } from 'Api/Tag';
import { ListFilter } from 'Api/Startup';
import { ApiInclude, ApiList } from 'UI/Functions/WebRequest';
import { useEffect, useState, useRef } from "react";
import { DefaultInputType } from "UI/Input/Default";

let lastId = 0;

const CLOSEST_MULTIPLE = 2;
const PREVIEW_SIZE = 512 as int;

var searchFields = ['originalName', 'alt', 'author', 'id'];

type FileInputType = {
	accept?: string,
	name?: string,
	value?: string|null,
	defaultValue?: string | null,
	onChange?: (e: FileSelectEvent) => void,
	onBlur?: (e: React.FocusEvent) => void
};

declare global {
	interface InputPropsRegistry {
		'file': FileInputType,
		'image': FileInputType,
		'icon': FileInputType,
		'upload': FileInputType,
		'nopaging': FileInputType
	}
}

const FileSelectorInput: React.FC<CustomInputTypeProps<"file">> = (props) => {
	const { field, onInputRef } = props;
	return (
		<FileSelector
			{...field}
			onInputRef={onInputRef ? el => onInputRef(el as HTMLElement) : undefined}
		/>
	);
};

const IconSelectorInput: React.FC<CustomInputTypeProps<"icon">> = (props) => {
	const { field } = props;

	const [icon, setIcon] = useState<string | null | undefined>(field.defaultValue);
	const ref = useRef<HTMLInputElement>(null);

	useEffect(() => {
		props.onInputRef && props.onInputRef(ref.current!);
	}, [ref.current]);

	return (
		<>
			<input required={props.required} type={'hidden'} name={field.name} ref={ref} value={icon || ''} />
			<FileSelector
				iconOnly
				{...field}
				onChange={(value) => {
					setIcon(value.target.value);
					field.onChange && field.onChange(value);
				}}
				defaultValue={icon}
			/>
		</>
	);
};

const UploadSelectorInput: React.FC<CustomInputTypeProps<"upload">> = (props) => {
	const { field, onInputRef } = props;
	return (
		<FileSelector
			browseOnly
			{...field}
			onInputRef={onInputRef ? el => onInputRef(el as HTMLElement) : undefined}
		/>
	);
};

const NoPagingSelectorInput: React.FC<CustomInputTypeProps<"nopaging">> = (props) => {
	const { field, onInputRef } = props;
	return (
		<FileSelector
			disablePaging
			{...field}
			onInputRef={onInputRef ? el => onInputRef(el as HTMLElement) : undefined}
		/>
	);
};

window.inputTypes['file'] = FileSelectorInput;
window.inputTypes['image'] = FileSelectorInput;
window.inputTypes['icon'] = IconSelectorInput;
window.inputTypes['upload'] = UploadSelectorInput;
window.inputTypes['nopaging'] = NoPagingSelectorInput;

type ModalFileType = "all" | "img" | "vid" | "audio" | "doc" | "other";

export type FileSelectEvent = {
	target: {
		value: string | undefined,
		upload: Upload | undefined,
	}
};

type FileSelectorProps = {
	value?: string|null,
	defaultValue?: string|null,
	showActive?: boolean,
	iconOnly?: boolean,
	browseOnly?: boolean,
	uploadOnly?: boolean,
	isPrivate?: boolean,
	compact?: boolean,
	disablePaging?: boolean,
	maxSize?: number,
	url?: string,
	name?: string,
	id?: string,
	onInputRef?: React.Ref<HTMLInputElement>,
	onChange?: (e: FileSelectEvent) => void,
	requestOpts?: { headers?: Record<string, string> }
};

type EditedRefData = {
	alt: string,
	focalX: number,
	focalY: number,
	author: string
};

type UploadTagsProps = {
	combinedFilter: ListFilter,
	filterTagId: uint | null,
	setFilterTagId: (tagId: uint | null) => void
};

const UploadTags: React.FC<UploadTagsProps> = (props) => {
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
			<li className="file-selector__tag">
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
		<ul className='file-selector__tags'>
			{!!tags && tags.map(renderTag)}
		</ul>
	)
}

/**
 * Select a file from a users available uploads, outputting a ref.
 * You can use <Input type="file" .. /> to obtain one of these.
 */
const FileSelector = (props : FileSelectorProps) => {
	const ref : string = props.value || props.defaultValue || '';
	const [updatedRef, setUpdatedRef] = useState<string | null>(ref);
	const [editedRefData, setEditedRefData] = useState<EditedRefData | null>(null);
	const [showUploadModal, setShowUploadModal] = useState<boolean>(false);
	const [fileType, setFileType] = useState <ModalFileType>('all');
	const [searchFilter, setSearchFilter] = useState<string | null>(null);
	const [filterTagId, setFilterTagId] = useState<uint | null>(null);
	const [showIconModal, setShowIconModal] = useState<boolean>(false);
	const [originalName, setOriginalName] = useState<string | undefined>('');
	const currentRef = (updatedRef !== undefined ? updatedRef : ref) || '';
	
	const newId = () => {
		lastId++;
		return `fileselector${lastId}`;
	}

	const showRef = (ref: fileRef.FileRefIsh, size: int) => {
		var parsedRef = fileRef.parse(ref);
		size = size || 256 as int;
		var targetSize : number | undefined = size;
		//var minSize = size == 256 ? 238 : size;

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

	const updateValue = (e: React.MouseEvent<Element> | null, newRef?: Upload) => {
		if (e) {
			e.preventDefault();
		}

		var originalName = newRef ? newRef.originalName : '';

		setEditedRefData(null);
		setUpdatedRef(newRef ? newRef.ref : '');
		setShowUploadModal(false);
		setOriginalName(originalName || undefined);
		props.onChange && props.onChange({ target: { value: newRef?.ref || undefined, upload: newRef } });
	}

	const updateTextualValue = (newRef?: string) => {
		setEditedRefData(null);
		setUpdatedRef(newRef || '');
		setShowUploadModal(false);
		setOriginalName('');
		props.onChange && props.onChange({ target: { value: newRef, upload: undefined } });
	};

	const saveUpdates = () => {
		var pr = fileRef.parse(currentRef);
		if (!pr) {
			return;
		}
		pr.setNumericArg('fx', editedRefData!.focalX);
		pr.setNumericArg('fy', editedRefData!.focalY);
		pr.setArg('au', editedRefData!.author);
		pr.setArg('al', editedRefData!.alt);
		
		var newRef = pr.toString();
		setUpdatedRef(newRef);
		setEditedRefData(null);
	}

	const showEditModal = () => {
		var refInfo = fileRef.parse(currentRef);

		if (!refInfo) {
			return;
		}

		setEditedRefData({
			author: refInfo.author,
			alt: refInfo.altText,
			focalX: refInfo.focalX,
			focalY: refInfo.focalY
		});
	}

	const renderEditModal = () => {
		var parsedRef = fileRef.parse(currentRef);

		if (!parsedRef) {
			return null;
		}

		var isImage = parsedRef.isImage();
		var isVideo = parsedRef.isVideo();
		var title = `Edit`;

		return <>
			<Dialog title={title} isOpen={!!editedRefData} onClose={() => setEditedRefData(null)} className="media-center__upload-dialog">
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
										...editedRefData,
										focalX: CLOSEST_MULTIPLE * Math.round((offsetX / imagePreviewRect.width * 100) / CLOSEST_MULTIPLE),
										focalY: CLOSEST_MULTIPLE * Math.round((offsetY / imagePreviewRect.height * 100) / CLOSEST_MULTIPLE)
									} as EditedRefData);
								}}>
								{showRef(currentRef, PREVIEW_SIZE)}
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
										...editedRefData,
										author: (e.target as HTMLInputElement).value
									} as EditedRefData);
								}} />
							</div>

							<div className="form-text media-center__alt">
								<Input type="text" label={`Alternative Text`} value={editedRefData?.alt} onChange={e => {
									setEditedRefData({
										...editedRefData,
										alt: (e.target as HTMLInputElement).value
									} as EditedRefData);
								}} />
							</div>

							{isImage && !isVideo &&
								<div className="form-text media-center__focal-point">
									<Button sm variant="secondary" outlined onClick={() => {
										setEditedRefData({
											...editedRefData,
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
					<Button outlined onClick={() => setEditedRefData(null)}>
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
					onQuery={(where, query) => {
						setSearchFilter(query);
					}} />
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
		return <>
			<Alert variant="info">
				{combinedFilter.query ? `No matching uploads found` : `No uploads found`}
			</Alert>
		</>;
	}

	var hasRef = !!(currentRef && currentRef.length);
	var filename = hasRef ? fileRef.parse(currentRef)!.ref : "";
	
	if (originalName) {
		filename = originalName;
	}

	var source: (filter?: ListFilter, includes?: ApiInclude[]) => Promise<ApiList<Upload>>;

	if (props.showActive) {
		source = (filter?: ListFilter, includes?: ApiInclude[]) => uploadApi.active(includes);
	}else{
		source = (filter?: ListFilter, includes?: ApiInclude[]) => filter ? uploadApi.list(filter, includes) : uploadApi.listAll(includes);
	}

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

	return <div className="file-selector">

		{/* upload browser */}
		{showUploadModal && <Dialog isOpen={showUploadModal} onClose={() => setShowUploadModal(false)} className="image-select-dialog">
			<Dialog.Header>
				<h2 className="ui-dialog__title">
					{`Select an Upload`}
				</h2>
				{renderHeader()}
				<UploadTags combinedFilter={combinedFilter} filterTagId={filterTagId} setFilterTagId={setFilterTagId} />
			</Dialog.Header>

			<div className="file-selector__grid">
				<Loop source={source} filter={combinedFilter} paged={props.disablePaging ? undefined : true}
					orNone={() => renderEmpty()}>
					{
						entry => {
							// NB: API has been seen to report valid images with isImage=false
							//var isImage = entry.isImage;
							var isImage = fileRef.isImage(entry.ref!);

							// default to 256px preview
							var renderedSize : number | undefined = 256;
							var imageWidth = entry.width || 0;
							var imageHeight = entry.height || 0;
							var previewClass = "file-selector__preview ";

							// render image < 256px if original image size was smaller
							if (!isNaN(imageWidth) && !isNaN(imageHeight) &&
								imageWidth < renderedSize && imageHeight < renderedSize) {
								renderedSize = undefined;
								previewClass += "file-selector__preview--auto";
							}

							return <>
								<div className="loop-item">
									<Button allowWrap title={entry.originalName ?? undefined} className="file-selector__item" onClick={(e) => updateValue(e, entry)}>
										<div className={previewClass}>
											{isImage && <Image fileRef={entry.ref!} size={renderedSize} />}
											{!isImage && (
												<i className="fal fa-4x fa-file"></i>
											)}
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
			<Dialog.Footer>
				<Button onClick={() => setShowUploadModal(false)}>
					{`Close`}
				</Button>
			</Dialog.Footer>
		</Dialog>}

		{/* Edit Image Modal */}
		{!!editedRefData && renderEditModal()}

		{/* icon browser */}
		<IconSelector
			visible={showIconModal}
			onClose={() => {
				setShowIconModal(false)
			}}
			onSelected={
				icon => {
					updateTextualValue(icon);
				}
			}
		/>

		{/* upload */}
		<Uploader
			compact={props.compact}
			currentRef={currentRef}
			originalName={props.iconOnly ? currentRef : originalName}
			id={props.id || newId()}
			isPrivate={props.isPrivate}
			url={props.url}
			requestOpts={props.requestOpts}
			maxSize={props.maxSize}
			iconOnly={props.iconOnly}
			onUploaded={
				file => updateValue(null, file.result)
			} />

		{/* options (browse, preview, remove) */}
		<div className="file-selector__options">
			{!props.browseOnly && <>

				{props.uploadOnly &&
					<Button className="file-selector__select" onClick={() => setShowUploadModal(true)}>
						{`Select upload`}
					</Button>
				}

				{props.iconOnly &&
					<Button className="file-selector__select" onClick={() => setShowIconModal(true)}>
						{`Select icon`}
					</Button>
				}

				{!props.uploadOnly && !props.iconOnly &&
					<Dropdown label={`Change file`} variant="primary" className="file-selector__select" items={
						[
							{
								onClick: () => setShowUploadModal(true),
								text: `Select from uploads`
							},
							hasRef ? {
								onClick: () => updateValue(null, undefined),
								text: `Remove`
							} : null,
							(hasRef && !props.iconOnly) ? {
								onClick: () => showEditModal(),
								text: `Edit file`
							} : null
							/*
							Icons are not dedicated refs anymore. The modal has been fixed such 
							that it at least displays icons, but selecting one will error due to its ref output.
							{
								onClick: () => setShowIconModal(true),
								text: `From icons`
							}*/
						]
					} />
				}

			</>}
			{hasRef && <>
				{!props.iconOnly && <>
					<a href={fileRef.getUrl(currentRef)} title={filename} className="btn btn-primary file-selector__link" target="_blank" rel="noopener noreferrer">
						{`View file`}
					</a>
				</>}
			</>}
		</div>

		{props.name && (
			// Also contains a hidden input field containing the value
			<input ref={props.onInputRef} type="hidden" value={currentRef} name={props.name} id={props.id} />
		)}
	</div>;
}

export default FileSelector;