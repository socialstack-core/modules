import Loop from 'UI/Loop';
import Container from 'UI/Container';
import Row from 'UI/Row';
import Column from 'UI/Column';
import Input from 'UI/Input';
import Image from 'UI/Image';
import Loading from 'UI/Loading';
import Search from 'UI/Search';
import SubHeader from 'Admin/SubHeader';
import Uploader from 'UI/Uploader';
import ConfirmModal from 'UI/Modal/ConfirmModal';
import Modal from 'UI/Modal';
import * as fileRef from 'UI/FileRef';
import MultiSelect from 'Admin/MultiSelect';
import uploadApi, { Upload } from 'Api/Upload';
import {useState, useMemo} from 'react';
import { Tag } from 'Api/Tag';
import {useRouter} from "UI/Router";
import Debounce from "UI/Functions/Debounce";
import {ListFilter} from "Api/Content";

var fields = ['id', 'originalName'];
var searchFields = ['originalName', 'alt', 'author', 'id'];

const CLOSEST_MULTIPLE = 2;
const PREVIEW_SIZE = 512;
const UPLOAD_SINGLE = 1;
const UPLOAD_MULTIPLE = 2;

type UploadModal = {
    uploadMode: number,
    bulkUploaded: boolean,
    focalX: number,
    focalY: number,
    uploadId?: uint,
    alt?: string,
    coverImageRef?: string,
    author?: string,
    tags?: Tag[],
    transcodeState?: int,
    existingFileRef?: string,
    originalName?: string
};

const getIntegerSearchParam = (name: string, currentParams: URLSearchParams, fallback: number) => {

    if (!currentParams.has(name)) {
        return fallback;
    }

    let value: number = parseInt(currentParams.get(name)!);

    if (Number.isNaN(value))
    {
        return fallback;
    }
    return value;
}

