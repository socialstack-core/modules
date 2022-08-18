import webRequest, {expandIncludes} from 'UI/Functions/WebRequest';
import WebSocket from 'UI/Functions/WebSocket';
import store from 'UI/Functions/Store';
import getRef from 'UI/Functions/GetRef';
import errorMessages from './errors.js';


var _huddleUserState = null;

function saveUserState(){
	store.set('huddle', JSON.stringify(_huddleUserState));
}

function loadUserState(){
	_huddleUserState = {};
	var json = store.get('huddle');
	if(!json){
		return;
	}
	
	try{
		var state = JSON.parse(json);
		
		if(state.displayName || state.avatarRef || state.token){
			_huddleUserState = state;
		}
	}catch(e){
		console.log(e);
	}
}

export default class HuddleClient{
	
	constructor(props) {
		this.props = props;
		this.eventHandlers = {};
		this.sendStream = new MediaStream();
	}
	
	initSocket(host, isHttp) {
		
		if(!_huddleUserState){
			loadUserState();
		}
		
		var changedUserState = false;
		
		if(this.props.displayName){
			if(this.props.displayName != _huddleUserState.displayName){
				_huddleUserState.displayName = this.props.displayName;
				changedUserState = true;
			}
		}
		
		if(this.props.avatarRef){
			if(this.props.avatarRef != _huddleUserState.avatarRef){
				_huddleUserState.avatarRef = this.props.avatarRef;
				changedUserState = true;
			}
		}
		
		if(changedUserState){
			saveUserState();
		}
		
		var wsPath = this.props.socketPath;
		
		if(!wsPath){
			if(host.startsWith('localhost')){
				wsPath = '';
			}else{
				wsPath = '/live-websocket/';
			}
		}
		
		this.socket = WebSocket.create({
			reconnectOnUserChange: false,
			addDefaults: true,
			url: (host.startsWith('localhost') ? 'ws://localhost:6051' : (isHttp ? 'ws' : 'wss') + '://' + host) + wsPath
		});
		
		this.setupPeer();
		
		/*
		global.navigator.mediaDevices.getUserMedia({ audio: true }).then((stream) => {
			var audioTrack = stream.getAudioTracks()[0];
			audioTrack.enabled = false;
			this._permAudioTrack = audioTrack;
			setTimeout(() => audioTrack.stop(), 120000);
		});
		*/
		
		/*
		setTimeout(() => {
			this.webcam(true)
		}, 5000);
		*/
		
		this.socket.addEventListener ('huddlepresence', (message) => {
			
			if(message.all){
				if(message.type == 'status'){
					// Did we just DC?
					if(!message.connected){
						// We've disconnected. The WS will be trying to reconnect itself.
						this.joined = false;
						this.closed();
					}else{
						// We've reconnected or connected for the first time. Make sure peer is going and we send our join request.
						this.setupPeer();
						this.sendSocketJoin();
					}
				}
				
				return;
			}
			
			var u = message.entity;
			
			if(!u){
				return;
			}
			
			var index = -1;
			for(var i=0;i<this.users.length;i++){
				if(this.users[i].id == u.id){
					index = i;
					break;
				}
			}
			
			if(index == -1){
				// Add, assuming method is create.
				if(message.method == 'update' || message.method == 'create'){
					this.users.push(u);
				}
				
				this.updatePresence(u);
				return;
			}
			
			if(message.method == 'delete'){
				// Mark as gone instead
				var existing = this.users[index];
				if(existing){
					existing.gone = true;
					existing.channels = 0;
					this.users[index] = u = {...existing};
				}
			}else{
				// Update mainly
				var existing = this.users[index];
				
				// Retain tracks etc:
				this.users[index] = u = {...existing, ...u};
			}
			
			this.updatePresence(u);
			
			if(u.id != this.selfId){
				this.updateAnswer();
			}else{
				var e = this.createEvent('userchange');
				e.users = this.users;
				this.dispatchEvent(e);
			}
		}, 0, null, false);
		
		this.socket.registerOpcode(60, r => {
			var payloadSize = r.readUInt32();
			
			// Huddle updated:
			var json = r.readUtf8SizedPlus1(payloadSize + 1);
			
			try{
				var huddleState = JSON.parse(json);
				
				if(!this.huddle){
					this.huddle = huddleState;
				}else{
					this.huddle = {
						...this.huddle,
						huddleState
					};
				}
				
				console.log("Huddle change: ", this.huddle);
				
				var e = this.createEvent('userchange');
				e.users = this.users;
				this.dispatchEvent(e);
				
			}catch(e){
				console.log(e);
			}
			
		}, false);
		
		this.socket.registerOpcode(52, r => {
			var payloadSize = r.readUInt32();
			
			// Stage state:
			var json = r.readUtf8SizedPlus1(payloadSize + 1);
			
			try{
				var stageState = JSON.parse(json);
				
				// Compare and see if anyone left/ joined.
				var existing = {};
				
				if(this.users){
					this.users.forEach(u => {
						existing[u.id] = u;
					});
				}
				
				if(stageState && stageState.length){
					
					var change = false;
					
					stageState.forEach((state, i) => {
						
						var existingUser = existing[state.presenceId];
						
						if(existingUser){
							
							// Possibly update gone etc.
							if(state.gone != existingUser.gone){
								existingUser.gone = state.gone;
							}
							
							if(state.channels != existingUser.channels){
								existingUser.channels = state.channels;
							}
							
							change = true;
							
						}else{
							
							// Add:
							var u = {
								gone: state.gone == 1,
								id: state.presenceId,
								channels: state.channels,
								stageSlotId: i+1,
								userId: state.userId,
								creatorUser: {
									id: state.userId,
									username: "name todo"
								}
							};
							
							this.updatePresence(u);
							this.users.push(u);
							change = true;
						}
						
					});
					
					if(!this.joined){
						// First one - trigger the ready state:

						this.props.onLoaded && this.props.onLoaded(this);
					}else if(change){
						this.updateAnswer();
					}
				}
				
			}catch(e){
				console.log(e);
			}
			
		}, false);
		
		this.socket.registerOpcode(49, r => {
			var payloadSize = r.readUInt32();
			console.log('Huddle ending - goodbye!');
			this.props.onLeave ? this.props.onLeave(2) : this.destroy();
		}, false);
		
		this.socket.registerOpcode(2, r => {
			var payloadSize = r.readUInt32();
			var severity = r.readByte();
			var inResponseTo = r.readUInt32();
			var eJson = r.readUtf8(); // Error json
			
			var errorObj = {
				error: 'unspecified'
			};
			
			try{
				errorObj = JSON.parse(eJson);
			}catch(e){
				console.log(e);
			}
			
			if(errorObj.error){
				var fullText = errorMessages[errorObj.error];
				
				if(!fullText){
					fullText = errorMessages.unspecified;
				}
				
				errorObj.message = fullText;
			}
			
			switch(severity){
				case 1:
					errorObj.severity = 'fatal';
				break;
				case 2:
					errorObj.severity = 'minor';
				break;
				case 3:
					errorObj.severity = 'warn';
				break;
			}
			
			this.props.onError && this.props.onError(errorObj);
			
		}, false);
		
		this.socket.registerOpcode(45, r => {
			var payloadSize = r.readUInt32();
			var selfPresenceId = r.readUInt32();
			
			this.selfId = selfPresenceId;
			
			// Various strings - the offer then the JSON of current producers.
			var offerHead = r.readUtf8();
			var candidate = r.readUtf8();
			var userToken = r.readUtf8(); // Huddle network user token
			var extension = r.readUtf8(); // Extension json
			
			if(userToken){
				// Store the updated user state
				_huddleUserState.token = userToken;
				saveUserState();
			}
			
			var huddleInfoSize = r.readUInt32();
			
			var huddleInfo = r.readUtf8SizedPlus1(huddleInfoSize);
			
			try {
				var huddle = JSON.parse(huddleInfo);
				huddle = expandIncludes(huddle);
				this.huddle = huddle;
				
				console.log(huddle, huddle.huddleType);
			}catch(e){
				console.log(e);
				return;
			}
			
			if(huddle && huddle.playback){
				this._selfId = this.selfId;
				this.selfId = 1;
			}
			
			var presSize = r.readUInt32();
			
			var presence = r.readUtf8SizedPlus1(presSize);
			
			// The base user set, including "myself":
			if(presence){
				var baseUserSet = JSON.parse(presence);
				
				// Expand includes:
				this.users = expandIncludes(baseUserSet).results;
			}else{
				this.users = [];
			}
			
			var targetBitrateK = this.props.maxBitrateK || '2600';
			
			var audioConfig = "m=audio 7 UDP/TLS/RTP/SAVPF 96\r\n" +
			"c=IN IP4 0.0.0.0\r\n"+
			"a=extmap-allow-mixed\r\n" +
			"a=rtcp:9 IN IP4 0.0.0.0\r\n"+
			"a=rtpmap:96 opus/48000/2\r\n"+
			"a=fmtp:96 stereo=1;usedtx=1"+
			"a=extmap:1 urn:ietf:params:rtp-hdrext:ssrc-audio-level\r\n"+
			// "a=extmap:3 urn:ietf:params:rtp-hdrext:sdes:mid\r\n" +
			"a=rtcp-mux\r\n" +
			"a=rtcp-rsize\r\n";
			
			var videoConfig = "m=video 7 UDP/TLS/RTP/SAVPF 97\r\n"+
			"c=IN IP4 0.0.0.0\r\n"+
			"a=rtcp:9 IN IP4 0.0.0.0\r\n"+
			"a=extmap-allow-mixed\r\n" +
			// "a=rtcp-fb:97 goog-remb\r\n"+
			"a=rtcp-fb:97 ccm fir\r\n"+
			"a=rtcp-fb:97 nack\r\n"+
			"a=rtcp-fb:97 nack pli\r\n"+
			"a=rtpmap:97 H264/90000\r\n"+
			"a=fmtp:97 profile-level-id=42e01f;level-asymmetry-allowed=1;packetization-mode=1;x-google-max-bitrate=" + targetBitrateK + ";x-google-min-bitrate=" + targetBitrateK + ";x-google-start-bitrate=" + targetBitrateK + "\r\n"+
			// "a=extmap:3 urn:ietf:params:rtp-hdrext:sdes:mid\r\n"+
			"a=extmap:4 http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time\r\n"+
			// "a=extmap:5 http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01\r\n"+
			"a=extmap:12 urn:ietf:params:rtp-hdrext:toffset\r\n"+
			"b=AS:" + targetBitrateK + "\r\n" +
			"a=rtcp-mux\r\n" +
			"a=rtcp-rsize\r\n";
			
			var offerCandidate = "a=candidate:udpcandidate 1 udp 1111 " + candidate + " typ host\r\n" + 
			"a=end-of-candidates\r\na=setup:passive\r\n";
			
			// Store offer:
			this.remoteOffer = {
				header: offerHead + '\r\na=msid-semantic: WMS *\r\n',
				audio: audioConfig,
				video: videoConfig,
				candidate: offerCandidate
			};
			
			// It's important that we add the receive channels first as it guarantees that there is at least one in the SDP which causes BUNDLE to break otherwise.
			// Secondly, it means we're in control of the media ID (the mid) which is critical for ensuring the packets demux properly.
			// Third, remote offers this (rather than the browser) as you can't change the mid once the browser has generated one.
			// Note that the tracks will actually have an incorrect direction as the browser knows it's not 
			// actually sending anything and thus sets "recvonly" even though the SDP states otherwise.
			// Fourth, when a local device goes active, it then replaces the sending track in the correct transceiver.
			// This is then offered. At that point the browser knows it is sending something
			// and then updates the direction value and links everything up the way we need.
			// WebRTC itself is unfortunately a mess but hopefully will be improved in the future.
			
			// Add the 3 recv channels:
			for(var k in this.shareState){
				var ssrc = (this.selfId << 2) | this.channelId(k);
				var mid = 'rem' + ssrc;
				
				var mediaText = k == 'microphone' ? this.remoteOffer.audio : this.remoteOffer.video;
				mediaText += this.remoteOffer.candidate;
				mediaText += 'a=recvonly\r\na=mid:' + mid + '\r\n';
				
				this.offerSet.offers.push({
					block: mediaText,
					mid
				});
			}
			
			this.users.forEach(u => {
				this.updatePresence(u);
			});
			
			if(!huddle.playback){
				this.props.onLoaded && this.props.onLoaded(this);
			}
		}, false);
	}
	
