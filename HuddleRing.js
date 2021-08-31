import {ring, sendRing, ringReject} from 'UI/Functions/HuddleClient';
import Icon from 'UI/Icon';
import Content from 'UI/Content';
import websocket from 'UI/Functions/WebSocket';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';
import { useRouter } from 'UI/Session';

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
	var [guests, setGuests] = React.useState(props.guests);
	var [declined, setDeclined] = React.useState([]);	

	var btnClass = props.btnClass ? props.btnClass : "huddle-ring-call-button";
	
	return <>
		<button className={btnClass} type="button" onClick={e => {
			e.stopPropagation();
			e.preventDefault();
			setActiveRinger(1);
		}}>
			{props.children || <Icon type="phone" light/>}
		</button>
		{activeRinger && <CallUI declined = {declined} setDeclined = {setDeclined} guests = {guests} setGuests = {setGuests} activeRinger = {activeRinger} setActiveRinger = {setActiveRinger} guests={props.guests} intro={`Calling...`} theme={props['data-theme']}>
			<HuddleRinger declined = {declined} setDeclined = {setDeclined} guests = {guests} setGuests = {setGuests} activeRinger = {activeRinger} setActiveRinger = {setActiveRinger} {...props} onHangUp={() => setActiveRinger(null)}/>
		</CallUI>}
	</>
}


function HuddleRinger(props){
	var {activeRinger, setActiveRinger, guests, setGuests, declined, setDeclined} = props;
	
	var hangUp = () => {
		activeRinger && activeRinger.stop();
		props.onHangUp && props.onHangUp();
	};

	console.log("declined", declined);
	
	React.useEffect(() => {
		
		var args = {};
		// Check if we are passing in extra args for the creation of the huddle that may be project specific. Defaults to only adding permitted users.
		if (this.props.extraArgs) {
			args = {...this.props.extraArgs, userPermits: props.guests.map(guest => guest.id)}
		}
		else {
			args = {userPermits: props.guests.map(guest => guest.id)}
		}

		webRequest("huddle", args).then(response => {
			var huddle = response.json;
			var guestIds = [];
			props.guests.forEach(g => {console.log("guest!!!", g); guestIds.push(g.id)});

			console.log("guestIds", guestIds);

			var r = ring(guestIds, huddle.slug);
			setActiveRinger(r);
		});

		var opcode = websocket.registerOpcode(41, reader => {
			console.log("reader ", reader);
			var slug = reader.readUtf8();
			var mode = reader.readByte();
			var userId = reader.readUInt32();

			console.log("slug", slug);
			console.log("mode", mode);
			console.log("userId", userId);

			if(mode == 2) {
				console.log("they are answering!");
			} else if (mode == 3) {
				console.log("they are declining!");
				ringReject(slug, userId);
				declined.push(userId);
				setDeclined(declined);
			}
		});

		return () => {
			opcode.unregister();
		};

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
	var {activeRinger, setActiveRinger, guests, setGuests, declined, setDeclined} = props;

	var user = guests[0];
	var isGroup = guests.length > 1;
	var avatarUrl = isGroup ? "" : user.avatarRef ? getRef(user.avatarRef, { url: true, size: 100 }) : "";
	var style = { "background-image": "url(" + avatarUrl + ")" };
	var wrapperClassName = "profile-avatar has-image";
	var everyoneHungup = guests.length == declined.length;

	return <div className="modal show">
		<div className="modal-dialog show modal-dialog-centered">
			<div className="modal-content">
			<section className="huddle-call-ui" data-theme={props.theme || 'huddle-ring-theme'}>
				<div className={wrapperClassName} style={style}>
					<span className="profile-initials">{isGroup ? "Group" : user ? getInitials(user.fullName) : ""}</span>
				</div>
				{everyoneHungup ? <p>Your call has been declined.</p> : props.intro && <p>{props.intro}</p>}
				<p className="profile-name">
					{
						guests.map(guest => {
							console.log("checking guests!");
							console.log("declined", declined);
							console.log("guest id", guest.id);
							console.log("decline includes guest id", declined.includes(guest.id));
							var declinedCall = false;
							declined.forEach(d => {console.log("the declined id", d, "guest id", guest.id); if(d == guest.id) {declinedCall = true}})

							if(declinedCall) {
								return <>{guest.fullName} <i class="fas fa-phone-slash"></i></>
							}
							return <>{guest.fullName} <i class="fas fa-phone"></i></>
						})
					}
				</p>
				<center className="call-options">
					{props.children}
				</center>
			</section>
			</div>
		</div>
	</div>;
}

function CallFromUI(props) {
	var {activeRinger, setActiveRinger, guests, setGuests, declined, setDeclined, user} = props;

	// How many guests does our activeRing have?
	console.log("activeRinger", activeRinger);
	console.log("guests", guests);
	console.log("declined", declined);

	//var user = guests[0];
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
				<p className="profile-name">
					{user.fullName}
				</p>
				<center className="call-options">
					{props.children}
				</center>
			</section>
			</div>
		</div>
	</div>;
}

export function RingListener(props){
	const {setPage} = useRouter();
	var [incomingRing, setIncomingRing] = React.useState();
	
	React.useEffect(() => {
		
		var opcode = websocket.registerOpcode(41, reader => {

			var slug = reader.readUtf8();
			var mode = reader.readByte();
			var userId = reader.readUInt32();

			if (mode == 1) {// Mode 1 is an incoming ring
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
			}

		});
		
		return () => {
			opcode.unregister();
		};
		
	});

	var hangup = (incomingRing) => {
		sendRing(incomingRing.slug, 3, incomingRing.userId);
		setIncomingRing(null);
	}

	var answer = (incomingRing) => {
		sendRing(incomingRing.slug, 2, incomingRing.userId);
		setIncomingRing(null);
		setPage(props.huddleUrl + incomingRing.slug)
	}
	
	if(incomingRing){
		
		return <Content type='user' id={incomingRing.userId}>
			{user => {
				if(!user){
					return null;
				}
				
				return <CallFromUI user={user} theme={props['data-theme']}>
						<button type="button" className="btn btn-primary me-3" onclick={() => {answer(incomingRing)}}>
							{`Accept`}
						</button>
						<button type="button" className="btn btn-secondary" onclick={() => hangup(incomingRing)}>
							{`Decline`}
						</button>
				</CallFromUI>;
			}}
		</Content>;
	}
	
	return null;
}