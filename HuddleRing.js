import {ring, sendRing} from 'UI/Functions/HuddleClient';
import Modal from 'UI/Modal';
import Icon from 'UI/Icon';
import Content from 'UI/Content';
import websocket from 'UI/Functions/WebSocket';
import webRequest from 'UI/Functions/WebRequest';
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
		webRequest("huddle", { userPermits: props.guests.map(guest => guest.id) }).then(response => {
			var huddle = response.json;
			var r = ring([props.guests[0].id], huddle.slug);
			setActiveRing(r);
		});
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
			
			var slug = reader.readUtf8();
			var mode = reader.readByte();
			var userId = reader.readUInt32();

			console.log("slug", slug);
			console.log("mode", mode);
			console.log("userId", userId);

			
			if(!incomingRing){
				incomingRing = {userId, slug};
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

	var hangup = (incomingRing) => {
		console.log("hang up clicked");
		sendRing(incomingRing.slug, 3, incomingRing.userId);
	}

	var answer = (incomingRing) => {
		console.log("answer clicked");
		sendRing(incomingRing.slug, 2, incomingRing.userId);
	}
	
	if(incomingRing){
		
		return <Content type='user' id={incomingRing.userId}>
			{user => {
				if(!user){
					return null;
				}
				
				return <CallUI user={user} theme={props['data-theme']}>
						<button type="button" className="btn btn-primary me-3" onclick={() => {answer(incomingRing)}}>
							{`Accept`}
						</button>
						<button type="button" className="btn btn-secondary" onclick={() => hangup(incomingRing)}>
							{`Decline`}
						</button>
				</CallUI>;
			}}
		</Content>;
	}
	
	return null;
}