	// Call this when you're ready to create a media connection
	// deviceHints is effectively any additional start config you want to pass.
	// it typically includes audioInitiallyDisabled, deviceIdAudio, videoInitiallyDisabled, deviceIdVideo
	startMedia(deviceHints){
		
		if(deviceHints){
			for(var k in deviceHints){
				this.props[k] = deviceHints[k];
			}
		}
		
		this.joined = true;
		
		this.checkInitialInputState().then(() => {
			this.updateAnswer();
			this.props.onJoined && this.props.onJoined(this);
		});
	}

	stopMedia() {
		// TODO
    }
	
	selfPresence(){
		if(!this.users){
			return null;
		}
		return this.users.find(u => u.id == this.selfId);
	}
	
	selfRole(){
		var pres = this.selfPresence();
		if(!pres){
			return 3; // guest
		}
		
		return pres.role;
	}
	
	updatePresence(presence){
		presence.isOnStage = presence.stageSlotId != 0;
		presence.isSharing = (presence.channels & 4) == 4;
		presence.isMicrophoneOn = (presence.channels & 1) == 1;
		presence.isWebcamOn = (presence.channels & 2) == 2;
		
		if(presence.channels && !presence.gone && presence.id != this.selfId){
			this.buildOffer(presence);
		}
	}
	
