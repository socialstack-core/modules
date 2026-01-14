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
import {useEffect, useState} from "react";

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
    const ref = React.useRef(null);

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
export default class FileSelector extends React.Component {

    constructor(props) {
        super(props);

        var ref = props.value || props.defaultValue;

        this.state = {
            ref: ref
        };
        
        this.inputRef = React.createRef();

        this.closeUploadModal = this.closeUploadModal.bind(this);
        this.closeEditModal = this.closeEditModal.bind(this);
        this.renderTag = this.renderTag.bind(this);
    }

    newId() {
        lastId++;
        return `fileselector${lastId}`;
    }

    showRef(ref, size) {
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

    updateValue(e, newRef) {
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

        this.setState({
            value: newRef,
            originalName: originalName,
            uploadModalOpen: false,
            editModalOpen:false
        }, () => {
            this.props.onChange && this.props.onChange({ target: { value: newRef } });
        });
    }

    saveUpdates(e) {

        var currentRef = this.props.value || this.props.defaultValue;

        if (this.state.value !== undefined) {
            currentRef = this.state.value;
        }
		
		var pr = fileRef.parse(currentRef);
		pr.setNumericArg('fx', this.state.focalX);
		pr.setNumericArg('fy', this.state.focalY);
		pr.setArg('au', this.state.author);
		pr.setArg('al', this.state.alt);
		
        var newRef = pr.toString();
		
        this.setState({ value: newRef , editModalOpen : false});
    }

    showUploadModal() {
        this.setState({ uploadModalOpen: true });
    }

    closeUploadModal() {
        this.setState({ uploadModalOpen: false });
    }

    showEditModal() {
        var currentRef = this.props.value || this.props.defaultValue;

        if (this.state.value !== undefined) {
            currentRef = this.state.value;
        }

        var refInfo = fileRef.parse(currentRef);

        this.setState({
            editModalOpen: true,
            author: refInfo.author,
            alt: refInfo.altText,
            focalX: refInfo.focalX,
            focalY: refInfo.focalY
        });
    }

    closeEditModal() {
        this.setState({ editModalOpen: false });
    }

    renderEditModal() {
        var currentRef = this.props.value || this.props.defaultValue;

        if (this.state.value !== undefined) {
            currentRef = this.state.value;
        }
		
		var parsedRef = fileRef.parse(currentRef);
        var isImage = parsedRef.isImage();
        var isVideo = parsedRef.isVideo();
        var title = `Edit`;

        return <>
            <Modal isExtraLarge title={title}
                buttons={[
                    {
                        label: `Close`,
                        onClick: this.closeEditModal
                    }
                ]}
                onClose={this.closeEditModal}
                visible={this.state.editModalOpen}
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
                                                this.setState({
                                                    focalX: CLOSEST_MULTIPLE * Math.round((e.offsetX / imagePreviewRect.width * 100) / CLOSEST_MULTIPLE),
                                                    focalY: CLOSEST_MULTIPLE * Math.round((e.offsetY / imagePreviewRect.height * 100) / CLOSEST_MULTIPLE)
                                                });
                                            }}>
                                            {this.showRef(currentRef, PREVIEW_SIZE)}
                                            {isImage && !isVideo && <>
                                                <div className="media-center__preview-crosshair" style={{
                                                    left: this.state.focalX + '%',
                                                    top: this.state.focalY + '%'
                                                }}></div>
                                            </>}
                                        </div>
                                    </div>
                                </Col>

                                <Col sizeMd='3'>
                                    <div className="media-center__metadata">

                                        <div className="form-text media-center__alt">
                                            <Input type="text" label={`Author/Photographer`} value={this.state.author} onChange={e => {
                                                this.setState({
                                                    author: e.target.value
                                                });
                                            }} />
                                        </div>

                                        <div className="form-text media-center__alt">
                                            <Input type="text" label={`Alternative Text`} value={this.state.alt} onChange={e => {
                                                this.setState({
                                                    alt: e.target.value
                                                });
                                            }} />
                                    </div>

