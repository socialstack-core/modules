import StageView from './StageView';
import PinnedView from './PinnedView';
import AudienceView from './AudienceView';
import AvTest from './AvTest';
import Header from './Header';
import Options from './Options';
import { useState, useEffect, useRef } from 'react';
import ChatLive from 'UI/ChatLive';
import HuddleClient from 'UI/HuddleClient';
import Container from 'UI/Container';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Loading from 'UI/Loading';
import SpeakerTest from 'UI/HuddleChat/SpeakerTest';

const MAX_STAGE_USERS = 6;
const MAX_PINNED_USERS = 10;
const MAX_SHARING_USERS = 2;
const MAX_AUDIENCE_PER_PAGE = 12;
const REMOVE_USER_INTERVAL = 5 * 1000;

const SidebarEnum = Object.freeze({
	CLOSED: 0,
	AUDIENCE: 1,
	CONVERSATION: 2
});

export default function HuddleChat(props){
	const [huddleReady, setHuddleReady] = useState(false);
	const [joined, setJoined] = useState(false);
	const [deviceHints, setDeviceHints] = useState({});

	if(!joined){
		// Click farming UI. This is for 2 things: so the user can check their mic/ cam, 
		// and also so we can farm the click in order to avoid autoplay blocks.
		return <div className="huddle-lobby">
			<AvTest onDeviceSelect={(newHints) => {
				var hints = { ...deviceHints, ...newHints };
				setDeviceHints(hints);
			}} huddleReadyCallback={setHuddleReady} />

			{huddleReady && <>
				<footer className="huddle-lobby__footer">
					<SpeakerTest />

					<button className="btn btn-primary" onClick={() => {
						setJoined(1);
					}}>
						<i className="fas fa-fw fa-sign-in"></i> {`Join meeting`}
					</button>
				</footer>
			</>}
		</div>;
	}
	
	return <HuddleChatClient {...props} {...deviceHints}/>;
}

function HuddleChatClient(props) {
	const [users, setUsers] = useState(null);
	const [failure, setFailure] = useState(null);
	
	var [huddleClient, setHuddleClient] = useState(() => {
		var client = new HuddleClient({
			slug: props.slug, // originate from URL
			serviceHost: props.serviceHost,
			host: props.host,
			isHttp: props.isHttp,
			avatarRef: props.avatarRef,
			displayName: props.displayName,
			deviceIdAudio: props.deviceIdAudio,
			deviceIdVideo: props.deviceIdVideo,
			audioInitiallyDisabled: props.audioInitiallyDisabled,
			videoInitiallyDisabled: props.videoInitiallyDisabled,
			onError: e => {
				// permanent failures here (such as huddle not found)
				setFailure(e);
			}
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
	
	if(failure){
		return <Container>
			<Row>
				<Col size={12}>
					{`This meeting wasn't found.`}
				</Col>
			</Row>
		</Container>;
	}
	
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
	
	var { users, huddleClient, disableChat, disableAudience, title, description } = props;

	title = title || 'Meet Now';
	description = description || 'Beta';
	
	const [sidebar, setSidebar] = useState(disableAudience ? SidebarEnum.CLOSED : SidebarEnum.AUDIENCE);	// sidebar mode
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

		// add a marker class so other page elements can be aware UI is active
		html.classList.add("huddle-ui--active");

		// initialise timer to remove users marked as 'gone' in batches
		var removeTimer = setInterval(clearGoneUsers, REMOVE_USER_INTERVAL);

		return () => {
			html.classList.remove("huddle-ui--active");
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
		<Header title={title} description={description} disableChat={disableChat} disableAudience={disableAudience}
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
			<StageView users={mainStageActors} />
			{pinnedStageActors && pinnedStageActors.length > 0 && <>
				<PinnedView users={pinnedStageActors} />
			</>}
		</div>

		{/* audience */}
		{!disableAudience && sidebar == SidebarEnum.AUDIENCE && <>
			<AudienceView users={users} pageSize={MAX_AUDIENCE_PER_PAGE} />
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