	destroy(mode){
		if(this.peer){
			this.peer.close();
			this.peer = null;
		}
		if(this.socket){
			if(mode && mode == 3){
				// This participant wants to end the meeting - Attempt to end call for others too:
				var writer = new this.socket.Writer(49);
				this.socket.send(writer);
			}
			this.socket.close();
			this.socket = null;
		}
		
		for(var k in this.shareState){
			var currentSender = this.shareState[k].sender;
			if(currentSender){
				if(currentSender.stream._stop){
					currentSender.stream._stop();
				}
				
				currentSender.track.stop();
				this.shareState[k].sender = null;
				this.shareState[k].active = false;
			}
		}
	}
	
	closed(){
		// Destroy and recreate the peer:
		if(this.peer){
			this.peer.close();
			this.peer = null;
		}
		
		var e = this.createEvent('status');
		e.connected = false;
		this.dispatchEvent(e);
	}
	
	createEvent(name){
		var e = document.createEvent('Event');
		e.initEvent(name, true, true);
		return e;
	}
	
	setupPeer(){
		if(this.peer){
			return;
		}
		
		this.sdpVersion = 1; // SDP version must be increased each time we renegotiate
		this.users = []; // All present users
		this.offerSet = {offers: []};
		this.shareState = {
			'microphone': {},
			'screenshare': {},
			'webcam': {},
		};
		
		this.peer = new RTCPeerConnection({
			/* iceServers: [
				{
					urls: "*turn server info*"
				}
			], */
			sdpSemantics: 'unified-plan',
			bundlePolicy: 'max-bundle'
		});
		
		// this.peer.addEventListener('icecandidate', e => {});
		this.peer.addEventListener('connectionstatechange', e => {
			
			// Possibly a disconnect
			
		});
		
		this.peer.addEventListener('iceconnectionstatechange', e => {
			
			// Possibly a disconnect
			
		});
		
		this.peer.addEventListener('icegatheringstatechange', e => {
			
			if(this.peer.iceGatheringState == 'complete'){
				// Trickle ice completed.
				this.checkInitialInputState().then(() => {
					this.syncTransceivers();
				});
			}
			
		});
		
	}
	
