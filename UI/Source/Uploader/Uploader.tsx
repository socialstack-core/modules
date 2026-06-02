import * as fileRef from 'UI/FileRef';
import Video from 'UI/Video';
import Button from 'UI/Button';
import { useId, useState, useEffect, useRef } from 'react';
import { ApiContent } from 'UI/Functions/WebRequest';
import { Upload } from 'Api/Upload';

var DEFAULT_ABORTED = `Upload aborted`;
var DEFAULT_ERROR = `Unable to upload`;
var DEFAULT_MESSAGE = `Drag and drop your file or click to upload here`;
var DEFAULT_MESSAGE_MULTIPLE = `Drag and drop your file(s) or click to upload here`;

const XHR_UNSENT = 0; // Client has been created.open() not called yet.
const XHR_OPENED = 1; // open() has been called.
const XHR_HEADERS_RECEIVED = 2;	// send() has been called, and headers and status are available.
const XHR_LOADING = 3; // Downloading; responseText holds partial data.
const XHR_DONE = 4; // The operation is complete.

type FileInfo = {
	loading: boolean;
	failed: string | boolean;
	success: boolean;
	progressPercent: number;
	progress: string;
	filename: string;
	ref?: string;
	xhr?: XMLHttpRequest | null;
	originalName?: string;
};

export type UploaderProps = {
	multiple?: boolean;
	label?: string;
	iconOnly?: boolean;
	onInputRef?: (ref: React.RefObject<HTMLInputElement | null>) => void;
	maxSize?: number;
	currentRef?: string;
	aspect169?: boolean;
	aspect43?: boolean;
	onStarted?: (file: File, file2: File) => void;
	onUploaded?: (info: ApiContent<Upload>) => void;
	onUploadProgress?: () => void;
	endpoint?: string;
	url?: string;
	requestOpts?: { headers?: Record<string, string> };
	isPrivate?: boolean;
	compact?: boolean;
	id?: string;
	accept?: string;
	originalName?: string;
};

