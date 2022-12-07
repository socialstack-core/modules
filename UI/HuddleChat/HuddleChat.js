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
import HuddleEnums from 'UI/HuddleClient/HuddleEnums';
import store from 'UI/Functions/Store';
import ToastList from 'UI/ToastList';
import { useToast } from 'UI/Functions/Toast';

const MAX_STAGE_USERS = 6;
const MAX_PINNED_USERS = 10;
const MAX_SHARING_USERS = 2;
const MAX_AUDIENCE_PER_PAGE = 10;
const REMOVE_USER_INTERVAL = 5 * 1000;

const SidebarEnum = Object.freeze({
	CLOSED: 0,
	AUDIENCE: 1,
	CONVERSATION: 2
});

export default function HuddleChat(props) {
	const { pop } = useToast();
	const [huddleReady, setHuddleReady] = useState(false);
	const [joined, setJoined] = useState(false);
	const [displayName, setDisplayName] = useState(props.displayName);
	const [deviceHints, setDeviceHints] = useState({});
	const [users, setUsers] = useState([]);
	const [huddleInfo, setHuddleInfo] = useState(null);
	const [permanentFailure, setPermanentFailure] = useState(null);
	const [userRole, setUserRole] = useState(3);
	const [leaveMode, setLeaveMode] = useState(0); // 0 = ongoing, 1 = left, 2 = ended (remote requested), 3 = ended (requested by this participant), 4=forced disconnect
	const [playbackInfo, setPlaybackInfo] = useState(null);
	const [endTimer, setEndTimer] = useState(null);
	const [statusMessage, setStatusMessage] = useState(null);
	const [showingStatus, setShowingStatus] = useState(false);

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
	
	var toggleStatus = () => {
		setShowingStatus(showingStatus ? false : true)
	}

	var updateStatus = (newStatus) => {

		if (newStatus == statusMessage) {
			return;
		}

		setStatusMessage(newStatus);
		setShowingStatus(true);
	}

	var toggleNotifications = () => {
		setShowingNotifications(showingNotifications ? false : true)
	}
	
	var removeNotification = (targetIndex) => {

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
	
	var onLeave = mode => {
		huddleClient.destroy(mode);
		setLeaveMode(mode);
		setHuddleClient(null);

		// Handling an automatic redirect:
		if (props.endAutoRedirectSeconds && !endTimer) {

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
			regionToUrl: props.regionToUrl,
			host: props.host,
			isHttp: props.isHttp,
			roleKey: props.roleKey,
			deviceHints: {
				audioInitiallyDisabled: props.audioInitiallyDisabled,
				videoInitiallyDisabled: props.videoInitiallyDisabled,
				autoStartAudio: props.autoStartAudio, // Used if avTest is skipped
				autoStartVideo: props.autoStartVideo,
				deviceIdVideo: props.deviceIdVideo,
				deviceIdAudio: props.deviceIdAudio, // Can be used if avTest is skipped too
			},
			maxBitrateK: props.maxBitrateK,
			onError: e => {
				console.log(e);
				
				if(e.inResponseTo == 46 && e.severity != 'fatal'){
					// This is a recording error. Display a popup.
					setNotifications(notifications ? [...notifications, e.message] : [e.message]);
					setShowingNotifications(true);
				}
				
				// failures here (such as huddle not found)
				if (e.severity == 'fatal') {
					setPermanentFailure(e);
					huddleClient && huddleClient.destroy(0);
					setHuddleClient(null);
				} else if (e.severity == 'minor') {
					console.log("Minor", e);
				} else {
					// Warning (severity == 'warn')
					console.warn(e);
				}

			},
			onLeave: onLeave,
			onJoined: client => {
				setUserRole(client.selfRole());
			},
			onLoaded: (huddle, client) => {
				// Huddle info has been loaded. It's never null here.
				
				setHuddleInfo(huddle);

				var defaultOpts = store.get("huddleDefaultDevices");
				if(props.skipAvTest || defaultOpts && defaultOpts.defaults) {
					// Skip the AV test and go straight to the media connection.
					client.startMedia(deviceHints);
					setJoined(1);
				}
				
				/*
				if(client.huddle.playback){
					setPlaybackInfo(client.getPlaybackInfo());
				}else{
					setPlaybackInfo(null);
				}
				*/
			}
		});
		
		// Get existing saved name/ avatar:
		var userState = client.getUserState();
		
		if(props.displayName){
			
			// Use display name override:
			client.updateDisplayName(props.displayName);
			
		}else{
			
			// Use display name present in the user state:
			setDisplayName(client.props.displayName);
			
		}
		
		var avatarOverride = props.avatarRef || props.avatarUrl;
		
		if(avatarOverride){
			
			// Use avatar present in the user state (the default) otherwise.
			client.updateAvatarRef(avatarOverride);
			
		}
		
		client.addEventListener('userchange', (e) => {
			// e.users is the list of all huddlePresence objects in this meeting
			setUsers([...e.users]);
		});

		client.addEventListener("userpresence", (e) => {
			// e.user, e.gone (true/false), e.others
			var user = e.user;
			var name = user.displayName;
			var extraPeople = e.others;

			// if the current user just got added, trigger a notification if muted by the host
			if (client.selfId == user.id && user.isHostMuted) {
				var mutedMessage = `Please note, video and audio have been disabled by default and can only be enabled by the host. Screen sharing will become available when you join the main stage.`;
				setNotifications(notifications ? [...notifications, mutedMessage] : [mutedMessage]);
				setShowingNotifications(true);
				return;
			}

			// otherwise inform the user who just left / joined
			var toastMessage = '';

			if (extraPeople) {

				if (e.gone) {
					toastMessage = `${name} +${extraPeople} other(s) left the meeting`;
				} else {
					toastMessage = `${name} +${extraPeople} others joined the meeting`;
				}

			} else {

				if (e.gone) {
					toastMessage = `${name} left the meeting`;
				} else {
					toastMessage = `${name} joined the meeting`;
				}

			}

			pop({
				title: '',
				description: toastMessage,
				duration: 3,
				variant: 'success'
			});

		});
		
		// Add event listeners here
		client.addEventListener('huddlechange', (e) => {
			// e.huddle is the huddle info such as recording state.
			setHuddleInfo(e.huddle);
			
			// If the huddle is being recorded, make sure the status declares it:
			if(e.huddle.playback){
				setPlaybackInfo(client.getPlaybackInfo());
			}else{
				if(playbackInfo){
					setPlaybackInfo(null);
				}
				
				if(e.huddle.recording && !e.previous.recording){
					updateStatus(`This meeting is being recorded`);
				}
			}
		});

		client.addEventListener('status', e => {
			if (!e.connected) {
				setHuddleInfo(null);
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
		if (props.customChatRoot) {
			props.customChatRoot.style.display = 'none';
		}

	}, []);

	if (permanentFailure) {

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

	if (leaveMode) {
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
							{leaveMode == 4 && <>
								{`Uh oh! Well, that threw a spanner in the works. You have been disconnected from the meeting.`}
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

	if (!huddleInfo) {
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

	if (!joined && !props.skipAvTest && !(huddleInfo && huddleInfo.playback)) {
		// Click farming UI. This is for 2 things: so the user can check their mic/ cam, 
		// and also so we can farm the click in order to avoid autoplay blocks.
		return <div className="huddle-lobby">

			<AvTest onDeviceSelect={(newHints) => {
				var hints = { ...deviceHints, ...newHints };
				setDeviceHints(hints);
			}} huddleReadyCallback={setHuddleReady}
				displayName={displayName}
				isDisableDevicesMenu={props.disableDevicesMenu}
				onChangeName={newName => {
					setDisplayName(newName);
					huddleClient.updateDisplayName(newName);
				}} />

			{huddleReady && <>
				<footer className="huddle-lobby__footer">
					
					{ !props.disableDevicesMenu && <SpeakerTest /> }

					<button className="btn btn-primary" onClick={() => {
						console.log("click - join meeting");
						huddleClient.startMedia(deviceHints);
						setJoined(1);
					}} onTouchStart={() => {
						console.log("touch start - join meeting");
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
		<HuddleChatUI {...props} userRole={userRole} huddleClient={huddleClient} 
			huddleInfo={huddleInfo} users={users} onLeave={onLeave} playbackInfo={playbackInfo}
			showingNotifications={showingNotifications}
			removeNotification={removeNotification}
			displayName={displayName}
			notifications={notifications}
			toggleNotifications={toggleNotifications}
			toggleStatus={toggleStatus}
			statusMessage={statusMessage}
			setStatusMessage={setStatusMessage}
			showingStatus={showingStatus}
			setShowingStatus={setShowingStatus}
			setDisplayName={setDisplayName}
		/>
	</>;
}

function HuddleChatUI(props) {

	const huddleRef = useRef(null);	

	var { users, huddleClient, disableChat, disableAudience, disableReactions, disableOptions, title, description, huddleInfo, playbackInfo, displayName, setDisplayName } = props;

	title = title || `Meet Now`;
	description = description || ``;

	const [removedUserIds, setRemovedUserIds] = useState(() => {
		var map = new Map();

		// Setup initial removed user IDs
		users.forEach(u => {
			if (u.gone) {
				map.set(u.id, true);
			}
		});

		return map;
	}); // add IDs of test users marked as "gone" to test filtering out a previously removed user

	var clearGoneUsers = () => {
		var usersToRemove = new Map();

		props.users.forEach(user => {
			if (user.gone) {
				usersToRemove.set(user.id, true);
			}
		});

		// Only trigger a redraw if it is actually different:
		var different = false;

		if (removedUserIds.size != usersToRemove.size) {
			different = true;
		} else if (usersToRemove.size != 0) {

			usersToRemove.forEach((v, k) => {
				if (!removedUserIds.has(k)) {
					different = true;
				}
			});

			removedUserIds.forEach((v, k) => {
				if (!usersToRemove.has(k)) {
					different = true;
				}
			});
		}

		if (different) {
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

	// Establish if the huddle is empty.
	var emptyHuddle = true;
	users.forEach(u => {
		if (u.isOnStage) {
			// Someone is on the stage. The huddle is not empty if it's not "this" user OR it's a hosted meeting.
			if (huddleClient.selfId != u.id || huddleInfo.huddleType != 0) {
				// Otherwise yes it's myself. It's not empty if I'm a host though.
				emptyHuddle = false;
			}
		}
	});

	var otherAttendees = users.filter(user => user.id !== huddleClient.selfId);
	var isHosted = huddleInfo.huddleType != 0;
	var hostArrived = users.filter(user => !user.gone && HuddleEnums.isHost(user.role)).length > 0;
	
	const [sidebar, setSidebar] = useState(SidebarEnum.CLOSED);	// sidebar mode. Can only switch to audience when connected.
	const [showDebugInfo, setShowDebugInfo] = useState(false);

	React.useEffect(() => {
		
		if (emptyHuddle) {
			props.setStatusMessage(props.waitingMessage || (!isHosted ? `Waiting for others to join ...` : `Waiting for host ...`));
			props.setShowingStatus(true);
		} else {
			props.setStatusMessage(null);
			props.setShowingStatus(false);
		}

	}, [emptyHuddle]);

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

	function isHost() {
		return huddleClient.selfRole() != HuddleEnums.Role.GUEST;
	}
	
	return <section className={huddleClasses} ref={huddleRef}>
		<Header huddleClient={huddleClient} title={title} description={description} users={users} isHost={isHost()}
			disableChat={playbackInfo ? true : disableChat} disableAudience={playbackInfo ? true : disableAudience}
			disableReactions={playbackInfo ? true : disableReactions} disableOptions={playbackInfo ? true : disableOptions}
			notifications={props.notifications} showingNotifications={props.showingNotifications}
			displayName={displayName}
			setDisplayName={(newName) => {
				setDisplayName(newName);
				huddleClient.updateDisplayName(newName);
			}}
			toggleNotifications={props.toggleNotifications}
			showingAudience={sidebar == SidebarEnum.AUDIENCE}
			showingConversation={sidebar == SidebarEnum.CONVERSATION}
			toggleAudience={() => {

				// If connected, send request to hide/ show audience.
				if (huddleClient.toggleAudience(sidebar != SidebarEnum.AUDIENCE)) {
					setSidebar(sidebar == SidebarEnum.AUDIENCE ? SidebarEnum.CLOSED : SidebarEnum.AUDIENCE);
				}

			}}
			toggleConversation={() => {
				setSidebar(sidebar == SidebarEnum.CONVERSATION ? SidebarEnum.CLOSED : SidebarEnum.CONVERSATION);
			}}
			showDebugInfo={showDebugInfo} toggleShowDebugInfo={(shown) => {
				setShowDebugInfo(shown);
            }}
			recordMode={huddleInfo.recording}
		/>

		{/* notifications */}
		<Notifications notifications={props.notifications} showingNotifications={props.showingNotifications}
			toggleNotifications={props.toggleNotifications} removeNotification={(i) => props.removeNotification(i)}
		/>

		{/* stage / pinned */}
		<div className="huddle-chat__main">
			<StageView users={mainStageActors} huddleClient={huddleClient} showDebugInfo={showDebugInfo}
				isHosted={isHosted} hostArrived={hostArrived} emptyHuddle={emptyHuddle} playbackInfo={playbackInfo}/>

			{pinnedStageActors && pinnedStageActors.length > 0 && <>
				<PinnedView users={pinnedStageActors} huddleClient={huddleClient} showDebugInfo={showDebugInfo} />
			</>}

			{<ToastList horizontal="Right" vertical="Bottom" />}
		</div>

		{/* audience */}
		{!disableAudience && sidebar == SidebarEnum.AUDIENCE && <>
			<AudienceView users={users} pageSize={MAX_AUDIENCE_PER_PAGE} huddleClient={huddleClient} showDebugInfo={showDebugInfo} toggleAudience={() => {
				if (huddleClient.toggleAudience(sidebar != SidebarEnum.AUDIENCE)) {
					setSidebar(sidebar == SidebarEnum.AUDIENCE ? SidebarEnum.CLOSED : SidebarEnum.AUDIENCE);
				}
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
					<ChatLive className="huddle-chat__sidebar-body-internal" pageId={huddleInfo.id}/>}
			</div>
		</aside>}

		{/* status */}
		{!playbackInfo && <Status message={props.statusMessage} showingStatus={props.showingStatus} toggleStatus={props.toggleStatus} />}

		{/* main huddle footer (share / record / leave huddle / audio/video options) */}
		<Footer huddleClient={huddleClient}
			playbackInfo={playbackInfo} 
			disableRecording={props.disableRecording}
			allowRecording={props.allowRecording}
			startPlayback={() => huddleClient.startMedia()}
			stopPlayback={() => huddleClient.stopMedia()}
			audioOn={huddleClient.isActive('microphone')}
			blockedChannels={huddleClient.getBlockedChannels()}
			shareOn={huddleClient.isActive('screenshare')}
			videoOn={huddleClient.isActive('webcam')}
			setAudio={state => huddleClient.microphone(state)}
			setVideo={state => huddleClient.webcam(state)}
			setShare={(state, cancelled) => huddleClient.screenshare(state, cancelled)}
			isHost={isHost()}
			onLeave={mode => {
				props.onLeave(mode);
			}}
			recordMode={huddleInfo.recording}
			onRecordMode={targetMode => {
				// Ask huddle client to set the recording state:
				huddleClient.recordingState(targetMode);
			}}
		/>

	</section>;
}