	getPlaybackInfo(){
		if(!this.huddle || !this.huddle.playback){
			return null;
		}
		
		var pb = this.huddle.playback;
		
		return {
			isLive: false,
			duration: pb.max * 2 
		};
	}
	
	isPlaybackMode(){
		return (this.huddle && this.huddle.playback);
	}
	
	checkInitialInputState(){
		
		// Must be both joined (websocket) and have a connected peer and not be playback mode.
		if(!this.peer || this.peer.iceGatheringState != 'complete' || !this.joined || this.isPlaybackMode()){
			return Promise.resolve(true);
		}
		
		var proms = [];
		
		if((this.props.autoStartVideo || this.props.deviceIdVideo) && !this.props.videoInitiallyDisabled){
			proms.push(this.webcam(1));
		}
		
		if((this.props.autoStartAudio || this.props.deviceIdAudio) && !this.props.audioInitiallyDisabled){
			proms.push(this.microphone(1));
		}
		
		return Promise.all(proms);
	}
	
	updateChannelState(selfUser){
		// Tell the world our current mic/ cam etc state:
		
		var channels = 0;
		
		if(this.shareState['microphone'].active){
			channels |= 1;
		}
		
		if(this.shareState['webcam'].active){
			channels |= 2;
		}
		
		if(this.shareState['screenshare'].active){
			channels |= 4;
		}
		
		selfUser.channels = channels;
		
		var writer = new this.socket.Writer(47);
		writer.writeUInt16(channels); // Write user share state
		this.socket.send(writer);
		
		var e = this.createEvent('userchange');
		this.users = [...this.users];
		e.users = this.users;
		this.dispatchEvent(e);
	}
	
	userSsrcAudio(id, channels, offerSet){
		if(channels & 1){
			this.userSsrc(id, 0, 0, offerSet);
		}
	}
	
	userSsrcVideo(id, channels, offerSet){
		var res = null;
		if(channels & 2){ // webcam
			this.userSsrc(id, 1, 1, offerSet);
		}
		if(channels & 4){ // screenshare
			this.userSsrc(id, 2, 1, offerSet);
		}
	}
	