/*
* General purpose file uploader. Doesn't declare a form so can be used inline anywhere.
*/
export default function Uploader(props: UploaderProps) {
	var defaultMessage = props.multiple ? DEFAULT_MESSAGE_MULTIPLE : DEFAULT_MESSAGE;
	var initialMessage = props.label || defaultMessage;

	if (props.iconOnly) {
		initialMessage = '';
	}

	const inputRef = useRef<HTMLInputElement | null>(null);

	useEffect(() => {
		if (props.onInputRef) {
			props.onInputRef(inputRef);
		}
	}, [props.onInputRef]);

	const [loading, setLoading] = useState(false);
	const [progressPercent, setProgressPercent] = useState(0);
	const [progress, setProgress] = useState("");
	const [message, setMessage] = useState(initialMessage);
	const [tooltip, setTooltip] = useState(initialMessage);
	const [maxSize, setMaxSize] = useState(props.maxSize || 0);
	const [ref, setRef] = useState(props.currentRef);
	const [aspect169, setAspect169] = useState(props.aspect169);
	const [aspect43, setAspect43] = useState(props.aspect43);
	const [filename, setFilename] = useState(props.currentRef ? fileRef.parse(props.currentRef)?.file : undefined);
	const [files, setFiles] = useState<FileInfo[]>([]);
	const [draggedOver, setDraggedOver] = useState(false);
	const [failed, setFailed] = useState<string | boolean>(false);
	const [success, setSuccess] = useState(false);
	const [fileIndex, setFileIndex] = useState<number | undefined>(undefined);
	const [xhr, setXhr] = useState<XMLHttpRequest | null | undefined>(undefined);
	const [originalName, setOriginalName] = useState(props.originalName);

	// componentWillReceiveProps equivalent
	useEffect(() => {
		setRef(props.currentRef);
		setOriginalName(props.originalName);
	}, [props.currentRef, props.originalName]);

	useEffect(() => {
		const handleDragEnter = () => setDraggedOver(true);
		const handleDragLeave = () => setDraggedOver(false);

		const input = inputRef.current;
		if (input) {
			input.addEventListener('dragenter', handleDragEnter);
			input.addEventListener('dragleave', handleDragLeave);
			input.addEventListener('drop', handleDragLeave);
		}

		return () => {
			if (input) {
				input.removeEventListener('dragenter', handleDragEnter);
				input.removeEventListener('dragleave', handleDragLeave);
				input.removeEventListener('drop', handleDragLeave);
			}
		};
	}, []);

	const onSelectedFile = (e: React.ChangeEvent<HTMLInputElement>) => {
		// if we cancel out (no files)
		if (!e.target.files || e.target.files.length == 0) {
			setLoading(false);
			setFailed(false);
			setSuccess(false);
			setProgressPercent(0);
			setProgress("");
			setFilename(undefined);
			setRef(undefined);
			setFileIndex(undefined);
			setXhr(undefined);
			return;
		}

		var newFiles: FileInfo[] = [];
		const srcFiles = Array.from(e.target.files);

		srcFiles.forEach((file, i) => {
			var fileInfo: FileInfo = {
				loading: true,
				failed: false,
				success: false,
				progressPercent: 0,
				progress: "",
				filename: file.name,
				ref: undefined
			};
			newFiles.push(fileInfo);
		});

		setFiles(newFiles);

		srcFiles.forEach((file, i) => {
			setLoading(true);
			setFailed(false);
			setSuccess(false);
			setProgressPercent(0);
			setProgress("");
			setFilename(file.name);
			setRef(undefined);
			setFileIndex(i);

			if (maxSize > 0 && file.size > maxSize) {
				setFiles(prevFiles => {
					var fs = [...prevFiles];
					if (fs[i]) {
						fs[i].loading = false;
						fs[i].success = false;
						fs[i].failed = `File too large`;
						fs[i].xhr = null;
					}
					return fs;
				});

				setLoading(false);
				setSuccess(false);
				setFailed(`File too large`);
				setXhr(null);

				return;
			}

			props.onStarted && props.onStarted(file, file);

			var xhrObj = new XMLHttpRequest();

			setFiles(prevFiles => {
				var fs = [...prevFiles];
				if (fs[i]) {
					fs[i].xhr = xhrObj;
				}
				return fs;
			});
			setXhr(xhrObj);

			xhrObj.onreadystatechange = () => {
				// console.log("XHR ONREADYSTATECHANGE: ", `${xhrObj.responseText} (${xhrObj.status})`);

				if (xhrObj.readyState == XHR_DONE) {
					var uploadInfo : any;

					try {
						uploadInfo = JSON.parse(xhrObj.responseText);
					} catch (e) {

					}

					if (!uploadInfo || xhrObj.status > 300) {
						let msg: string | boolean = DEFAULT_ERROR;
						
						setFiles(prevFiles => {
							var fs = [...prevFiles];
							if (fs[i]) {
								msg = fs[i].progressPercent > 0 ? DEFAULT_ABORTED : DEFAULT_ERROR;
								if (uploadInfo && uploadInfo.message) {
									msg = uploadInfo.message;
								}
								fs[i].loading = false;
								fs[i].success = false;
								fs[i].failed = msg;
								fs[i].xhr = null;
							}
							return fs;
						});

						setLoading(false);
						setSuccess(false);
						setFailed(msg);
						setXhr(null);

						return;
					}

					// Run the main callback:
					props.onUploaded && props.onUploaded(uploadInfo as ApiContent<Upload>);

					setFiles(prevFiles => {
						var fs = [...prevFiles];
						if (fs[i]) {
							fs[i].loading = false;
							fs[i].success = true;
							fs[i].failed = false;
							fs[i].ref = uploadInfo.result.ref;
							fs[i].xhr = null;
						}
						return fs;
					});

					setLoading(false);
					setSuccess(true);
					setFailed(false);
					setRef(uploadInfo.result.ref);
					setXhr(null);

				} else if (xhrObj.readyState == XHR_HEADERS_RECEIVED) {
					// Headers received
					if (xhrObj.status > 300) {
						setFiles(prevFiles => {
							var fs = [...prevFiles];
							if (fs[i]) {
								fs[i].loading = false;
								fs[i].success = false;
								fs[i].failed = DEFAULT_ERROR;
								fs[i].xhr = null;
							}
							return fs;
						});

						setLoading(false);
						setSuccess(false);
						setFailed(DEFAULT_ERROR);
						setXhr(null);
					}
				}
			};

			xhrObj.onerror = (e) => {
				console.log("XHR onerror", e);
				setFiles(prevFiles => {
					var fs = [...prevFiles];
					if (fs[i]) {
						fs[i].loading = false;
						fs[i].success = false;
						fs[i].failed = DEFAULT_ERROR;
						fs[i].xhr = null;
					}
					return fs;
				});

				setLoading(false);
				setSuccess(false);
				setFailed(DEFAULT_ERROR);
				setXhr(null);
			};

			xhrObj.upload.onprogress = (evt) => {
				var pc = Math.floor(evt.loaded * 100 / evt.total);
				
				setFiles(prevFiles => {
					var fs = [...prevFiles];
					if (fs[i]) {
						fs[i].progressPercent = pc;
						fs[i].progress = ' ' + pc + '%';
					}
					return fs;
				});

				setProgressPercent(pc);
				setProgress(' ' + pc + '%');

				// console.log("XHR UPLOAD PROGRESS: ", ' ' + pc + '%');

				props.onUploadProgress && props.onUploadProgress();
			};

			xhrObj.onabort = (evt) => {
				// console.log("XHR ABORT: ", evt);
			}

			xhrObj.onloadend = (evt) => {
				// upload complete
				// console.log("XHR LOADEND EVENT: ", evt);
			}

			var ep = props.endpoint || "upload/create";

			var apiUrl = props.url || (window as any).ingestUrl || (window as any).apiHost || '';
			if (!apiUrl.endsWith('/')) {
				apiUrl += '/';
			}
			apiUrl += 'v1/';

			ep = (ep.indexOf('http') === 0 || ep[0] == '/') ? ep : apiUrl + ep;

			xhrObj.open('PUT', ep, true);

			var { requestOpts } = props;

			if (requestOpts && requestOpts.headers) {
				for (var header in requestOpts.headers) {
					xhrObj.setRequestHeader(header, requestOpts.headers[header]);
				}
			}

			xhrObj.setRequestHeader("Content-Name", encodeURIComponent(file.name));
			xhrObj.setRequestHeader("Private-Upload", props.isPrivate ? '1' : '0');
			xhrObj.send(file);
		});
	};

	const abortFile = (e: React.MouseEvent, xhrToAbort?: XMLHttpRequest | null) => {
		// console.log("attempting to abort: ", e);

		if (xhrToAbort) {
			xhrToAbort.abort();
		}
	};

	const formatBytes = (bytes: number, decimals = 2) => {
		if (bytes === 0) {
			return "";
		}

		const k = 1024;
		const dm = decimals < 0 ? 0 : decimals;
		const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];

		const i = Math.floor(Math.log(bytes) / Math.log(k));

		return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
	};

	const renderBulkUploadUI = (id: string) => {
		var uploaderClasses = ['uploader', 'uploader--multiple'];

		if (props.compact) {
			uploaderClasses.push("uploader--compact");
		}

		if (aspect169) {
			uploaderClasses.push("uploader--16-9");
		}

		if (aspect43) {
			uploaderClasses.push("uploader--4-3");
		}

		if (draggedOver) {
			uploaderClasses.push("uploader--drag-target");
		}

		return <div className={uploaderClasses.join(' ')}>
			{/* prompt to upload */}
			{!files || !files.length ? <>
				<div className="uploader__internal">
					<input id={id} className="uploader__input" type="file" disabled={props.iconOnly} ref={inputRef}
						onChange={e => onSelectedFile(e)} title={tooltip} multiple accept={props.accept} />
					<label htmlFor={id} className="uploader__label">
						<span className="uploader__label-internal">
							{message}
						</span>
					</label>
				</div>
			</> : null}

			{/* display selected files */}
			{files && files.length > 0 && <>
				<div className="uploader__bulk-list">
					{files.map((file, i) => {
						var fileClasses = ['upload'];
						var labelClasses = ['uploader__label'];
						var fileLabel: string | boolean = "";

						if (file.loading) {
							fileClasses.push("uploader--progress");
							fileLabel = file.progressPercent == 100 ? `Processing ...` : `Uploading ${file.progress} ...`;
						}

						if (file.failed) {
							fileClasses.push("uploader--error");
							fileLabel = file.failed;
						}

						var hasRef = file.ref && file.ref.length;
						var hasFilename = file.filename && file.filename.length;
						var hasOriginalName = file.originalName && file.originalName.length;
						var labelStyle: React.CSSProperties = {};
						var canShowImage = false;
						var canShowVideo = false;

						if (hasRef) {
							var refInfo = file.ref ? fileRef.parse(file.ref) : null;
							canShowImage = !!refInfo && refInfo.isImage(false);
							canShowVideo = !!refInfo && refInfo.isVideo(false);

							fileClasses.push("uploader--content");
							fileLabel = "";

							// TODO: check original image width/height values here; if both are less than 256px,
							// use the original image and set background-size to auto
							if (!!refInfo && canShowImage && !canShowVideo) {
								labelStyle = { backgroundImage: "url(" + fileRef.getUrl(refInfo, { size: '256' }) + ")" };
							}

							if ((canShowImage || canShowVideo)) {
								fileClasses.push("uploader--image");
							}

							if (canShowVideo) {
								labelClasses.push("video");
							}
						}

						var renderedSize = '256';
						var caption: string | boolean = hasFilename ? file.filename : false;

						if (hasOriginalName) {
							caption = file.originalName!;
						}

						return <div key={i} className={fileClasses.join(' ')}>
							<div className="uploader__internal">

								{(canShowImage || canShowVideo) &&
									<div className="uploader__imagebackground">
									</div>
								}

								<label className={labelClasses.join(' ')} style={labelStyle}>

									{/* loading */}
									{file.loading && <>
										<div className="spinner-border" role="status"></div>
									</>}

									{/* has a reference, but isn't an image */}
									{hasRef && !canShowImage && !canShowVideo && <>
										<i className="fal fa-file uploader__file" />
									</>}

									{/* has an video reference */}
									{hasRef && canShowVideo && <Video fileRef={file.ref} size={renderedSize} />}

									{/* failed to upload */}
									{file.failed && <>
										<i className="fas fa-times-circle"></i>
									</>}

									<span className="uploader__label-internal">
										{fileLabel}
									</span>
								</label>
								{file.loading && file.progressPercent < 100 && file.xhr && <>
									<Button outlined variant="danger" className="uploader__abort" onClick={(e) => abortFile(e, file.xhr)}>
										{`Cancel upload`}
									</Button>
									<progress className="uploader__progress" max={100} value={file.progressPercent}></progress>
								</>}
							</div>
							{caption && <>
								<small className="uploader__caption text-muted">
									{caption}
								</small>
							</>}
						</div>;
					})}
				</div>
			</>}

		</div>;
	};

	var isMultiple = props.multiple;

	const componentId = useId();
	const id = props.id || componentId;

	if (isMultiple) {

		return renderBulkUploadUI(id);
	}

	var hasRef = ref && ref.length ? true : false;
	var hasMaxSize = maxSize > 0;
	var hasFilename = (filename && filename.length);
	var hasOriginalName = (originalName && originalName.length);
	var label: string | boolean = loading ? (`Uploading` + " " + progress + " ...") : message;

	if (loading && progressPercent == 100) {
		label = `Processing ...`;
	}
	
	var parsedRef = hasRef ? fileRef.parse(ref!) : undefined;
	var canShowImage = hasRef ? parsedRef!.isImage(false) : false;
	var canShowVideo = hasRef ? parsedRef!.isVideo(false) : false;
	var labelStyle: React.CSSProperties = {};
	var uploaderClasses = ['uploader'];
	var uploaderLabelClasses = ['uploader__label'];

	if (props.compact) {
		uploaderClasses.push("uploader--compact");
	}

	if (loading) {
		uploaderClasses.push("uploader--progress");
	}

	if (failed) {
		uploaderClasses.push("uploader--error");
		label = failed;
	}

	if (aspect169) {
		uploaderClasses.push("uploader--16-9");
	}

	if (aspect43) {
		uploaderClasses.push("uploader--4-3");
	}

	if (draggedOver) {
		uploaderClasses.push("uploader--drag-target");
	}

	var focalX = 50;
	var focalY= 50;
	var altText = '';
	var author = '';

	if (hasRef) {
		var refInfo = fileRef.parse(ref!);

		uploaderClasses.push("uploader--content");
		label = "";

		// get the focal point (if any)
		if (refInfo && refInfo.focalX && refInfo.focalY) {
			focalX = refInfo.focalX;				
			focalY = refInfo.focalY;
		}

		// get the author (if any)
		if (refInfo && refInfo.author && refInfo.author.length > 0) {
			author = refInfo.author;				
		}

		// get the alt text (if any)
		if (refInfo && refInfo.altText && refInfo.altText.length > 0) {
			altText = refInfo.altText;				
		}

		// TODO: check original image width/height values here; if both are less than 256px,
		// use the original image and set background-size to auto
		if (canShowImage && !canShowVideo) {
			labelStyle = { backgroundImage: "url(" + fileRef.getUrl(refInfo!, { size: '256' }) + ")" };
		}

		if ((canShowImage || canShowVideo)) {
			uploaderClasses.push("uploader--image");
		}

		if (canShowVideo) {
			uploaderLabelClasses.push("video");
		}

	}

	var uploaderClass = uploaderClasses.join(' ');
	var uploaderLabelClass = uploaderLabelClasses.join(' ');

	var renderedSize = '256';

	var caption: string | boolean = hasFilename ? filename! : `None selected`;

	if (hasOriginalName) {
		caption = originalName!;
	}

	var currentXhr = fileIndex == undefined ? xhr : (files[fileIndex] ? files[fileIndex].xhr : undefined);

	const iconClassName = props.iconOnly ? (
		props.currentRef && typeof props.currentRef === 'string' &&
		(
			props.currentRef.startsWith("fas:") ||
			props.currentRef.startsWith("far:") ||
			props.currentRef.startsWith("fab:") ||
			props.currentRef.startsWith("fal:")
		) ?
			props.currentRef.substring(0,3) + " " + props.currentRef.substring(4) : 
			"fal fa-file uploader__file"
	) : "fal fa-file uploader__file";
			
	return <div className={uploaderClass}>
		<div className={props.iconOnly ? "uploader__internal uploader__internal--icon" : "uploader__internal"}>

			{(canShowImage || canShowVideo) &&
				<div className="uploader__imagebackground">
									<div className="uploader__imagebackground-crosshair" style={{
												left: focalX + '%',
												top: focalY + '%'
											}}></div>
				</div>
			}

			<input id={id} className="uploader__input" type="file" disabled={props.iconOnly} ref={inputRef}
				onChange={e => onSelectedFile(e)} title={loading ? `Loading ...` : tooltip} accept={props.accept} />
			<label htmlFor={id} className={uploaderLabelClass} style={labelStyle}>

				{/* loading */}
				{loading && <>
					<div className="spinner-border" role="status"></div>
				</>}

				{/* has a reference, but isn't an image */}
				{hasRef && !canShowImage && !canShowVideo && <>
					<i className={iconClassName} />
				</>}

				{/* has an video  reference */}
				{hasRef && canShowVideo && <Video fileRef={ref} size={renderedSize} />}

				{/* failed to upload */}
				{failed && <>
					<i className="fas fa-times-circle"></i>
				</>}

				{!hasRef && props.iconOnly && <i className={"fal fa-file uploader__file"}/>}

				<span className="uploader__label-internal">
					{label}
				</span>
			</label>
			{loading && progressPercent < 100 && currentXhr && <>
				<Button outlined variant="danger" className="uploader__abort" onClick={(e) => abortFile(e, currentXhr)}>
					{`Cancel upload`}
				</Button>
				<progress className="uploader__progress" max="100" value={progressPercent}></progress>
			</>}
		</div>
		{!isMultiple && <>
			<small className="uploader__caption uploader__caption__single text-muted ">
				{caption}
				<br />

				{altText && altText.length > 0 && 
					<>
						{altText}
						<br/>
					</>
				}

				{author && author.length > 0 && 
					<>
						{author}
						<br/>
					</>
				}


				{hasMaxSize && <>
					{`Max file size: ${formatBytes(maxSize)}`}
				</>}
			</small>
		</>}
	</div>;
}