import {ring} from 'UI/Functions/HuddleClient';
import Modal from 'UI/Modal';
import Icon from 'UI/Icon';
import Content from 'UI/Content';
import websocket from 'UI/Functions/WebSocket';
import getRef from 'UI/Functions/GetRef';


export default function HuddleRing(props){
	// a call now button.
	var wrapperClass = "huddle-ring-call-button-wrapper";
	
	if(props.noWrapper){
		return renderCallNow(props);
	}
	
	return <div className={wrapperClass}>
		{renderCallNow(props)}
	</div>;
}

function renderCallNow(props) {
	var [activeRinger, setActiveRinger] = React.useState(false);
	var btnClass = props.btnClass ? props.btnClass : "huddle-ring-call-button";
	
	return <>
		<button className={btnClass} type="button" onClick={e => {
			e.stopPropagation();
			e.preventDefault();
			setActiveRinger(1);
		}}>
			{props.children || <Icon type="phone" light/>}
		</button>
		{activeRinger && <CallUI user={props.guests[0]} intro={`Calling...`} theme={props['data-theme']}>
			<HuddleRinger {...props} onHangUp={() => setActiveRinger(null)}/>
		</CallUI>}
	</>
}


function HuddleRinger(props){
	var [activeRing, setActiveRing] = React.useState();
	
	var hangUp = () => {
		activeRing && activeRing.stop();
		props.onHangUp && props.onHangUp();
	};
	
	React.useEffect(() => {
		var r = ring(props.guests[0].id);
		setActiveRing(r);
	}, []);
	
	return <div>
		<button className="btn btn-danger hang-up" onClick={hangUp}><Icon type="phone" /></button>
		<p className="hint-text">{`Cancel`}</p>
	</div>;
}

function getInitials(name){
	if(!name){
		return '';
	}
	
	var parts = name.split(' ');
	
	if(parts.length == 1){
		return name[0];
	}
	
	return parts[0][0] + parts[parts.length-1][0];
}

function CallUI(props){
	var {user} = props;
	var avatarUrl = user.avatarRef ? getRef(user.avatarRef, { url: true, size: 100 }) : "";
	var style = { "background-image": "url(" + avatarUrl + ")" };
	var wrapperClassName = "profile-avatar has-image";
	
	return <div className="modal show">
		<div className="modal-dialog show modal-dialog-centered">
			<div className="modal-content">
			<section className="huddle-call-ui" data-theme={props.theme || 'huddle-ring-theme'}>
				<div className={wrapperClassName} style={style}>
					<span className="profile-initials">{getInitials(user.fullName)}</span>
				</div>
				{props.intro && <p>{props.intro}</p>}
				<p className="profile-name">{user.fullName}</p>
				<center className="call-options">
					{props.children}
				</center>
			</section>
			</div>
		</div>
	</div>;
}

export function RingListener(props){
	
	var [incomingRing, setIncomingRing] = React.useState();
	
	React.useEffect(() => {
		
		var opcode = websocket.registerOpcode(41, reader => {
			var userId = reader.readUInt32();
			
			if(!incomingRing){
				incomingRing = {userId};
				setIncomingRing(incomingRing);
			}else if(incomingRing.userId == userId){
				// Pushback the kill interval.
				clearTimeout(incomingRing.i);
			}else{
				// 2+ rings at once! handle this however you'd like. Can also just ignore it, like this:
				return;
			}
			
			incomingRing.i = setTimeout(() => {
				setIncomingRing(null);
			}, 2000);
			
		});
		
		return () => {
			opcode.unregister();
		};
		
	});
	
	if(incomingRing){
		
		return <Content type='user' id={incomingRing.userId}>
			{user => {
				if(!user){
					return null;
				}
				
				return <CallUI user={user} theme={props['data-theme']}>
						<button type="button" className="btn btn-primary me-3" onclick={() => {console.log("Accept call")}}>
							{`Accept`}
						</button>
						<button type="button" className="btn btn-secondary" onclick={() => {console.log("Decline call")}}>
							{`Decline`}
						</button>
				</CallUI>;
			}}
		</Content>;
	}
	
	return null;
}