	channelName(id){
		switch(id){
			case 0:
				return 'microphone';
			case 1:
				return 'webcam';
			case 2:
				return 'screenshare';
		}
		
		return null;
	}
	
	channelId(name){
		switch(name){
			case 'microphone':
				return 0;
			case 'webcam':
				return 1;
			case 'screenshare':
				return 2;
		}
		
		return null;
	}
	
	makeOffer(){
		if(this.isUpdatingOfferAnswer){
			return this.isUpdatingOfferAnswer.then(() => this.makeOffer());
		}
		
		this.isUpdatingOfferAnswer = new Promise((s, r) => {
			
			// Create the offer now:
			this.peer.createOffer({}) // offerToReceiveVideo: true, offerToReceiveAudio: true})
			.then(offer => {
				
				// Extract the locally generated mid's:
				var sdp = this.mungeLocalDescription(offer.sdp, true);
				
				console.log("(local) offer: ", sdp);
				
				return this.peer.setLocalDescription({
					type: 'offer',
					sdp
				});
			})
			.then(() => {
				
				// Construct the remote SDP:
				var sdp = this.createRemoteSdp();
				
				console.log("(remote) answer: ", sdp);
				
				// Apply it as an answer:
				return this.peer.setRemoteDescription({
					type: 'answer',
					sdp
				})
			})
			.then(() => {
				this.syncTransceivers();
			})
			.then(() => {
				this.isUpdatingOfferAnswer = null;
				return s();
			})
			.catch(r);
			
		});
		
		return this.isUpdatingOfferAnswer;
	}
	
	userSsrc(id, channelId, isVideo, offerSet){
		
		var ssrc = ((id << 2) | channelId) + '';
		
		var mid = 'rem' + ssrc;
		
		// First just check it doesn't already exist in the offer set:
		var existing = offerSet.offers.find(o => o.mid == mid);
		
		if(existing){
			return;
		}
		
		var mediaText = isVideo ? this.remoteOffer.video : this.remoteOffer.audio;
		mediaText += this.remoteOffer.candidate;
		mediaText += 'a=sendonly\r\na=mid:' + mid + '\r\n';
		
		/*
			if(id == this.selfId){
				var state = this.shareState[this.channelName(channelId)];
				
				console.log("- Adding a sendonly tx -");
				state.sender = this.peer.addTransceiver(state.track, {streams:[this.sendStream], direction: 'sendonly'});
				trackId = state.track.id;
				streamId = this.sendStream.id;
			}else{
				
				var str = this.streamMaps[''+id];
				
				if(!str){
					str = new MediaStream();
					this.streamMaps[''+id] = str;
				}
				
				console.log("- Adding a tx -");
				var tx = this.peer.addTransceiver(isVideo ? 'video' : 'audio', {direction: 'recvonly'});
				trackId = tx.receiver.track.id;
				streamId = str.id;
			}
		*/
		
		// All of a users sources are part of the same stream. This is important for lip sync.
		// They are however different tracks - each channel gets its own track ID.
		
		var streamId = 'hud-str-' + ssrc; // User
		var trackId = 'hud-trk-' + ssrc; // Channel
		
		var cname = 'h' + ssrc;
		var len = cname.length;
		
		for(var i=len;i<5;i++){
			cname += '_';
		}
		
		mediaText += 'a=ssrc:' + ssrc + ' cname:'+ cname +'\r\n' + 
		'a=msid:' + streamId + ' ' + trackId + '\r\n'; // stream ID + track ID
		// 'a=ssrc:' + ssrc + ' mslabel:' + streamId + '\r\n' + // stream ID
		// 'a=ssrc:' + ssrc + ' label:' + trackId + '\r\n'; // track ID
		
		// a=msid:p5btOBadXL9eX5QG 75145d65-f8b0-4eed-b842-3dfebe51e0af
		// a=ssrc:273768218 cname:p5btOBadXL9eX5QG
		
		offerSet.offers.push({
			block: mediaText,
			mid
		});
	}
	
	createRemoteSdp(){
		var mids = 'a=group:BUNDLE';
		var offerBlock = '';
		
		for(var i=0;i<this.offerSet.offers.length;i++){
			var offer = this.offerSet.offers[i];
			mids += ' ' + offer.mid;
			offerBlock += offer.block;
		}
		
		var sdp = this.remoteOffer.header.replace('o=huddle 0 1 IN ', 'o=huddle 0 ' + (this.sdpVersion++) + ' IN ');
		sdp += mids + '\r\n';
		sdp += offerBlock;
		
		return sdp;
	}
	
