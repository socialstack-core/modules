import Modal from 'UI/Modal';
import VideoChat from 'UI/VideoChat';

export default function CallModal(props){	
	return renderCallModal(props);
}

function renderCallModal(props) {
	var [callActive, setCallActive] = React.useState(true);

	function onClose() {
		setCallActive(false);
		if (props.hangup) {
			props.hangup();
		}
	}

	if (!props.callSlug || !callActive) {
		return null;
	}
	
	return <Modal visible={callActive} isExtraLarge noClose customClass="call-modal">
			<VideoChat roomSlug={props.callSlug} onClose={onClose} closeWhenNoPeers={props.closeWhenNoPeers}/>
		</Modal>;
	
}