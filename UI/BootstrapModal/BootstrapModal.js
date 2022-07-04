import { useState } from 'react';
import CloseButton from 'UI/CloseButton';

const MODAL_PREFIX = 'modal';
const MODAL_DIALOG_PREFIX = MODAL_PREFIX + '-dialog';

/**
 * Bootstrap Modal component
 * @param {string} title		- modal title
 * @param {boolean} showClose	- set true to show close button within header
 * @param {boolean} scrollable  - set true to allow content to be scrollable
 * @param {boolean} verticallyCentred  - set true to vertically centre modal
 */
export default function BootstrapModal(props) {
    const { title, showClose, scrollable, verticallyCentred } = props;
    const [showModal, setShowModal] = useState(false);

    var modalClass = [MODAL_PREFIX];
    var modalDialogClass = [MODAL_DIALOG_PREFIX];

    if (scrollable) {
        modalDialogClass.push(MODAL_DIALOG_PREFIX + '-scrollable');
    }

    if (verticallyCentred) {
        modalDialogClass.push(MODAL_DIALOG_PREFIX + '-centered');
    }

    return <>
        {showModal && <>
            <div className={modalClass.join(' ')} tabindex="-1">
                <div className={modalDialogClass.join(' ')}>
                    <div className="modal-content">
                        <div className="modal-header">
                            <h5 className="modal-title">
                                {title}
                            </h5>
                            {showClose && <>
                                <CloseButton callback={() => setShowModal(false)} />
                            </>}
                        </div>
                        <div className="modal-body">
                            {children}
                        </div>
                        {/* NB: not using a separate modal-footer block here, allowing us to define footer controls within {children} */}
                    </div>
                </div>
            </div>
        </>}
	</>;
}

BootstrapModal.propTypes = {
};

BootstrapModal.defaultProps = {
}

BootstrapModal.icon='align-center';