	mungeLocalDescription(sdp, isOfferer){
		
		var newHeader = '';
		var newMediaSections = '';
		
		var offerLines = sdp.split('\r\n');
		var current = null;
		var currentCname = null;
		var localGenerated = '';
		var mid;
		
		for(var i=0;i<offerLines.length;i++){
			var line = offerLines[i];
			
			if(line.startsWith("m=audio")){
				
				current = "audio";
				currentCname = null;
				newMediaSections += this.remoteOffer.audio;
				
			}else if(line.startsWith("m=video")){
				
				current = "video";
				currentCname = null;
				newMediaSections += this.remoteOffer.video;
				
			}else if(!current){
				
				// Header zone
				newHeader += line + '\r\n';
				
			}else if(line.startsWith("a=mid:")){
				
				mid = line.substring(6).trim();
				
				/*
				var block = this.offerSet.offers.find(o => o.mid == mid);
				
				if(!block){
					var mediaText = current == 'audio' ? this.remoteOffer.audio : this.remoteOffer.video;
					mediaText += this.remoteOffer.candidate;
					mediaText += 'a=recvonly\r\na=mid:' + mid + '\r\n';
					
					// this is the description for "channel" (mic, screenshare etc)
					
					this.offerSet.offers.push({
						block: mediaText,
						mid
					});
				}
				*/

				var isSelf = false;
				
				if(mid.startsWith('rem')){
					// Derive the ID and check if it's ours:
					var id = mid.substring(3);
					if((parseInt(id) >> 2) == this.selfId){
						isSelf = true;
					}
				}
				
				if(!isSelf){
					// Remote mid - recvonly:
					newMediaSections += 'a=recvonly\r\n';
				}else{
					// sendonly, active:
					newMediaSections += 'a=sendonly\r\n';
					
				}
				
				if(isOfferer){
					newMediaSections +='a=setup:actpass\r\n';
				}else{
					newMediaSections +='a=setup:active\r\n';
				}
				
				// Add the mid:
				newMediaSections += 'a=mid:' + mid + '\r\n';
				
			}else{
				// capture ssrcs and candidates. We'll then use them if the current block is identified as a local one.
				if(line.startsWith('a=candidate') || line.startsWith('a=end-of-candidates') || line.startsWith("a=fingerprint") || line.startsWith("a=ice-")){
					newMediaSections += line + '\r\n';
				}else if(line.startsWith('a=ssrc') && line.indexOf(" cname:") != -1 && !currentCname){
					currentCname = true;
					
					// Remap the SSRC to the correct one for this media:
					var newSsrc = mid.substring(3);
					var numAndInfo = line.substring(7);
					var pieces = numAndInfo.split(' ');
					
					pieces[0] = newSsrc;
					newMediaSections += 'a=ssrc:' + pieces.join(' ') + '\r\n';
				}
			}
		}
		
		return newHeader + newMediaSections;
	}
	
	buildOffer(presence, allChannels){
		var offerSet = this.offerSet;
		this.userSsrcAudio(presence.id, allChannels ? 15 : presence.channels, offerSet);
		this.userSsrcVideo(presence.id, allChannels ? 15 : presence.channels, offerSet);
	}
	
	updateAnswer(){
		if(this.isUpdatingOfferAnswer){
			return this.isUpdatingOfferAnswer.then(() => this.updateAnswer());
		}
		
		this.isUpdatingOfferAnswer = new Promise((s, r) => {
			
			if(!this.offerSet.offers.length){
				
				var e = this.createEvent('userchange');
				e.users = this.users;
				this.dispatchEvent(e);
				
				// Nothing producing or consuming.
				return Promise.resolve(this.peer);
			}
			
			var offer = this.createRemoteSdp();
			
			console.log("(remote) offer", offer);
			
			return this.peer.setRemoteDescription({sdp: offer, type: 'offer'})
			.then(() => this.peer.createAnswer())
			.then(answer => {
				var sdp = this.mungeLocalDescription(answer.sdp, false); // won't be any new local channels in this
				
				console.log("(local) answer", sdp);
				
				answer = new RTCSessionDescription({
					type: 'answer',
					sdp: sdp
				});
				
				return this.peer.setLocalDescription(answer);
			})
			.then(() => {
				this.syncTransceivers();
				return this.peer;
			})
			.catch((e) => {
				console.error("Caught: ", e);
			})
			.then(() => {
				this.isUpdatingOfferAnswer = null;
				return s();
			})
			.catch(r);
			
		});
		
		return this.isUpdatingOfferAnswer;
	}
	
