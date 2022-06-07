import StageView from './StageView';
import PinnedView from './PinnedView';
import AudienceView from './AudienceView';
import Header from './Header';
import Options from './Options';
import demoAudio from './curb.mp3';
import demo1 from './demo1.mp4';
import demo2 from './demo2.mp4';
import demo3 from './demo3.mp4';
import demo4 from './demo4.mp4';
import demo5 from './demo5.mp4';
import demo6 from './demo6.mp4';
import demoSharing1 from './abstract.mp4';
import demoSharing2 from './cat.mp4';
import getRef from 'UI/Functions/GetRef';
import { useState, useEffect, useRef } from 'react';
import ChatLive from 'UI/ChatLive';
import HuddleClient from 'UI/HuddleClient';
import Container from 'UI/Container';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Loading from 'UI/Loading';

const MAX_STAGE_USERS = 6;
const MAX_PINNED_USERS = 10;
const MAX_SHARING_USERS = 2;
const MAX_AUDIENCE_PER_PAGE = 12;
const REMOVE_USER_INTERVAL = 5 * 1000;

// Set to true to preview a group of users
var DEMO_MODE = false;

const SidebarEnum = Object.freeze({
	CLOSED: 0,
	AUDIENCE: 1,
	CONVERSATION: 2
});

function castVideoFile(fileRef) {
	var video = document.createElement('video');
	video.loop = true;
	video.src = getRef(fileRef, { url: true });

	return video.play().then(() => {

		if (video.mozCaptureStream) {
			return video.mozCaptureStream();
		}

		return video.captureStream();
	});
}

function castAudioFile(fileRef) {
	var audio = new Audio();
	audio.loop = true;
	audio.src = getRef(fileRef, { url: true });

	return audio.play().then(() => {
		var AudioContext = window.AudioContext || window.webkitAudioContext;
		var audioCtx = new AudioContext();
		var eleSource = audioCtx.createMediaElementSource(audio);
		var mixedOutput = audioCtx.createMediaStreamDestination();
		eleSource.connect(mixedOutput);
		return mixedOutput.stream;
	});
}

