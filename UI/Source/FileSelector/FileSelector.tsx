import Loop from 'UI/Loop';
import Container from 'UI/Container';
import Row from 'UI/Row';
import Image from 'UI/Image';
import Modal from 'UI/Modal';
import Uploader from 'UI/Uploader';
import * as fileRef from 'UI/FileRef';
import IconSelector from 'UI/FileSelector/IconSelector';
import Dropdown from 'UI/Dropdown';
import Alert from 'UI/Alert';
import Col from 'UI/Column';
import Input from 'UI/Input';
import Search from 'UI/Search';
import uploadApi from 'Api/Upload';
import { useEffect, useState, useRef } from "react";

var inputTypes = global.inputTypes = global.inputTypes || {};
let lastId = 0;

const CLOSEST_MULTIPLE = 2;
const PREVIEW_SIZE = 512;

var searchFields = ['originalName', 'alt', 'author', 'id'];

window.inputTypes['file'] = window.inputTypes['image'] = function (props) {
	const { field } = props;
	return (
		<FileSelector
			{...field}
			onInputRef={props.onInputRef}
		/>
	);
};

window.inputTypes['icon'] = function (props) {
	const { field } = props;
	
	const [icon, setIcon] = useState(field.defaultValue);
	const ref = useRef(null);

	useEffect(() => {
		props.onInputRef && props.onInputRef(ref.current);
	}, [ref.current]);
	
	return (
		<>
			<input required={props.required} type={'hidden'} name={field.name} ref={ref} value={icon} />
			<FileSelector
				iconOnly
				{...props}
				onChange={(value) => {
					setIcon(value.target.value);
					field.onChange && field.onChange(value);
				}}
				defaultValue={icon}
			/>
		</>
	);
};

window.inputTypes['upload'] = function (props) {
	const { field } = props;
	return (
		<FileSelector
			browseOnly
			{...field}
			onInputRef={props.onInputRef}
		/>
	);
};

window.inputTypes['nopaging'] = function (props) {
	const { field } = props;
	return (
		<FileSelector
			disablePaging
			{...field}
			onInputRef={props.onInputRef}
		/>
	);
};

/**
 * Select a file from a users available uploads, outputting a ref.
 * You can use <Input type="file" .. /> to obtain one of these.
 */
