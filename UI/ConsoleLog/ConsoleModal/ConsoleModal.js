import Modal from 'UI/Modal';
import ConsoleLog from 'UI/ConsoleLog';

export default function ConsoleModal(props) {

  const onClose = () => {
    props.close();
}

  return (    
    
    <Modal
      title={`Console Log`}
      onClose={onClose}
      visible={props.isVisible}
    >
        <ConsoleLog theme='light' />
    </Modal>

  )
}