function HuddleChatDemo(props) {
	// **********************
	// load demo data
	
	const [demoAudio1Track, setDemoAudio1Track] = useState(null);
	const [demoVideo1Track, setDemoVideo1Track] = useState(null);
	const [demoVideo4Track, setDemoVideo4Track] = useState(null);
	const [demoVideo6Track, setDemoVideo6Track] = useState(null);
	const [demoSharing1Track, setDemoSharing1Track] = useState(null);
	const [demoSharing2Track, setDemoSharing2Track] = useState(null);
	
	var [huddleClient, setHuddleClient] = useState(() => {
		
		var client = new HuddleClient({
			slug: props.slug, // originate from URL
			serviceHost: props.serviceHost,
			host: props.host,
			isHttp: props.isHttp,
			avatarRef: props.avatarRef,
			displayName: props.displayName
		});
		
		// Client not started for demo mode, ignoring the user events as well
		return client;
	});
	
	var examples = [
		{
			audioTrack: demoAudio1Track,
			channels: (demoAudio1Track ? 1 : 0) + (demoVideo1Track ? 2 : 0) + (demoSharing2Track ? 4 : 0),
			createdUtc: Date.now(),
			creatorUser: {
				id: 1,
				displayName: 'Gloria',
				avatarRef: 'public:6DBAE2A6F533051CE199D7FAD982E2D1/2.jpg'
			},
			editedUtc: Date.now(),
			gone: false,
			huddleId: 1,
			id: 1,
			isOnStage: true,
			serverId: 1,
			sharingTrack: demoSharing2Track,
			stageArrivalUtc: Date.now(),
			type: "HuddlePresence",
			userId: 1,
			videoTrack: demoVideo1Track,
			isWebcamOn: !!demoVideo1Track,
			stageSlotId: 1
		},
		{
			audioTrack: null,
			channels: (demoVideo6Track ? 2 : 0), // video only
			createdUtc: Date.now(),
			creatorUser: {
				id: 2,
				displayName: 'Dave',
				avatarRef: 'public:6DBAE2A6F533051CE199D7FAD982E2D1/2.jpg'
			},
			editedUtc: Date.now(),
			gone: true,
			huddleId: 1,
			id: 2,
			isOnStage: true,
			serverId: 2,
			sharingTrack: null,
			stageArrivalUtc: Date.now(),
			type: "HuddlePresence",
			userId: 2,
			videoTrack: demoVideo6Track,
			isWebcamOn: !!demoVideo6Track,
			stageSlotId: 2
		},
		{
			audioTrack: null,
			channels: (demoVideo4Track ? 2 : 0) + (demoSharing1Track ? 4 : 0),
			createdUtc: Date.now(),
			creatorUser: {
				id: 3,
				displayName: 'Markus',
				avatarRef: 'public:6DBAE2A6F533051CE199D7FAD982E2D1/3.jpg'
			},
			editedUtc: Date.now(),
			gone: false,
			huddleId: 1,
			id: 3,
			isOnStage: true,
			serverId: 3,
			sharingTrack: demoSharing1Track,
			stageArrivalUtc: Date.now(),
			type: "HuddlePresence",
			userId: 3,
			videoTrack: demoVideo4Track,
			isWebcamOn: !!demoVideo4Track,
			stageSlotId: 3
		}/*,
		{
			id: 6,
			userId: 6,
			displayName: 'Lurker',
			avatarRef: 'public:6DBAE2A6F533051CE199D7FAD982E2D1/3.jpg',
			audioOn: false,
			videoOn: false
		},
		{
			id: 7,
			userId: 7,
			displayName: 'Steve',
			avatarRef: 'public:6DBAE2A6F533051CE199D7FAD982E2D1/2.jpg',
			audioOn: true,
			videoOn: true,
			videoUrl: demo3Url
		},
		{
			id: 8,
			userId: 8,
			displayName: 'Geoff',
			avatarRef: 'public:6DBAE2A6F533051CE199D7FAD982E2D1/2.jpg',
			audioOn: true,
			videoOn: true,
			videoUrl: demo4Url
		}
		*/
	];

	// fill stage
	for (var i = 1; i <= 3; i++) {
		var j = i + 3;
		examples.push({
			audioTrack: null,
			channels: (demoVideo1Track ? 2 : 0),
			createdUtc: Date.now(),
			creatorUser: {
				id: j,
				displayName: 'Dummy User ' + j,
				avatarRef: 'public:6DBAE2A6F533051CE199D7FAD982E2D1/2.jpg'
			},
			editedUtc: Date.now(),
			gone: false,
			huddleId: 1,
			id: j,
			isOnStage: true,
			serverId: 1,
			sharingTrack: null,
			stageArrivalUtc: Date.now(),
			type: "HuddlePresence",
			userId: j,
			videoTrack: demoVideo1Track,
			isWebcamOn: !!demoVideo1Track,
			stageSlotId: j
		});

    }

	// add to pinned bar / overflow to audience
	var total = 6;
	//var total = 50;
	for (var i = 1; i <= total; i++) {
		var j = i + 6;
		examples.push({
			audioTrack: null,
			channels: (demoVideo1Track ? 2 : 0),
			createdUtc: Date.now(),
			creatorUser: {
				id: j,
				displayName: 'Dummy User ' + j,
				avatarRef: 'public:6DBAE2A6F533051CE199D7FAD982E2D1/2.jpg'
			},
			editedUtc: Date.now(),
			gone: false,
			huddleId: 1,
			id: j,
			isOnStage: true,
			serverId: 1,
			sharingTrack: null,
			stageArrivalUtc: Date.now(),
			type: "HuddlePresence",
			userId: j,
			videoTrack: demoVideo1Track,
			isWebcamOn: !!demoVideo1Track
		});

	}
	
	function loadTracks() {

		castAudioFile(demoAudio).then(stream => {
			//setDemoAudio1Track(stream.getTracks()[0]);
		});

		castVideoFile(demo1).then(stream => {
			setDemoVideo1Track(stream.getTracks()[0]);
		});

		castVideoFile(demo4).then(stream => {
			setDemoVideo4Track(stream.getTracks()[0]);
		});

		castVideoFile(demo6).then(stream => {
			setDemoVideo6Track(stream.getTracks()[0]);
		});

		castVideoFile(demoSharing1).then(stream => {
			//setDemoSharing1Track(stream.getTracks()[0]);
		});

		castVideoFile(demoSharing2).then(stream => {
			//setDemoSharing2Track(stream.getTracks()[0]);
		});
	}
	
	return <>
		<button type="button" onClick={() => loadTracks()} style={{'position':'absolute','z-index':1000}}>DEMO</button>
		<HuddleChatUI {...props} huddleClient={huddleClient} users={examples} />
	</>;
}

