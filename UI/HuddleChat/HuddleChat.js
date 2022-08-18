import StageView from './StageView';
import PinnedView from './PinnedView';
import AudienceView from './AudienceView';
import AvTest from './AvTest';
import Header from './Header';
import Notifications from './Notifications';
import Status from './Status';
import Footer from './Footer';
import { useState, useEffect, useRef } from 'react';
import ChatLive from 'UI/ChatLive';
import HuddleClient from 'UI/HuddleClient';
import Container from 'UI/Container';
import Row from 'UI/Row';
import Col from 'UI/Column';
import Loading from 'UI/Loading';
import SpeakerTest from 'UI/HuddleChat/SpeakerTest';
import CustomChat from 'UI/HuddleChat/CustomChat';
import Alert from 'UI/Alert';
import CloseButton from 'UI/CloseButton';

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

export default function HuddleChat(props) {
	const [huddleReady, setHuddleReady] = useState(false);
	const [joined, setJoined] = useState(false);
	const [displayName, setDisplayName] = useState(props.displayName);
	const [deviceHints, setDeviceHints] = useState({});
	const [users, setUsers] = useState(null);
	const [permanentFailure, setPermanentFailure] = useState(null);
	const [userRole, setUserRole] = useState(3);
	const [leaveMode, setLeaveMode] = useState(0); // 0 = ongoing, 1 = left, 2 = ended (remote requested), 3 = ended (requested by this participant)
	const [playbackInfo, setPlaybackInfo] = useState(null);
	const [endTimer, setEndTimer] = useState(null);
	
	var onLeave = mode => {
		huddleClient.destroy(mode);
		setLeaveMode(mode);
		setHuddleClient(null);
		
		// Handling an automatic redirect:
		if(props.endAutoRedirectSeconds && !endTimer){
			
			var timer = setTimeout(() => {
				window.location = props.backUrl || '/';
			}, props.endAutoRedirectSeconds * 1000);
			
			setEndTimer(timer);
		}
		
	};
	
	var [huddleClient, setHuddleClient] = useState(() => {
		var client = new HuddleClient({
			slug: props.slug, // originate from URL
			serviceHost: props.serviceHost,
			host: props.host,
			isHttp: props.isHttp,
			avatarRef: props.avatarRef || props.avatarUrl,
			roleKey: props.roleKey,
			displayName: props.displayName,
			deviceIdAudio: props.deviceIdAudio, // Can be used if avTest is skipped too
			maxBitrateK: props.maxBitrateK,
			deviceIdVideo: props.deviceIdVideo,
			autoStartAudio: props.autoStartAudio, // Used if avTest is skipped
			autoStartVideo: props.autoStartVideo,
			audioInitiallyDisabled: props.audioInitiallyDisabled,
			videoInitiallyDisabled: props.videoInitiallyDisabled,
			onError: e => {
				console.log(e);
				
				// failures here (such as huddle not found)
				if(e.severity == 'fatal'){
					setPermanentFailure(e);
					huddleClient && huddleClient.destroy(0);
					setHuddleClient(null);
				}else if(e.severity == 'minor'){
					console.log("Minor", e);
				}else{
					// Warning (severity == 'warn')
					console.warn(e);
				}
				
			},
			onLeave: onLeave,
			onJoined: client => {
				setUserRole(client.selfRole());
			},
			onLoaded: () => {
				// Called when the huddle info is loaded, but we have not connected media delivery yet.
				setUsers([...client.users]);
				
				// Might immediately jump into playback. Check for that now:
				setPlaybackInfo(client.getPlaybackInfo());
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

	useEffect(() => {
		
		return () => {
			// Called when this component unmounts.
			huddleClient && huddleClient.destroy();
		};
		
	}, []);
	
	useEffect(() => {
		
		// Hide custom node in its current parent:
		if(props.customChatRoot){
			props.customChatRoot.style.display='none';
		}
		
	}, []);

	if (permanentFailure){
		
		return <div className="huddle-chat--not-connected">
			<Container>
				<Row>
					<Col size={12}>
						<Alert variant="danger">
							{permanentFailure.message}
						</Alert>
					</Col>
				</Row>
			</Container>
		</div>;
	}
	
	if (!users){
		// Note: initial removed IDs is set based on the first array of users given.
		// So, it's important that we don't give it an empty array until we are loaded.
		return <div className="huddle-chat--not-connected">
			<Container>
				<Row>
					<Col size={12}>
						<Loading message={`Connecting`} />
					</Col>
				</Row>
			</Container>
		</div>;
	}
	
	if (leaveMode){
		return <div className="huddle-chat--not-connected">
			<Container>
				<Row>
					<Col size={12}>
						<Alert variant="info">
							{leaveMode == 1 && <>
								{`You've left the meeting.`}
							</>}
							{leaveMode == 2 && <>
								{`This meeting has now ended.`}
							</>}
							{leaveMode == 3 && <>
								{`You have ended this meeting.`}
							</>}
						</Alert>
						<footer>
							<a className="btn btn-primary" href={props.backUrl || '/'}>{props.backText || `Go back`}</a>
						</footer>
					</Col>
				</Row>
			</Container>
		</div>;
	}
	
	if(!joined && !props.skipAvTest && !(huddleClient.huddle && huddleClient.huddle.playback)){
		// Click farming UI. This is for 2 things: so the user can check their mic/ cam, 
		// and also so we can farm the click in order to avoid autoplay blocks.
		return <div className="huddle-lobby">
			<AvTest onDeviceSelect={(newHints) => {
				var hints = { ...deviceHints, ...newHints };
				setDeviceHints(hints);
			}} huddleReadyCallback={setHuddleReady} 
			displayName={displayName}
			onChangeName={newName => setDisplayName(newName)}/>

			{huddleReady && <>
				<footer className="huddle-lobby__footer">
					<SpeakerTest />

					<button className="btn btn-primary" onClick={() => {
						huddleClient.startMedia(deviceHints);
						setJoined(1);
					}}>
						<i className="fas fa-fw fa-sign-in"></i> {`Join meeting`}
					</button>
				</footer>
			</>}
		</div>;
	}

	return <>
		<HuddleChatUI {...props} userRole={userRole} huddleClient={huddleClient} users={users} onLeave={onLeave} playbackInfo={playbackInfo} />
	</>;
}

function HuddleChatUI(props) {
	const huddleRef = useRef(null);
	
	var { users, huddleClient, disableChat, disableAudience, disableReactions, disableOptions, title, description } = props;

	title = title || `Meet Now`;
	description = description || `Beta`;

	const [notifications, setNotifications] = useState([]);
	// uncomment for example notifications
	/*
	const [notifications, setNotifications] = useState([
			`Failed to screenshare - please ensure browser permissions have been set.`,
			<>
				<p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Mauris non neque neque.
					Praesent at facilisis nisl. Phasellus vestibulum ultricies nibh, non tempus mi commodo vitae.
					Fusce tincidunt orci vitae risus eleifend, a pretium nibh consequat. Fusce auctor pretium massa,
					vel tincidunt risus placerat vulputate. Proin congue odio non velit fermentum tempus.
					Suspendisse ac dolor a elit molestie volutpat. Nulla pharetra iaculis gravida.
					Sed quis ex et erat pulvinar dignissim. Aenean et eros eu odio <a href="#">congue commodo</a> et non nisl.
					Vestibulum ac justo et velit feugiat condimentum rutrum vel arcu. Maecenas elit tortor,
					suscipit sit amet fringilla ut, vestibulum vel magna. Morbi nisi nulla,
					ullamcorper ac aliquet vitae, scelerisque sit amet elit.</p>

				<p>Proin scelerisque lectus vel turpis vestibulum sollicitudin. Nulla aliquet, arcu eu volutpat facilisis,
					nunc sapien porttitor purus, ac luctus nunc justo quis ipsum. Ut at est mattis, pulvinar enim in, vehicula mi.
					Donec blandit dictum velit, non ullamcorper ex vulputate sed. Nullam vulputate mi nec massa molestie maximus.
					Pellentesque laoreet quis sem sit amet molestie. Curabitur sodales ex vulputate, mollis nibh nec, commodo eros.
					Mauris tellus nibh, euismod at luctus feugiat, lobortis nec neque.</p>
			</>,
			<>
				<h2>{`Camera not found`}</h2>
				<p>
					{`Unable to find an available camera - please check your hardware is connected and not currently in use by another application.`}
				</p>
			</>
		]);
	*/

	const [showingNotifications, setShowingNotifications] = useState(false);
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

	// filter any users marked as "gone" which already got nuked
	users = users.filter(user => !(user.gone && removedUserIds.has(user.id)));

	var emptyHuddle = !users || users.length <= 1;
	const [statusMessage, setStatusMessage] = useState(emptyHuddle ? `Waiting for others to join ...` : null);
	const [showingStatus, setShowingStatus] = useState(emptyHuddle);
	const [sidebar, setSidebar] = useState(disableAudience || emptyHuddle ? SidebarEnum.CLOSED : SidebarEnum.AUDIENCE);	// sidebar mode

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

		//setSidebar(SidebarEnum.CLOSED);
	}

	function toggleNotifications() {
		setShowingNotifications(showingNotifications ? false : true)
	}

	function removeNotification(targetIndex) {

		if (!notifications || notifications.length - 1 < targetIndex) {
			return;
		}

		setNotifications(
			notifications.filter((notification, index) => {
				return index != targetIndex;
			})
		);

		if (notifications.length == 1) {
			setShowingNotifications(false);
        }

	}
	
	function toggleStatus() {
		setShowingStatus(showingStatus ? false : true)
	}

	function updateStatus(newStatus) {

		if (newStatus == statusMessage) {
			return;
		}

		setStatusMessage(newStatus);
		setShowingStatus(true);
    }

	return <section className={huddleClasses} ref={huddleRef}>
		<Header huddleClient={huddleClient} title={title} description={description} users={users}
			disableChat={disableChat} disableAudience={disableAudience}
			disableReactions={disableReactions} disableOptions={disableOptions}
			notifications={notifications} showingNotifications={showingNotifications}
			toggleNotifications={toggleNotifications}
			showingAudience={sidebar == SidebarEnum.AUDIENCE}
			showingConversation={sidebar == SidebarEnum.CONVERSATION}
			toggleAudience={() => {
				setSidebar(sidebar == SidebarEnum.AUDIENCE ? SidebarEnum.CLOSED : SidebarEnum.AUDIENCE);
			}}
			toggleConversation={() => {
				setSidebar(sidebar == SidebarEnum.CONVERSATION ? SidebarEnum.CLOSED : SidebarEnum.CONVERSATION);
			}}
			recordMode={huddleClient.huddle.recording}
		/>

		{/* notifications */}
		<Notifications notifications={notifications} showingNotifications={showingNotifications}
			toggleNotifications={toggleNotifications} removeNotification={(i) => removeNotification(i)}
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
			<AudienceView users={users} pageSize={MAX_AUDIENCE_PER_PAGE} toggleAudience={() => {
				setSidebar(sidebar == SidebarEnum.AUDIENCE ? SidebarEnum.CLOSED : SidebarEnum.AUDIENCE);
			}} />
		</>}

		{/* conversation */}
		{!disableChat && sidebar == SidebarEnum.CONVERSATION && <aside className="huddle-chat__sidebar huddle-chat__sidebar--conversation">
			<header class="huddle-chat__sidebar-header">
				<h2 class="huddle-chat__sidebar-heading">
					{`Conversation`}
				</h2>
				<CloseButton isSmall callback={() => {
					setSidebar(sidebar == SidebarEnum.CONVERSATION ? SidebarEnum.CLOSED : SidebarEnum.CONVERSATION);
				}} />
			</header>
			<div className="huddle-chat__sidebar-body">
				{props.customChatRoot || props.customChatUrl ? <CustomChat root={props.customChatRoot} url={props.customChatUrl} /> :
				<ChatLive className="huddle-chat__sidebar-body-internal" />}
			</div>
		</aside>}

		{/* status */}
		<Status message={statusMessage} showingStatus={showingStatus} toggleStatus={toggleStatus} />

		{/* main huddle footer (share / record / leave huddle / audio/video options) */}
		<Footer huddleClient={huddleClient}
				playbackInfo={props.playbackInfo}
			startPlayback={() => huddleClient.startMedia()}
			stopPlayback={() => huddleClient.stopMedia()}
		        audioOn={huddleClient.isActive('microphone')}
				shareOn={huddleClient.isActive('screenshare')}
				videoOn={huddleClient.isActive('webcam')}
			setAudio={state => huddleClient.microphone(state)}
			setVideo={state => huddleClient.webcam(state)}
			setShare={(state, cancelled) => huddleClient.screenshare(state, cancelled)}
			isHost={props.userRole == 1}
			onLeave={mode => {
				props.onLeave(mode);
			}}
			recordMode={huddleClient.huddle.recording}
			onRecordMode={targetMode => {
				huddleClient.recordingState(targetMode);
				var msg = targetMode ? `This meeting is now being recorded` : `Meeting recording has ended`;
				updateStatus(msg);
			}}
		/>

	</section>;
}