const FileSelector = (props) => {
	var ref = props.value || props.defaultValue;
	const [updatedRef, setUpdatedRef] = useState(ref);
	const [editedRefData, setEditedRefData] = useState();
	const [showUploadModal, setShowUploadModal] = useState();
	const [searchFilter, setSearchFilter] = useState();
	const [filterTagId, setFilterTagId] = useState();
	const [showIconModal, setShowIconModal] = useState(false);
	const [originalName, setOriginalName] = useState('');
	const currentRef = updatedRef !== undefined ? updatedRef : ref;
	
	const newId = () => {
		lastId++;
		return `fileselector${lastId}`;
	}

	const showRef = (ref, size) => {
		var parsedRef = fileRef.parse(ref);
		var size = size || 256;
		var targetSize = size;
		//var minSize = size == 256 ? 238 : size;

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
			<Image fileRef={ref} size={targetSize} portraitCheck /> :
			<span className="fal fa-4x fa-file"></span>;
	}

	const updateValue = (e, newRef) => {
		if (e) {
			e.preventDefault();
		}

		var originalName = newRef ? newRef.originalName : '';

		if (!newRef) {
			newRef = '';
		}
		
		if (newRef.result && newRef.result.ref) {
			// Accept upload objects also.
			newRef = newRef.result.ref;
		} else if (newRef.ref) {
			newRef = newRef.ref;
		}

		setEditedRefData(null);
		setUpdatedRef(newRef);
		setShowUploadModal(false);
		setOriginalName(originalName);
		props.onChange && props.onChange({ target: { value: newRef } });
	}

	const saveUpdates = (e) => {
		var pr = fileRef.parse(currentRef);
		pr.setNumericArg('fx', editedRefData.focalX);
		pr.setNumericArg('fy', editedRefData.focalY);
		pr.setArg('au', editedRefData.author);
		pr.setArg('al', editedRefData.alt);
		
		var newRef = pr.toString();
		setUpdatedRef(newRef);
		setEditedRefData(null);
	}

	const closeUploadModal = () => {
		setShowUploadModal(false);
	}

	const showEditModal = () => {
		var refInfo = fileRef.parse(currentRef);

		setEditedRefData({
			author: refInfo.author,
			alt: refInfo.altText,
			focalX: refInfo.focalX,
			focalY: refInfo.focalY
		});
	}

	const closeEditModal = () => {
		setEditedRefData(null);
	}

	const renderEditModal = () => {
		var parsedRef = fileRef.parse(currentRef);
		var isImage = parsedRef.isImage();
		var isVideo = parsedRef.isVideo();
		var title = `Edit`;

		return <>
			<Modal isExtraLarge title={title}
				buttons={[
					{
						label: `Close`,
						onClick: closeEditModal
					}
				]}
				onClose={closeEditModal}
				visible={!!editedRefData}
				className="media-center__upload-modal">
				<div className="media-center__upload-modal-internal">
					<Container>
						<Row>
								<Col sizeMd='9'>
									<Alert type='info'>
										{`Click the image to set its focal point.`}
									</Alert>
									<div className='media-center__preview-wrapper'>
										<div className="media-center__preview"
											onClick={(e) => {
												var imagePreviewRect = e.target.getBoundingClientRect();
												setEditedRefData({
													...editedRefData,
													focalX: CLOSEST_MULTIPLE * Math.round((e.offsetX / imagePreviewRect.width * 100) / CLOSEST_MULTIPLE),
													focalY: CLOSEST_MULTIPLE * Math.round((e.offsetY / imagePreviewRect.height * 100) / CLOSEST_MULTIPLE)
												});
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

								<Col sizeMd='3'>
									<div className="media-center__metadata">

										<div className="form-text media-center__alt">
										<Input type="text" label={`Author/Photographer`} value={editedRefData?.author} onChange={e => {
												setEditedRefData({
													...editedRefData,
													author: e.target.value
												});
											}} />
										</div>

										<div className="form-text media-center__alt">
										<Input type="text" label={`Alternative Text`} value={editedRefData?.alt} onChange={e => {
											setEditedRefData({
												...editedRefData,
												alt: e.target.value
											});
										}} />
									</div>

										{isImage && !isVideo &&
											<div className="form-text media-center__focal-point">
												<button type="button" className="btn btn-sm btn-outline-secondary me-2" onClick={() => {
													setEditedRefData({
														...editedRefData,
														focalX: 50,
														focalY: 50
													});
												}}>
													<i className="fal fa-fw fa-sync"></i>{` Reset focal point to center`}
												</button>
											</div>
										}

									</div>
								</Col>
						</Row>
					</Container>
				</div>

				<footer className="media-center__upload-modal-footer">
					<div className="media-center__upload-modal-footer-options">
						<button type="button" className="btn btn-outline-primary" onClick={() => closeEditModal()}>
							{`Cancel`}
						</button>
						<button type="button" className="btn btn-primary" onClick={() => saveUpdates()}>
							{`Save`}
						</button>
					</div>
				</footer>
			</Modal>
		</>;
	}

	const renderTag = (tag) => {
		if (!tag || !tag.name || tag.name.length == 0) {
			return ('');
		}

		var tagClassName = (filterTagId == tag.id) ? "file-selector__tag file-selector__tag-selected" : "file-selector__tag"

		return (
			<li className={tagClassName} onClick={() => {
				
				if (filterTagId && filterTagId == tag.id) {
					setFilterTagId(null);
				} else {
					setFilterTagId(tag.id);
				}
			}}>
				{tag.name}
			</li>
		);
	}

	const renderTags = (combinedFilter) => {
		var tagids = [];
		var tags = [];
		
		return (
			<ul className='file-selector__tags'>
				<Loop over={uploadApi} filter={combinedFilter} includes={[uploadApi.includes.tags]} onResults={results => {
					results.map(media => {
						media.tags?.map(tag => {
							if (!tagids.includes(tag.id)) {
								tagids.push(tag.id);
								tags.push(tag);
							}
						});
					});

					return tags;
				}}>
					{renderTag}
				</Loop>
			</ul>
		)
	}

	const renderHeader = () => {
		return <div className="row header-container file-selector__search">
			{searchFields && <>
				<Search className="admin-page__search" placeholder={`Search`}
					onQuery={(where, query) => {
						setSearchFilter(query);
					}} />
			</>}
		</div>;
	}
	
	var hasRef = currentRef && currentRef.length;
	var filename = hasRef ? fileRef.parse(currentRef).ref : "";
	
	if (originalName) {
		filename = originalName;
	}

	var source;
	if (props.showActive) {
		source = (filter, includes) => uploadApi.active(filter, includes);
	}else{
		source = (filter, includes) => uploadApi.list(filter, includes);
	}

	// do we need to search ?
	var combinedFilter = { sort: { field: 'CreatedUtc', direction: 'desc' } };;

	if (filterTagId) {
		combinedFilter.query = "Tags contains ?"
		combinedFilter.args = [];
		combinedFilter.args.push(filterTagId);
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

	var tags = renderTags(combinedFilter);

	return <div className="file-selector">

		{/* upload browser */}
		<Modal
			isExtraLarge
			title={`Select an Upload`}
			className={"image-select-modal"}
			buttons={[
				{
					label: `Close`,
					onClick: closeUploadModal
				}
			]}
			onClose={closeUploadModal}
			visible={showUploadModal}
		>
			{renderHeader()}

			{tags && <>
				{tags}
			</>}

			<div className="file-selector__grid">
				<Loop source={source} filter={combinedFilter} paged={props.disablePaging ? undefined : true}>
					{
						entry => {

							// NB: API has been seen to report valid images with isImage=false
							//var isImage = entry.isImage;
							var isImage = fileRef.isImage(entry.ref);

							// default to 256px preview
							var renderedSize = 256;
							var imageWidth = parseInt(entry.width, 10);
							var imageHeight = parseInt(entry.height, 10);
							var previewClass = "file-selector__preview ";

							// render image < 256px if original image size was smaller
							if (!isNaN(imageWidth) && !isNaN(imageHeight) &&
								imageWidth < renderedSize && imageHeight < renderedSize) {
								renderedSize = undefined;
								previewClass += "file-selector__preview--auto";
							}

							return <>
								<div class="loop-item">
									<button title={entry.originalName} type="button" className="btn file-selector__item" onClick={(e) => updateValue(e, entry)}>
										<div className={previewClass}>
											{isImage && <Image fileRef={entry.ref} size={renderedSize} />}
											{!isImage && (
												<i className="fal fa-4x fa-file"></i>
											)}
										</div>
										<span className="file-selector__name">
											{entry.originalName}
										</span>
									</button>
								</div>
							</>;
						}

					}
				</Loop>
			</div>
		</Modal>

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
					updateValue(null, icon);
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
				file => updateValue(null, file)
			} />

		{/* options (browse, preview, remove) */}
		<div className="file-selector__options">
			{!props.browseOnly && <>

				{props.uploadOnly &&
					<button type="button" className="btn btn-primary file-selector__select" onClick={() => setShowUploadModal(true)}>
						{`Select upload`}
					</button>
				}

				{props.iconOnly &&
					<button type="button" className="btn btn-primary file-selector__select" onClick={() => setShowIconModal(true)}>
						{`Select icon`}
					</button>
				}

				{!props.uploadOnly && !props.iconOnly &&
					<Dropdown label={`Change file`} variant="primary" className="file-selector__select" items={
						[
							{
								onClick: () => setShowUploadModal(true),
								text: `Select from uploads`
							},
							hasRef ? {
								onClick: () => updateValue(null, null),
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
					<a href={fileRef.getUrl(currentRef)} alt={filename} className="btn btn-primary file-selector__link" target="_blank" rel="noopener noreferrer">
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