export default function HuddleChat(props) {
	
	if(DEMO_MODE){
		return HuddleChatDemo(props);
	}
	
	const [users, setUsers] = useState(null);
	
	var [huddleClient, setHuddleClient] = useState(() => {
		var client = new HuddleClient({
			slug: props.slug, // originate from URL
			serviceHost: props.serviceHost,
			host: props.host,
			isHttp: props.isHttp,
			avatarRef: props.avatarRef,
			displayName: props.displayName
		});
		
		// Add event listeners here
		client.addEventListener('userchange', (e) => {
			// e.users is the list of all huddlePresence objects in this meeting
			setUsers([...e.users]);
		});
		
		client.addEventListener('status', e => {
			if(!e.connected){
				setUsers(null);
			}
		});
		
		client.start();
		return client;
	});
	
	if(!users){
		// Note: initial removed IDs is set based on the first array of users given.
		// So, it's important that we don't give it an empty array until we are loaded.
		return <Container>
			<Row>
				<Col size={12}>
					<Loading message={`Connecting`} />
				</Col>
			</Row>
		</Container>;
	}
	
	return <>
		<HuddleChatUI {...props} huddleClient={huddleClient} users={users} />
	</>;
}

function HuddleChatUI(props) {
	const huddleRef = useRef(null);
	
	var { users, huddleClient, disableChat } = props;
	
	const [sidebar, setSidebar] = useState(SidebarEnum.AUDIENCE);	// sidebar mode
	const [removedUserIds, setRemovedUserIds] = useState(() => {
		var map = new Map();
		
		// Setup initial removed user IDs
		users.forEach(u => {
			if(u.gone){
				map.set(u.id, true);
			}
		});
		
		return map;
	}); // add IDs of test users marked as "gone" to test filtering out a previously removed user
	
	var clearGoneUsers = () => {
		var usersToRemove = new Map();
		
		props.users.forEach(user => {
			if(user.gone){
				usersToRemove.set(user.id, true);
			}
		});
		
		// Only trigger a redraw if it is actually different:
		var different = false;
		
		if(removedUserIds.size != usersToRemove.size){
			different = true;
		}else if(usersToRemove.size != 0){
			
			usersToRemove.forEach((v, k) => {
				if(!removedUserIds.has(k)){
					different = true;
				}
			});
			
			removedUserIds.forEach((v, k) => {
				if(!usersToRemove.has(k)){
					different = true;
				}
			});
		}
		
		if(different){
			setRemovedUserIds(usersToRemove);
		}
	};
	
	useEffect(() => {
		var html = document.querySelector("html");

		// temporarily disable vertical scrolling
		html.classList.add("disable-scroll");

		// remove padding for fixed header
		html.classList.add("disable-header-padding");

		// initialise timer to remove users marked as 'gone' in batches
		var removeTimer = setInterval(clearGoneUsers, REMOVE_USER_INTERVAL);

		return () => {
			html.classList.remove("disable-header-padding");
			html.classList.remove("disable-scroll");
			clearInterval(removeTimer);
		};
	}, [removedUserIds, props.users]);

	var huddleClasses = ["huddle-chat"];

	switch (sidebar) {
		case SidebarEnum.AUDIENCE:
			huddleClasses.push("huddle-chat--audience");
			break;

		case SidebarEnum.CONVERSATION:
			huddleClasses.push("huddle-chat--conversation");
			break;
	}

	huddleClasses = huddleClasses.join(" ");
	
	// filter any users marked as "gone" which already got nuked
	users = users.filter(user => !(user.gone && removedUserIds.has(user.id)));
	
	// get everyone on the stage (main players and pinned overflow)
	var allActors = users.filter(user => user.isOnStage);
	
	allActors.sort(function (a, b) {
		return a.stageSlotId - b.stageSlotId;
	});

	var mainStageActors = allActors.slice(0, MAX_STAGE_USERS);
	var pinnedStageActors = allActors.slice(mainStageActors.length, MAX_STAGE_USERS + MAX_PINNED_USERS);
	
	// check for users sharing
	var sharingActors = users.filter(user => user.isSharing);
	
	sharingActors.sort(function (a, b) {
		return a.stageSlotId - b.stageSlotId;
	});
	
	if (sharingActors.length) {
		// limit number of sharing users on stage
		mainStageActors = sharingActors.slice(0, MAX_SHARING_USERS);

		// update pinned collection
		// (first exclude sharing users from the "on stage" list)
		pinnedStageActors = users.filter(user => user.isOnStage);
		pinnedStageActors = pinnedStageActors.filter(user => !(sharingActors.some(user2 => user2.userId === user.userId)))

		// limit number of users in pinned area
		pinnedStageActors = pinnedStageActors.slice(0, MAX_STAGE_USERS + MAX_PINNED_USERS);

		setSidebar(SidebarEnum.CLOSED);
	}
	
	return <section className={huddleClasses} ref={huddleRef}>
		<Header title={"HaaS v2 Demo"} description={"Work in progress"} disableChat={disableChat}
			showingAudience={sidebar == SidebarEnum.AUDIENCE}
			showingConversation={sidebar == SidebarEnum.CONVERSATION}
			toggleAudience={() => {
				setSidebar(sidebar == SidebarEnum.AUDIENCE ? SidebarEnum.CLOSED : SidebarEnum.AUDIENCE);
			}}
			toggleConversation={() => {
				setSidebar(sidebar == SidebarEnum.CONVERSATION ? SidebarEnum.CLOSED : SidebarEnum.CONVERSATION);
			}}
		/>

		{/* stage / pinned */}
		<div className="huddle-chat__main">
			<StageView users={mainStageActors} isDemo={DEMO_MODE} />
			{pinnedStageActors && pinnedStageActors.length > 0 && <>
				<PinnedView users={pinnedStageActors} isDemo={DEMO_MODE} />
			</>}
		</div>

		{/* audience */}
		{sidebar == SidebarEnum.AUDIENCE && <>
			<AudienceView users={users} pageSize={MAX_AUDIENCE_PER_PAGE} isDemo={DEMO_MODE} />
		</>}

		{/* conversation */}
		{!disableChat && sidebar == SidebarEnum.CONVERSATION && <aside className="huddle-chat__sidebar huddle-chat__sidebar--conversation">
			<header class="huddle-chat__sidebar-header">
				<h2 class="huddle-chat__sidebar-heading">
					Conversation
				</h2>
			</header>
			<div className="huddle-chat__sidebar-body">
				<ChatLive className="huddle-chat__sidebar-body-internal" />
			</div>
		</aside>}

		{/* main huddle footer (share / leave huddle / audio/video options) */}
		<Options audioOn={huddleClient.isActive('microphone')}
				shareOn={huddleClient.isActive('screenshare')}
				videoOn={huddleClient.isActive('webcam')}
			setAudio={state => huddleClient.microphone(state)}
			setVideo={state => huddleClient.webcam(state)}
			setShare={state => huddleClient.screenshare(state)}
		/>

	</section>;
}