const MediaCenter = (props) => {
    
    // added in updateQuery and pageState to allow state hydration 
    // from the query params, allows the nice use of back buttons
    // so that the search filter and page size, index are all 
    // tracked, 
    const { updateQuery, pageState } = useRouter();
    
    // when searching, and going back the user doesn't want to go back
    // per character basis, so debounce essentially stops this
    // happening and allows only intentional searches fromn being 
    // held in history.
    const debounce = useMemo(() => new Debounce((query: string) => {
        
        const newQParam = {
            q: query,
            page: "1"
        }
        
        updateQuery(newQParam)
    }), [updateQuery])
    
    
    const [sorter, setSort] = useState({ field: 'id', direction: 'desc' });
    
    // page size is held in the URL too, under the param "limit" as seen below, it falls back to 20, the reason 
    // no mutating method has been declared here is it doesn't need to exist, when the param for this is changed,
    // the URL changes, which then in effect triggers the re-render needed to update this value. 
    const [pageSize] = useState<uint>(getIntegerSearchParam('limit', pageState.query, 20) as uint);
    
    const [bulkSelections, setBulkSelections] = useState<Record<int, boolean> | null>(null);
    const [confirmDelete, setConfirmDelete] = useState<boolean>(false);
    const [uploadModal, setUploadModal] = useState<UploadModal | null>(null);
    const [deleting, setDeleting] = useState<boolean>(false);
    const [deleteCount, setDeleteCount] = useState<number>(0);
    const [deleteFailed, setDeleteFailed] = useState<boolean>(false);
    const searchFilter = pageState.query.get("q");
    const [filterTagId, setFilterTagId] = useState<uint | null>(null);

    let sort = sorter;

    if (sorter && !fields.find(field => field == sort.field)) {
        // Restore to id sort:
        sort = { field: 'id', direction: 'desc' };
    }

    const showRef = (ref: string, size: number = 256) => {
        var parsedRef = fileRef.parse(ref);

        if (!parsedRef) {
            return null;
        }

        var targetSize : number | undefined = size;
        //var minSize = size == 256 ? 238 : size;

        var fileClassName = parsedRef.fileType != '' ? 'fal fa-4x fa-file fa-file-' + parsedRef.fileType : 'fal fa-4x fa-file';

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
            <span className={fileClassName}></span>;

    }

    const renderTags = (uploads : Upload[] | null) => {
        if (!uploads) {
            return null;
        }

        var tags : Tag[] = [];

        uploads.map(media => {
            media.tags?.map(tag => {
                if (!tags.includes(tag)) {
                    tags.push(tag);
                }
            });
        });

        return (
            <ul className='media-center__tags'>
                {tags.map(renderTag)}
            </ul>
        )
    }

    const renderTag = (tag: Tag) => {
        if (!tag || !tag.name || tag.name.length == 0) {
            return ('');
        }

        var tagClassName = (filterTagId == tag.id) ? "media-center__tag media-center__tag-selected" : "media-center__tag"
        return (
            <li className={tagClassName} onClick={() => {
                if (filterTagId == tag.id) {
                    setFilterTagId(null);
                } else {
                    setFilterTagId(tag.id);
                }
            }}>
                {tag.name}
            </li>
        );
    }

    const renderHeader = (allContent) => {
        // Header (Optional)
        var heads = fields.map(field => {

            // Class for styling the sort buttons:
            var sortingByThis = sort && sort.field == field;
            var className = '';

            if (sortingByThis) {
                className = sort.direction == 'desc' ? 'sorted-desc' : 'sorted-asc';
            }

            // Field name with its first letter uppercased:
            var ucFirstFieldName = field.length ? field.charAt(0).toUpperCase() + field.slice(1) : '';

            return (
                <th className={className}>
                    {ucFirstFieldName} <i className="fa fa-caret-down" onClick={() => {
                        // Sort desc
                        setSort({
                            field,
                            direction: 'desc'
                        });
                    }} /> <i className="fa fa-caret-up" onClick={() => {
                        // Sort asc
                        setSort({
                            field,
                            direction: 'asc'
                        });
                    }} />
                </th>
            );
        });

        // If everything in allContent is selected, mark this as selected as well.
        var checked = false;

        if (bulkSelections && allContent.length) {
            checked = true;
            allContent.forEach(e => {
                if (!bulkSelections[e.id]) {
                    checked = false;
                }
            });
        }

        return [
            <th>
                <input type='checkbox' checked={checked} onClick={() => {

                    // Check or uncheck all things.
                    if (checked) {
                        setBulkSelections(null);
                    } else {
                        const newSels = {};
                        allContent.forEach(e => newSels[e.id] = true);
                        setBulkSelections(newSels);
                    }

                }} />
            </th>,
            <th>
                File
            </th>
        ].concat(heads);
    }

    const getSelectedCount = () => {
        if (!bulkSelections) {
            return 0;
        }
        var c = 0;
        for (var k in bulkSelections) {
            c++;
        }
        return c;
    }
    
    // the page number doesn't need state anymore
    // it is now collected from the query string. 
    // when one isn't present, it defaults to 1
    const pageIndex = getIntegerSearchParam('page', pageState.query, props.defaultPage || 1) as uint;

    const renderEntry = (entry : Upload) => {
        var id = `upload_${entry.id}`;
        var parsedRef = fileRef.parse(entry.ref as string);

        if (!parsedRef) {
            return null;
        }

        var focalX = parsedRef.focalX || 50;
        var focalY = parsedRef.focalY || 50;
        var url = fileRef.getUrl(parsedRef);
        var isImage = parsedRef.isImage();
        var isVideo = parsedRef.isVideo(false);
        var checked = bulkSelections ? !!bulkSelections[entry.id] : false;
        var checkbox = <>
            <input type='checkbox' className="btn-check" checked={checked} id={id} autoComplete="off" onChange={() => {
                const newSels = { ...bulkSelections };

                if (newSels[entry.id]) {
                    delete newSels[entry.id];
                } else {
                    newSels[entry.id] = true;
                }

                setBulkSelections(newSels);
            }} />
        </>;

        return <>
            <div className="media-center__list-item" title={entry.originalName}>
                {checkbox}
                <label className="btn btn-outline-secondary" htmlFor={id}>
                    {showRef(entry.ref)}
                    <span className="media-center__id badge bg-secondary rounded-pill">
                        {entry.usageCount && entry.usageCount > 0 &&
                            <span className="media-center__usage">
                                {entry.usageCount}
                            </span>
                        }
                        {entry.id}
                    </span>
                </label>

                {/* allow image properties (such as focal point) to be set */}
                    <button type="button" className="btn btn-sm btn-primary media-center__original-filename" data-clamp="2"
                    onClick={() => {
                        setUploadModal({
                            bulkUploaded: false,
                            existingFileRef: entry.ref,
                            uploadMode: UPLOAD_SINGLE,
                            uploadId: entry.id,
                            focalX: focalX,
                            focalY: focalY,
                            alt: entry.alt,
                            coverImageRef: entry.coverImageRef,
                            author: entry.author,
                            originalName: entry.originalName,
                            transcodeState: entry.transcodeState,
                            tags: entry.tags
                        });
                    }}>
                        {`Edit - `}{entry.originalName}
                    </button>

            </div>
        </>;
    }

    const renderBulkOptions = (selectedCount : number) => {
        var message = (selectedCount > 1) ? `${selectedCount} items selected` : `1 item selected`;

        return <div className="admin-page__footer-actions">
            <span className="admin-page__footer-actions-label">
                {message}
            </span>
            <button type="button" className="btn btn-danger" onClick={() => startDelete()}>
                {`Delete selected`}
            </button>
        </div>;
    }

    const startDelete = () => {
        setConfirmDelete(true);
    }

    const cancelDelete = () => {
        setConfirmDelete(false);
    }

    const doConfirmDelete = () => {
        setConfirmDelete(false);
        setDeleting(true);
        setDeleteFailed(false);

        // get the item IDs:
        var ids = Object.keys(bulkSelections!);

        var deletes = ids.map(id => uploadApi.delete(parseInt(id) as uint));

        Promise.all(deletes).then(response => {
            setBulkSelections(null);
            setDeleteCount(deleteCount + 1);
        }).catch(e => {
            console.error(e);
            setDeleting(false);
            setDeleteFailed(true);
        });
    }

    const renderConfirmDelete = (count : number) => {
        return <ConfirmModal confirmCallback={() => doConfirmDelete()} confirmVariant="danger" cancelCallback={() => cancelDelete()}>
            <p>
                {`Are you sure you want to delete ${count} item(s)?`}
            </p>
        </ConfirmModal>
    }

    const showUploadModal = (uploadMode : number) => {
        setUploadModal({
            uploadMode: uploadMode,
            bulkUploaded: false,
            focalX: 50,
            focalY: 50
        });
    }

    const cancelUpload = () => {
        setUploadModal(null);
    }

    const saveUpload = () => {
        if (!uploadModal) {
            return;
        }

        const { focalX, focalY, alt, author, tags, coverImageRef } = uploadModal;

        uploadApi.update(uploadModal.uploadId!,
            {
                focalX: focalX as int,
                focalY: focalY as int,
                'alt': alt,
                coverImageRef,
                'author': author,
                tags: tags ? tags.map(obj => obj.id) : null
            }
        ).then(() => {
            setUploadModal(null);
        });
    }

    const renderUploadModal = () => {
        if (!uploadModal) {
            return null;
        }

        const {
            bulkUploaded,
            uploadMode,
            existingFileRef,
            focalX,
            focalY
        } = uploadModal;

        var url = '';
        var title = `Upload media`;
        var isImage = false;
        var isVideo = false;
        var isNewMedia = true;

        if (existingFileRef) {
            var parsedRef = fileRef.parse(existingFileRef);

            if (parsedRef) {
                isNewMedia = false;
                url = fileRef.getUrl(parsedRef) || '';
                isImage = parsedRef.isImage();
                isVideo = parsedRef.isVideo(false);
            }

            title = `Edit - ` + uploadModal.originalName;
        } else if(uploadMode == UPLOAD_MULTIPLE) {
            title = `Bulk upload media`;
        }

        return <>
            <Modal visible isExtraLarge title={title}
                onClose={() => {
                    if (uploadMode == UPLOAD_MULTIPLE && bulkUploaded) {
                        window.location.reload();
                    } else {
                        cancelUpload();
                    }
                }}
                className="media-center__upload-modal">
                <div className="media-center__upload-modal-internal">
                    <Container>
                        <Row>
                            {!isNewMedia && <>
                                <Column sizeMd='6'>
                                    <div className='media-center__preview-wrapper'>
                                        <div className="media-center__preview"
                                            onClick={(e) => {
                                                var imagePreviewRect = (e.target as HTMLDivElement).getBoundingClientRect();
                                                const newFx = CLOSEST_MULTIPLE * Math.round((e.offsetX / imagePreviewRect.width * 100) / CLOSEST_MULTIPLE);
                                                const newFy = CLOSEST_MULTIPLE * Math.round((e.offsetY / imagePreviewRect.height * 100) / CLOSEST_MULTIPLE);

                                                setUploadModal({...uploadModal, focalX: newFx, focalY: newFy});
                                            }}>
                                            {showRef(uploadModal.existingFileRef as string, PREVIEW_SIZE)}
                                            {isImage && !isVideo && <>
                                                <div className="media-center__preview-crosshair" style={{
                                                    left: focalX + '%',
                                                    top: focalY + '%'
                                                }}></div>
                                            </>}
                                        </div>
                                    </div>
                                </Column>
                            </>}
                            {isNewMedia && <>
                                <Uploader
                                    multiple={uploadMode == UPLOAD_MULTIPLE ? true : undefined}
                                    onUploaded={(e) => {

                                        if (uploadMode == UPLOAD_MULTIPLE) {
                                            setUploadModal({
                                                ...uploadModal, bulkUploaded: true
                                            });
                                            return;
                                        }

                                        if (!e.result.isImage) {
                                            window.location.reload();
                                            return;
                                        }
                                        setUploadModal({
                                            ...uploadModal,
                                            focalX: 50,
                                            focalY: 50,
                                            existingFileRef: e.result.ref,
                                            uploadId: e.result.id,
                                        });
                                    }} />
                            </>}
                            {!isNewMedia && <>
                                <Column sizeMd='6'>
                                    <div className="media-center__metadata">

                                        {isImage &&
                                            <>
                                            <div className="media-center__transcode">
                                                {`Transcode Status`}:{uploadModal.transcodeState}
                                            </div>

                                        <div className="form-text media-center__alt">
                                                <Input type="text" label={`Author/Photographer`} value={uploadModal.author} onChange={e => {
                                                    const input = (e.target as HTMLInputElement);
                                                    setUploadModal({ ...uploadModal, author: input.value });
                                            }} />
                                        </div>

                                        <div className="form-text media-center__alt">
                                                <Input type="text" label={`Alternative Text`} value={uploadModal.alt} onChange={e => {
                                                    const input = (e.target as HTMLInputElement);
                                                    setUploadModal({ ...uploadModal, alt: input.value });
                                            }} />
                                        </div>
                                            </>
                                        }

                                        {isVideo && 
                                            <div className="form-text media-center__alt">
                                                <Input type="image" label={`Cover image`} value={uploadModal.coverImageRef} onChange={e => {
                                                    const input = (e.target as HTMLInputElement);
                                                    setUploadModal({ ...uploadModal, coverImageRef: input.value });
                                                }} />
                                            </div>
                                        }

                                        {isImage && !isVideo &&
                                            <div className="form-text media-center__focal-point">
                                                <button type="button" className="btn btn-sm btn-outline-secondary me-2" onClick={() => {
                                                    setUploadModal({ ...uploadModal, focalX: 50, focalY: 50 });
                                                }}>
                                                    <i className="fal fa-fw fa-sync"></i>
                                                </button>
                                                {focalX}%, {focalY}%
                                            </div>
                                        }

                                        <MultiSelect value={uploadModal.tags} contentType='tag' field='name' label={`Folders`} showCreateOrEditModal={true}
                                            onChange={e => {
                                                setUploadModal({ ...uploadModal, tags: e.fullValue });
                                            }}>
                                        </MultiSelect>

                                    </div>

                                </Column>
                            </>}
                        </Row>
                    </Container>
                </div>

                <footer className="media-center__upload-modal-footer">
                    {!isNewMedia && <>
                        <a href={url} target="_blank" className="btn btn-secondary">
                            <i className="fa-fw fal fa-external-link"></i> {`Preview`}
                        </a>
                    </>}
                    {isNewMedia && <>&nbsp;</>}
                    <div className="media-center__upload-modal-footer-options">

                        {isNewMedia && <>
                            <button type="button" className="btn btn-primary" onClick={() => {

                                if (uploadMode == UPLOAD_MULTIPLE && bulkUploaded) {
                                    window.location.reload();
                                } else {
                                    cancelUpload();
                                }

                            }}>
                                {`Close`}
                            </button>
                        </>}
                        {!isNewMedia && <>
                            <button type="button" className="btn btn-outline-primary" onClick={() => cancelUpload()}>
                                {`Cancel`}
                            </button>
                            <button type="button" className="btn btn-primary" onClick={() => saveUpload()}>
                                {`Save`}
                            </button>
                        </>}
                    </div>
                </footer>
            </Modal>
        </>;
    }
    
    var selectedCount = getSelectedCount();
    
    // here we build up the filter that the loop component uses. 
    // a query will not always be present, however we do always 
    // have a start and a page limit, so these exist as absolutes.
    const filter: Partial<ListFilter> = {
        pageSize,
        pageIndex,
        sort: {
            field: 'id',
            direction: 'desc'
        }
    } 
    
    // when a string is empty, in an if condition, it's executed as false, 
    // which means this AST branch doesn't execute, however, when a 
    // string holds a value, it's executed as true, meaning
    // we can validly pass a string in an if condition.
    // when a search value exists, we make the filter query the originalName, 
    // which is what is shown under the file's preview. 
    if (searchFilter) {
        // added cases for author and alt text
        // so when the query partially matches the alt or
        // author, results show for anything that matches. 
        filter.query = 'originalName contains ? or author contains ? or alt contains ?';
        filter.args = [searchFilter, searchFilter, searchFilter]
    }
    
    return <>
		<SubHeader 
            title={`Uploads`} 
            breadcrumbs={[
                {
                    title: `Uploads`
                }
            ]} 
            // altered this now to debounce. 
            onQuery={(where, query) => {
                
            }}
            onInput={(query) => {
                debounce.handle(query);
            }}
            // made sure the current '?q=' value is held in here 
            // should one exist.
            defaultSearchValue={searchFilter || ''}
        />
		<div className="admin-page__content">
			<div className="admin-page__internal">
                <Input 
                    // added an input to change the results per page value
                    // this doesn't mutate any state, this causes a URL change
                    // which then re-renders the page. 
                    type={'select'}
                    label={`Results per page`}
                    onChange={(ev) => {
                        
                        const target: HTMLSelectElement = ev.target as HTMLSelectElement;
                        updateQuery({ limit: target.value });
                        
                    }}
                >
                    <option selected={pageSize === 20} value={20}>20</option>
                    <option selected={pageSize === 50} value={50}>50</option>
                    <option selected={pageSize === 75} value={75}>75</option>
                    <option selected={pageSize === 100} value={100}>100</option>
                </Input>
                {/*{renderTags(uploads)}*/}
                <div className="media-center__list">
                    <Loop
                        // enables pagination
                        paged
                        // iterates over uploadApi.list 
                        over={uploadApi}
                        // set the key based off index
                        key={'page-' + pageIndex}
                        // pass the generated filter based off the current query string
                        filter={filter}
                        // specify a custom page change handler.
                        customChangeHandler={(pageNumber: number) => {
                            updateQuery({ page: pageNumber.toString() });
                        }}
                        includes={[
                            uploadApi.includes.tags
                        ]}
                    >
                        {renderEntry}
                    </Loop>
				</div>
				{confirmDelete && renderConfirmDelete(selectedCount)}
				{uploadModal && renderUploadModal()}
			</div>
			<footer className="admin-page__footer no-pad">
				{selectedCount > 0 ? renderBulkOptions(selectedCount) : null}
				<button type="button" className="btn btn-primary" onClick={() => showUploadModal(UPLOAD_SINGLE)}>
					{`Upload`}
				</button>
				<button type="button" className="btn btn-primary" onClick={() => showUploadModal(UPLOAD_MULTIPLE)}>
					{`Bulk upload`}
				</button>
			</footer>
		</div>
    </>;
}

export default MediaCenter;