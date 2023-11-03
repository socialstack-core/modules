import Loop from 'UI/Loop';
import Container from 'UI/Container';
import Row from 'UI/Row';
import Modal from 'UI/Modal';
import Uploader from 'UI/Uploader';
import omit from 'UI/Functions/Omit';
import getRef from 'UI/Functions/GetRef';
import IconSelector from 'UI/FileSelector/IconSelector';
import Dropdown from 'UI/Dropdown';
import Col from 'UI/Column';
import Input from 'UI/Input';
import Search from 'UI/Search';

var inputTypes = global.inputTypes = global.inputTypes || {};
let lastId = 0;

const CLOSEST_MULTIPLE = 2;
const PREVIEW_SIZE = 512;

var searchFields = ['originalName', 'alt', 'author', 'id'];

inputTypes.ontypefile = inputTypes.ontypeimage = function (props, _this) {
    return (
        <FileSelector
            id={props.id || _this.fieldId}
            className={props.className || "form-control"}
            {...omit(props, ['id', 'className', 'type', 'inline'])}
        />
    );
};

inputTypes.ontypeicon = function (props, _this) {
    return (
        <FileSelector
            id={props.id || _this.fieldId}
            iconOnly
            className={props.className || "form-control"}
            {...omit(props, ['id', 'className', 'type', 'inline'])}
        />
    );
};

inputTypes.ontypeupload = function (props, _this) {
    return (
        <FileSelector
            id={props.id || _this.fieldId}
            browseOnly
            className={props.className || "form-control"}
            {...omit(props, ['id', 'className', 'type', 'inline'])}
        />
    );
};

inputTypes.ontypenopaging = function (props, _this) {
    return (
        <FileSelector
            id={props.id || _this.fieldId}
            disablePaging
            className={props.className || "form-control"}
            {...omit(props, ['id', 'className', 'type', 'inline'])}
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

        this.closeUploadModal = this.closeUploadModal.bind(this);
        this.closeEditModal = this.closeEditModal.bind(this);
        this.renderTag = this.renderTag.bind(this);
    }

    newId() {
        lastId++;
        return `fileselector${lastId}`;
    }

    showRef(ref, size) {
        var parsedRef = getRef.parse(ref);
        var size = size || 256;
        var targetSize = size;
        //var minSize = size == 256 ? 238 : size;

        // Check if it's an image/ video/ audio file. If yes, a preview is shown. Otherwise it'll be a placeholder icon.
        var canShowImage = getRef.isImage(ref);

        if (canShowImage && parsedRef.args) {
            var args = parsedRef.args;

            if ((args.w && args.w < size) && (args.h && args.h < size)) {
                targetSize = undefined;
            }

        }

        return canShowImage ?
            getRef(ref, { size: targetSize, portraitCheck: true}) :
            <span className="fal fa-4x fa-file"></span>;
    }

    updateValue(e, newRef) {
        if (e) {
            e.preventDefault();
        }

        var originalName = newRef ? newRef.originalName : '';

        //this.props.currentRef ? getRef.parse(this.props.currentRef).ref

        if (!newRef) {
            newRef = '';
        }

        if (newRef.result && newRef.result.ref) {
            // Accept upload objects also.
            newRef = newRef.result.ref;
        } else if (newRef.ref) {
            newRef = newRef.ref;
        }

        this.props.onChange && this.props.onChange({ target: { value: newRef } });

        this.setState({
            value: newRef,
            originalName: originalName,
            uploadModalOpen: false,
            editModalOpen:false
        });
    }

    saveUpdates(e) {

        var currentRef = this.props.value || this.props.defaultValue;

        if (this.state.value !== undefined) {
            currentRef = this.state.value;
        }

        var args = {
            fx : this.state.focalX,
            fy : this.state.focalY,
            au: this.state.author,
            al:this.state.alt
       };

        var newRef = getRef.update(currentRef, args);

        console.log('new', newRef);

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

        var refInfo = getRef.parse(currentRef);

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

        var isImage = getRef.isImage(currentRef);
        var isVideo = getRef.isVideo(currentRef);
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
                                                    <i className="fal fa-fw fa-sync"></i>
                                                </button>
                                                {this.state.focalX}%, {this.state.focalY}%
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
                <Loop raw over={'upload/list'} filter={combinedFilter} includes={'tags'} onResults={results => {
                    results.map(media => {
                        media.tags.map(tag => {
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

        var { searchFilter } = this.state;

        var currentRef = this.props.value || this.props.defaultValue;

        if (this.state.value !== undefined) {
            currentRef = this.state.value;
        }

        var hasRef = currentRef && currentRef.length;
        var filename = hasRef ? getRef.parse(currentRef).ref : "";
        var originalName = this.state.originalName && this.state.originalName.length ? this.state.originalName : '';

        if (originalName) {
            filename = originalName;
        }

        var source = "upload/list";
        if (this.props.showActive) {
            source = "upload/active";
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
                    <Loop raw over={source} filter={combinedFilter} paged={this.props.disablePaging ? undefined : true}>
                        {
                            entry => {

                                // NB: API has been seen to report valid images with isImage=false
                                //var isImage = entry.isImage;
                                var isImage = getRef.isImage(entry.ref);

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
                                                {isImage && getRef(entry.ref, { size: renderedSize })}
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
                        console.log("onSelected");
                        console.log(icon);
                        this.updateValue(null, icon);
                    }
                }
            />

            {/* upload */}
            <Uploader
                compact={this.props.compact}
                currentRef={currentRef}
                originalName={originalName}
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
                        <Dropdown label={`Select`} variant="primary" className="file-selector__select">
                            <li>
                                <button type="button" className="btn dropdown-item" onClick={() => this.showUploadModal()}>
                                    {`From uploads`}
                                </button>
                            </li>
                            <li>
                                <button type="button" className="btn dropdown-item" onClick={() => this.setState({ iconModalOpen: true })}>
                                    {`From icons`}
                                </button>
                            </li>
                        </Dropdown>
                    }

                </>}
                {hasRef && <>
                    {!getRef.isIcon(currentRef) && <>
                        <a href={getRef(currentRef, { url: true })} alt={filename} className="btn btn-primary file-selector__link" target="_blank" rel="noopener noreferrer">
                            {`Preview`}
                        </a>
                    </>}

                    {!getRef.isIcon(currentRef) && <>
                        <button type="button" className="btn btn-primary file-selector__select" onClick={() => this.showEditModal()}>
                            {`Edit`}
                        </button>
                    </>}

                    <button type="button" className="btn btn-danger file-selector__remove" onClick={() => this.updateValue(null, null)}>
                        {`Remove`}
                    </button>
                </>}
            </div>

            {this.props.name && (
                // Also contains a hidden input field containing the value
                <input type="hidden" value={currentRef} name={this.props.name} id={this.props.id} />
            )}
        </div>;
    }

}