	syncTransceivers(){
		if(this.peer.iceGatheringState != 'complete'){
			return;
		}
		
		this.users.forEach(presence => {
			if(presence.id == this.selfId){
				return;
			}
			presence.audioTrack = null;
			presence.videoTrack = null;
			presence.sharingTrack = null;
		});
		
		// Get the receivers:
		var allTransceivers = this.peer.getTransceivers();
		
		// For each one, identify which user it's associated with:
		allTransceivers.forEach(tcv => {
			
			if(!tcv.mid || tcv.mid.indexOf('rem') != 0){
				return;
			}
			
			// ssrc:
			var ssrc = parseInt(tcv.mid.substring(3));
			
			var trackType = ssrc & 3; // 0=mic, 1=webcam, 2=screenshare
			
			// Identify the user:
			var peerId = ssrc >> 2;
			
			if(peerId == this.selfId){
				return;
			}
			
			var track = tcv.receiver.track;
			
			var user = this.users.find(p => p.id == peerId);
			
			console.log("Associating track ", track, peerId, user);
			
			if(!user || !track){
				return;
			}
			
			if(track.kind == 'audio'){
				user.audioTrack = track;
			}else if(track.kind == 'video'){
				if(trackType == 2){
					user.sharingTrack = track;
				}else{
					user.videoTrack = track;
				}
			}
			
		});
		
		var e = this.createEvent('userchange');
		e.users = this.users;
		this.dispatchEvent(e);
		
	}
	
	addEventListener(a,b){
		var set = this.eventHandlers[a];
		if(!set){
			this.eventHandlers[a]=set=[];
		}
		set.push(b);
	}
	
	dispatchEvent(e){
		console.log("Event object: ", e);
		var set = this.eventHandlers[e.type];
		if(set){
			set.forEach(method => method(e));
		}
	}
	
	start(){
		if(this.wasStarted){
			return;
		}
		this.wasStarted=true;
		
		if (this.props.host)
		{
			this.initSocket(props.host, props.isHttp);
			this.socket.start();
		}
		else
		{
			// Ask for a random server:
			var host = '';
			
			if(this.props.serviceHost){
				if(this.props.serviceHost.indexOf('localhost') === 0){
					host = 'http://' + this.props.serviceHost + '/v1/';
				}else{
					host = 'https://' + this.props.serviceHost + '/v1/';
				}
			}
			
			webRequest(host + 'huddle/join').then(response => {
				console.log('Joining via server ' + response.json.address);
				this.initSocket(response.json.address, response.json.http);
				this.socket.start();
			});
		}
	}
	
	sendSocketJoin(){
		// Connect the websocket and make a join request:
		var writer = new this.socket.Writer(44);
		writer.writeUtf8(_huddleUserState.avatarRef); // avatarRef
		writer.writeUtf8(null); // configJson (unused, extension port)
		writer.writeUtf8(_huddleUserState.displayName); // displayName
		writer.writeUtf8(this.props.slug); // huddleSlug
		writer.writeUInt32(12); // Initial number of audience members to tune in to.
		writer.writeUtf8(this.props.roleKey); // Role key
		writer.writeUtf8(_huddleUserState.token); // User token
		this.socket.send(writer);
		
		// Note that a successful join request will subscribe to changes and reply with both an offer and the list of people in the meeting.
	}
	
	recordingState(active){
		// Connect the websocket and make a join request:
		var writer = new this.socket.Writer(46);
		writer.writeUInt32(active ? 1 : 0); // Recording mode
		this.socket.send(writer);
	}
	
	castVideoFile(fileRef) {
		var video = document.createElement('video');
		video.loop = true;
		video.src = getRef(fileRef, {url: true});
		return video.play().then(() => {
			if(video.mozCaptureStream){
				return video.mozCaptureStream();
			}
			return video.captureStream();
		});
	}

	castAudioFile(fileRef) {
		var audio = new Audio();
		audio.loop = true;
		audio.src = getRef(fileRef, {url: true});
		return audio.play().then(() => {
			var AudioContext = window.AudioContext || window.webkitAudioContext;
			var audioCtx = new AudioContext();
			var eleSource = audioCtx.createMediaElementSource(audio);
			var mixedOutput = audioCtx.createMediaStreamDestination();
			eleSource.connect(mixedOutput);
			return mixedOutput.stream;
		});
	}
	
	getScreenshare(withAudio){
		return global.navigator.mediaDevices.getDisplayMedia(
			{
				audio : withAudio,
				video : {
					displaySurface : 'monitor',
					logicalSurface : true,
					cursor         : true,
					width          : { max: 1920, ideal: 1920 },
					height         : { max: 1080, ideal: 1080 },
					frameRate      : { max: 30 }
				}
			}
		);
	}

