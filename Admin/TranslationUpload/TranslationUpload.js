import React, { useState } from 'react';
import { baseFileOptions, convertFileToBase64, onFailure } from 'UI/Functions/RequestTools';
import { useRef } from 'react';
import { useToast } from 'UI/Functions/Toast';

export default function TranslationUpload(props) {

    const [selectedFile, setSelectedFile] = useState(null);
    const [uploading, setUploading] = useState(false);
    const [uploaded, setUploaded] = useState(false);
    const [failed, setFailed] = useState(null);
    const [status, setStatus] = useState(null);

    const entityRef = useRef(null);
    const fileRef = useRef(null);

    const { pop } = useToast();

    const imageUpload = (e) => {
        // update the real file object
        setSelectedFile(e.target.files[0]);
    };

    const onSubmit = (e) => {
        if (e.preventDefault)
            e.preventDefault();
		
        if (!entityRef.current || entityRef.current.value.length == 0) return;
        if (!fileRef.current || fileRef.current.value.length == 0) return;

		setUploading(true);
        setFailed(null);
        setUploaded(false);
        setStatus(null);

        var options = {
            method: 'put',
            mode: 'cors',
            credentials: 'include',
            body: selectedFile
        };

        var action = '/v1/' + entityRef.current.value + '/list.pot';

        fetch(action, options).then(response => {
            setUploading(false);
            if (response.ok) {
                return response.json();
            }
            return null;
        }).then(data => {
            if (data && data.success) {
                //ok force refresh 
                setStatus(data);
                setUploaded(true);
            } else {
                setFailed("Invalid format response");
            }
        }).catch(reason => {
            setFailed(reason);
            setUploading(false);
        });
    };

    return (

            <form>

                <div className="form-group">
                    <label for="entity" className="form-label">
                        {`Entity Name`}
                    </label>
                    <input id="entity" ref={entityRef} type="text" className="form-control" />
                </div>

                <div className="form-group">

                    {selectedFile && (
                        <div>
                            <div>{selectedFile.name}</div> 
                            <span onClick={() => setSelectedFile(null)}>{`Remove`}</span>
                        </div>
                    )}

                    <label for="file" className="form-label fr fr-download">
                        {`Upload your translation pot file`}
                    </label>

                    <input id="file" ref={fileRef} type="file" className="form-control" onChange={(e) => { imageUpload(e) }} />

                </div>
					
				{uploading ?  
					<div className="share-loading">
						<i className="fas fa-spinner fa-spin" />
					</div>
					: 
					<span className="btn btn-primary" onClick={e => onSubmit(e)}>
						{`Process translation file`}
					</span>
				}

                {uploaded && status && 
                    <div>
                        {`Translations uploaded`} - {`Updated`} {status.updated} - {`Missing`} {status.missing} - {`Skipped`} {status.skipped} 
                    </div>
                }

                {failed && 
                    <div>
                        {`Translations failed`} - {failed} 
                    </div>
                }
            </form>

    )
}