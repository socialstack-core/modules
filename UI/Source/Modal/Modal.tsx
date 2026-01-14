import { useEffect, useState } from 'react';
import * as fileRef from 'UI/FileRef';

interface ButtonInfo {
  label: string;
  onClick: () => void;
  className?: string;
  props?: React.ButtonHTMLAttributes<HTMLButtonElement>;
}

interface ModalProps {
  buttons?: ButtonInfo[];
  onOpen?: () => void;
  onClose?: () => void;
  visible: boolean;
  className?: string;
  fade?: boolean;
  isNotCentred?: boolean;
  isNotScrollable?: boolean;
  isSmall?: boolean;
  isLarge?: boolean;
  isExtraLarge?: boolean;
  customClass?: string;
  hideSelector?: string;
  backgroundImageRef?: string;
  closeIcon?: React.ReactNode;
  noBackdrop?: boolean;
  noHeader?: boolean;
  noClose?: boolean;
  noFooter?: boolean;
  footer?: () => React.ReactNode;
  children?: React.ReactNode;
  title?: string | React.ReactNode;
  'data-theme'?: string;
}

let titleId = 1;

const Modal: React.FC<ModalProps> = (props) => {
  const [modalTitleId, setModalTitleId] = useState<string>('');

  const { onOpen } = props;

  useEffect(() => {
    newTitleId();
    if (onOpen) {
      onOpen();
    }
  }, [onOpen]);

  const newTitleId = () => {
    setModalTitleId(`modal_title_${titleId++}`);
  };

  const backdropClassName = () => {
    return 'modal-backdrop show';
  };

  const modalClassName = () => {
    let classes = props.className || '';
    classes += ' modal';
    if (props.fade) {
      classes += ' fade';
    } else {
      classes += ' show';
    }

    return classes;
  };

  const modalDialogClassName = () => {
    let classes = 'modal-dialog show';

    if (!props.isNotCentred) {
      classes += ' modal-dialog-centered';
    }

    if (!props.isNotScrollable) {
      classes += ' modal-dialog-scrollable';
    }

    if (props.isSmall) {
      classes += ' modal-sm';
    }

    if (props.isLarge) {
      classes += ' modal-lg';
    }

    if (props.isExtraLarge) {
      classes += ' modal-xl';
    }

    if (props.customClass) {
      classes += ` ${props.customClass}`;
    }

    return classes;
  };

  const closeModal = () => {
    if (props.hideSelector) {
      const hideElements = Array.prototype.slice.apply(
        document.querySelectorAll(props.hideSelector)
      );

      hideElements.forEach((element) => {
        element.classList.remove('hidden-by-modal');
      });
    }

    if (props.onClose) {
      props.onClose();
    }
  };

  if (!props.visible) {
    return null;
  }

  if (props.hideSelector) {
    const hideElements = Array.prototype.slice.apply(
      document.querySelectorAll(props.hideSelector)
    );

    hideElements.forEach((element) => {
      element.classList.add('hidden-by-modal');
    });
  }

  const style: React.CSSProperties = {};
  if (props.backgroundImageRef) {
    style.backgroundImage = `url(${fileRef.getUrl(props.backgroundImageRef)})`;
    style.height = '690px'; /* You must set a specified height */
    style.backgroundPosition = 'center'; /* Center the image */
    style.backgroundRepeat = 'no-repeat'; /* Do not repeat the image */
    style.backgroundSize = 'cover';
  }

  const closeClass = props.closeIcon ? 'close btn-close custom-icon' : 'close btn-close';
  const closeIconClass = props.closeIcon ? 'close-icon custom-icon-content' : 'close-icon';

  return [
    props.noBackdrop ? null : (
      <div className={backdropClassName()} onClick={() => closeModal()}></div>
    ),
    <div
      className={modalClassName()}
      tabIndex={-1}
      role="dialog"
      aria-labelledby={modalTitleId}
      data-theme={props['data-theme'] || 'modal-theme'}
      onKeyUp={
        props.noClose
          ? undefined
          : (e: React.KeyboardEvent) => {
              if (e.key === 'Escape') {
                closeModal();
              }
            }
      }
      onTouchStart={(e) => e.stopPropagation()}
    >
      <div className={modalDialogClassName()} role="document">
        <div className="modal-content" style={style}>
          {!props.noHeader ? (
            <div className="modal-header">
              <div className="modal-title" id={modalTitleId}>
                {typeof props.title === 'string' ? <h5>{props.title}</h5> : props.title}
              </div>
              {!props.noClose && (
                <button
                  type="button"
                  className={closeClass}
                  data-dismiss="modal"
                  aria-label="Close"
                  onClick={() => closeModal()}
                >
                  <span aria-hidden="true" className={closeIconClass}>
                    {!props.closeIcon && <>×</>}
                    {props.closeIcon && <>{props.closeIcon}</>}
                  </span>
                </button>
              )}
            </div>
          ) : (
            <></>
          )}
          <div className="modal-body">{props.children}</div>
          {!props.noFooter && (
            <div className="modal-footer">
              {props.footer ? props.footer() : renderButtons()}
            </div>
          )}
        </div>
      </div>
    </div>,
  ];

  function renderButtons() {
    if (!props.buttons) {
      return null;
    }

    return props.buttons.map((buttonInfo, index) => (
      <button
        key={index}
        type="button"
        className={'btn ' + (buttonInfo.className || 'btn-primary')}
        onClick={buttonInfo.onClick}
        {...(buttonInfo.props || {})}
      >
        {buttonInfo.label}
      </button>
    ));
  }
};

export default Modal;