                                        {isImage && !isVideo &&
                                            <div className="form-text media-center__focal-point">
                                                <button type="button" className="btn btn-sm btn-outline-secondary me-2" onClick={() => {
                                                    this.setState({
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
                        <button type="button" className="btn btn-outline-primary" onClick={() => this.closeEditModal()}>
                            {`Cancel`}
                        </button>
                        <button type="button" className="btn btn-primary" onClick={() => this.saveUpdates()}>
                            {`Save`}
                        </button>
                    </div>
                </footer>
            </Modal>
        </>;
    }

    renderTags(combinedFilter) {
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
                    {this.renderTag}
                </Loop>
            </ul>
        )
    }

    renderTag(tag) {
        if (!tag || !tag.name || tag.name.length == 0) {
            return ('');
        }

        var tagClassName = (this.state.filterTagId && this.state.filterTagId == tag.id) ? "file-selector__tag file-selector__tag-selected" : "file-selector__tag"

        return (
            <li className={tagClassName} onClick={() => {
                if (this.state.filterTagId && this.state.filterTagId == tag.id) {
                    this.setState({ filterTagId: null });
                } else {
                    this.setState({ filterTagId: tag.id });
                }
            }}>
                {tag.name}
            </li>
        );
    }



    renderHeader() {
        return <div className="row header-container file-selector__search">
            {searchFields && <>
                <Search className="admin-page__search" placeholder={`Search`}
                    onQuery={(where, query) => {
                        this.setState({
                            searchFilter: query
                        });
                    }} />
            </>}
        </div>;
    }

    render() {
        
        this.props.onInputRef && this.props.onInputRef(this.inputRef.current);
        
        var { searchFilter } = this.state;

        var currentRef = this.props.value || this.props.defaultValue;

        if (this.state.value !== undefined) {
            currentRef = this.state.value;
        }

        var hasRef = currentRef && currentRef.length;
        var filename = hasRef ? fileRef.parse(currentRef).ref : "";
        var originalName = this.state.originalName && this.state.originalName.length ? this.state.originalName : '';

        if (originalName) {
            filename = originalName;
        }

        var source;
        if (this.props.showActive) {
            source = (filter, includes) => uploadApi.active(filter, includes);
        }else{
			source = (filter, includes) => uploadApi.list(filter, includes);
		}

        // do we need to search ?
        var combinedFilter = { sort: { field: 'CreatedUtc', direction: 'desc' } };;

        if (this.state.filterTagId) {
            combinedFilter.query = "Tags contains ?"
            combinedFilter.args = [];
            combinedFilter.args.push(this.state.filterTagId);
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

        var tags = this.renderTags(combinedFilter);

        return <div className="file-selector">

            {/* upload browser */}
            <Modal
                isExtraLarge
                title={`Select an Upload`}
                className={"image-select-modal"}
                buttons={[
                    {
                        label: `Close`,
                        onClick: this.closeUploadModal
                    }
                ]}
                onClose={this.closeUploadModal}
                visible={this.state.uploadModalOpen}
            >
                {this.renderHeader()}

                {tags && <>
                    {tags}
                </>}

                <div className="file-selector__grid">
                    <Loop source={source} filter={combinedFilter} paged={this.props.disablePaging ? undefined : true}>
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
                                        <button title={entry.originalName} type="button" className="btn file-selector__item" onClick={(e) => this.updateValue(e, entry)}>
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
            {this.state.editModalOpen && this.renderEditModal()}

            {/* icon browser */}
            <IconSelector
                visible={this.state.iconModalOpen}
                onClose={() => {
                    this.setState({ iconModalOpen: false })
                }}
                onSelected={
                    icon => {
                        this.updateValue(null, icon);
                    }
                }
            />

            {/* upload */}
            <Uploader
                compact={this.props.compact}
                currentRef={currentRef}
                originalName={this.props.iconOnly ? currentRef : originalName}
                id={this.props.id || this.newId()}
                isPrivate={this.props.isPrivate}
                url={this.props.url}
                requestOpts={this.props.requestOpts}
                maxSize={this.props.maxSize}
                iconOnly={this.props.iconOnly}
                onUploaded={
                    file => this.updateValue(null, file)
                } />

            {/* options (browse, preview, remove) */}
            <div className="file-selector__options">
                {!this.props.browseOnly && <>

                    {this.props.uploadOnly &&
                        <button type="button" className="btn btn-primary file-selector__select" onClick={() => this.showUploadModal()}>
                            {`Select upload`}
                        </button>
                    }

                    {this.props.iconOnly &&
                        <button type="button" className="btn btn-primary file-selector__select" onClick={() => this.setState({ iconModalOpen: true })}>
                            {`Select icon`}
                        </button>
                    }

                    {!this.props.uploadOnly && !this.props.iconOnly &&
                        <Dropdown label={`Change file`} variant="primary" className="file-selector__select" items={
							[
								{
									onClick: () => this.showUploadModal(),
									text: `Select from uploads`
								},
								hasRef ? {
									onClick: () => this.updateValue(null, null),
									text: `Remove`
								} : null,
								(hasRef && !this.props.iconOnly) ? {
									onClick: () => this.showEditModal(),
									text: `Edit file`
								} : null
								/*
								Icons are not dedicated refs anymore. The modal has been fixed such 
								that it at least displays icons, but selecting one will error due to its ref output.
								{
									onClick: () => this.setState({ iconModalOpen: true }),
									text: `From icons`
								}*/
							]
						} />
                    }

                </>}
                {hasRef && <>
                    {!this.props.iconOnly && <>
                        <a href={fileRef.getUrl(currentRef)} alt={filename} className="btn btn-primary file-selector__link" target="_blank" rel="noopener noreferrer">
                            {`View file`}
                        </a>
                    </>}
                </>}
            </div>

            {this.props.name && (
                // Also contains a hidden input field containing the value
                <input ref={this.inputRef} type="hidden" value={currentRef} name={this.props.name} id={this.props.id} />
            )}
        </div>;
    }

}