	getWebcam(){
		var constraints = {
			video : {
				width          : { max: 1920/2, ideal: 1920/2 },
				height         : { max: 1080/2, ideal: 1080/2 },
				frameRate      : { max: 30 }
			},
			audio: false
		};
		
		if(this.props.deviceIdVideo){
			constraints.video.deviceId = this.props.deviceIdVideo;
		}
		
		return global.navigator.mediaDevices.getUserMedia(constraints);
	}
	
	getMicrophone(){
		var automaticTidyUp = true;
		
		var constraints = {
			audio : {
				autoGainControl: automaticTidyUp,
				noiseSuppression: automaticTidyUp,
				echoCancellation: automaticTidyUp,
				typingNoiseDetection: automaticTidyUp,
				audioMirroring: false,
				highpassFilter: automaticTidyUp
			},
			video: false
		};
		
		if(this.props.deviceIdAudio){
			constraints.audio.deviceId = this.props.deviceIdAudio;
		}
		
		return global.navigator.mediaDevices.getUserMedia(constraints).then(stream => {
			var AudioContext = window.AudioContext || window.webkitAudioContext;
			var audioCtx = new AudioContext();
			var micIn = audioCtx.createMediaStreamSource(stream);
			var mixedOutput = audioCtx.createMediaStreamDestination();
			micIn.connect(mixedOutput);
			var s = mixedOutput.stream;
			s._stop = () => {
				stream.getTracks().forEach(t => t.stop());
			};
			return s;
		});
	}
	
	webcam(on){
		return this.changeState(on, 'webcam', () => this.getWebcam());
	}
	
	screenshare(on, cancelled) {
		return this.changeState(on, 'screenshare', () => this.getScreenshare(false), cancelled);
	}
	
	microphone(on) {
		return this.changeState(on, 'microphone', () => this.getMicrophone());
	}
	
	isActive(channel) {
		return this.shareState[channel].active;
	}
	
	changeState(on, channel, getStream, cancelled) {
		var currentSender = this.shareState[channel].sender;
		
		if((!!currentSender) == on){
			// No change
			return Promise.resolve(currentSender);
		}
		
		var selfUser = this.users.find(u => u.id == this.selfId);
		
		if(!selfUser){
			return Promise.resolve(currentSender);
		}
		
		if(!on){
			this.shareState[channel].sender = null;
			this.shareState[channel].active = false;
			
			if(channel == 'screenshare'){
				selfUser.sharingTrack = null;
			}else if(channel == 'webcam'){
				selfUser.videoTrack = null;
			}
			
			this.updateChannelState(selfUser);
			
			// this.peer.removeTrack(currentSender.transceiver);
			
			if(currentSender.stream._stop){
				currentSender.stream._stop();
			}
			
			currentSender.track.stop();
			
			return this.makeOffer().then(()=> null);
		}
		
		return getStream().then(stream => {

			if(!stream){
				return null;
			}
			
			var kind = 'video';
			
			if(channel == 'microphone'){
				kind = 'audio';
			}
			
			var tracks = stream.getTracks();
			var track = tracks.find(t => t.kind == kind);
			
			if(!track){
				return null;
			}
			
			var state = this.shareState[channel];
			
			var sender = {};
			state.sender = sender;
			state.active = true;
			
			sender.track = track;
			sender.stream = stream;
			
			track.addEventListener('ended', () => {
				
				// Identifies external user track ending triggers, such as pressing the "stop sharing" screenshare button
				this.changeState(false, channel);
				
			});
			
			// Updating the answer will result in the sendrecv info being added to it.
			this.sendStream.addTrack(track);
			
			if(channel == 'screenshare'){
				selfUser.sharingTrack = track;
			}else if(channel == 'webcam'){
				selfUser.videoTrack = track;
			}
			
			this.updateChannelState(selfUser);
			
			// Add the channel offer:
			var channelId = this.channelId(channel);
			
			var ssrc = ((this.selfId << 2) | channelId) + '';
		
			var mid = 'rem' + ssrc;
			
			var transceiver = this.peer.getTransceivers().find(tc => tc.mid == mid);
			
			transceiver.sender.replaceTrack(track).then(r => console.log(r)).catch(console.error);
			
			// As we made a local side change, must offer it:
			return this.makeOffer(channel).then(() => sender);
		})
			.catch((err) => {

				if (on && channel == 'screenshare') {
					return cancelled;
				}
			});

	}
	
}