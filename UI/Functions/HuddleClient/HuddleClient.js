import {
	mediasoup,
	protoo
} from './WebRtc.js';

import webRequest from 'UI/Functions/WebRequest';
import webSocket from 'UI/Functions/WebSocket';

// If more people than this in a meeting, the camera does not enable by default when more people join.
// Still usable, but just not on by default anymore.
const CAM_MAX = 10;

const VIDEO_CONSTRAINS =
{
	qvga : { width: { ideal: 320 }, height: { ideal: 240 } },
	vga  : { width: { ideal: 640 }, height: { ideal: 480 } },
	hd   : { width: { ideal: 1280 }, height: { ideal: 720 } }
};

const PC_PROPRIETARY_CONSTRAINTS =
{
	optional : [ { googDscp: true } ]
};

// Used for simulcast webcam video.
const WEBCAM_SIMULCAST_ENCODINGS =
[
	{ scaleResolutionDownBy: 4, maxBitrate: 500000 },
	{ scaleResolutionDownBy: 2, maxBitrate: 1000000 },
	{ scaleResolutionDownBy: 1, maxBitrate: 5000000 }
];

// Used for VP9 webcam video.
const WEBCAM_KSVC_ENCODINGS =
[
	{ scalabilityMode: 'S3T3_KEY' }
];

// Used for simulcast screen sharing.
const SCREEN_SHARING_SIMULCAST_ENCODINGS =
[
	{ scaleResolutionDownBy: 1, dtx: true, maxBitrate: 1500000 },
	{ scaleResolutionDownBy: 1, dtx: true, maxBitrate: 6000000 }
];

// Used for VP9 screen sharing.
const SCREEN_SHARING_SVC_ENCODINGS =
[
	{ scalabilityMode: 'S3T3', dtx: true }
];

export default class HuddleClient
{
	
	constructor(
		props
	)
	{
		const {
			// roomId, roomSlug or joinUrl - one of these is required.
			joinUrl,
			useSimulcast,
			useSharingSimulcast,
			forceTcp,
			produce,
			consume,
			forceH264,
			forceVP9,
			svc,
			datachannel,
			device,
			busyMeeting,
			directChatOnly,
			excludeRoles,
			cameraQuality, // camera stream quality to use. hd, qvga, vga.
			shareAudio // True if it should capture screenshare audio instead of mic audio
		} = props;
		
		this.props = props;
		
		this.events = {};
		this.producers = [];
		this.producersById = {};
		this.consumers = [];
		this.consumersById = {};
		this.peers = [];
		this.peersById = {};
		this.device = device || {};
		
		this.room = {
			state: 'closed',
			activeSpeakerId: 0
		};
		
		this.filters = {
			directChatOnly,
			excludeRoles
		};
		
		this.me = {
			canSendMic: false,
			canSendWebcam: false,
			canChangeWebcam: false,
			webcamInProgress: false,
			shareInProgress: false,
			audioOnly: false,
			audioOnlyInProgress: false,
			audioMuted: false,
			restartIceInProgress: false,
			profile: {}
		};
		
		this.shareAudio = shareAudio === undefined ? false : shareAudio;
		
		// Closed flag.
		// @type {Boolean}
		this._closed = false;
		
		// Whether we want to force RTC over TCP.
		// @type {Boolean}
		this._forceTcp = forceTcp;

		// Whether we want to produce audio/video.
		// @type {Boolean}
		this._produce = produce;

		// Whether we should consume.
		// @type {Boolean}
		this._consume = consume;

		// Whether we want DataChannels.
		// @type {Boolean}
		this._useDataChannel = datachannel;

		// Force H264 codec for sending.
		this._forceH264 = Boolean(forceH264);

		// Force VP9 codec for sending.
		this._forceVP9 = Boolean(forceVP9);

		// External video.
		// @type {HTMLVideoElement}
		this._externalVideo = null;

		// MediaStream of the external video.
		// @type {MediaStream}
		this._externalVideoStream = null;

		// Next expected dataChannel test number.
		// @type {Number}
		this._nextDataChannelTestNumber = 0;
		
		// Whether simulcast should be used.
		// @type {Boolean}
		this._useSimulcast = useSimulcast;

		// Whether simulcast should be used in desktop sharing.
		// @type {Boolean}
		this._useSharingSimulcast = useSharingSimulcast;
		
		this._busyMeeting = busyMeeting;
		
		// protoo-client Peer instance.
		// @type {protoo.Peer}
		this._protoo = null;

		// mediasoup-client Device instance.
		// @type {mediasoup.Device}
		this._mediasoupDevice = null;

		// mediasoup Transport for sending.
		// @type {mediasoup.Transport}
		this._sendTransport = null;

		// mediasoup Transport for receiving.
		// @type {mediasoup.Transport}
		this._recvTransport = null;

		// Local mic mediasoup Producer.
		// @type {mediasoup.Producer}
		this._micProducer = null;

		// Local webcam mediasoup Producer.
		// @type {mediasoup.Producer}
		this._webcamProducer = null;

		// Local share mediasoup Producer.
		// @type {mediasoup.Producer}
		this._shareProducer = null;

		// Local chat DataProducer.
		// @type {mediasoup.DataProducer}
		this._chatDataProducer = null;

		// Local bot DataProducer.
		// @type {mediasoup.DataProducer}
		this._botDataProducer = null;

		// mediasoup Consumers.
		// @type {Map<String, mediasoup.Consumer>}
		this._consumers = new Map();

		// mediasoup DataConsumers.
		// @type {Map<String, mediasoup.DataConsumer>}
		this._dataConsumers = new Map();

		// Map of webcam MediaDeviceInfos indexed by deviceId.
		// @type {Map<String, MediaDeviceInfos>}
		this._webcams = new Map();
		
		this.defaultCameraQuality = cameraQuality || 'hd';
		
		// Local Webcam.
		// @type {Object} with:
		// - {MediaDeviceInfo} [device]
		// - {String} [resolution] - 'qvga' / 'vga' / 'hd'.
		this._webcam =
		{
			device     : null,
			resolution : this.defaultCameraQuality
		};

		// Set custom SVC scalability mode.
		if (svc)
		{
			WEBCAM_KSVC_ENCODINGS[0].scalabilityMode = svc + '_KEY';
			SCREEN_SHARING_SVC_ENCODINGS[0].scalabilityMode = svc;
		}
	}

	close()
	{
		var wasClosed = this._closed;
		this._closed = true;
		
		if(this._permAudioTrack){
			this._permAudioTrack.stop();
			this._permAudioTrack=null;
		}
		
		// Close protoo Peer
		if(this._protoo)
			this._protoo.close();
		
		// Close mediasoup Transports.
		if (this._sendTransport)
			this._sendTransport.close();
		
		if (this._recvTransport)
			this._recvTransport.close();
		
		if(!wasClosed){
			this.room.state = 'closed';
			this.dispatchEvent({type: 'roomupdate', room: this.room});
		}
	}
	
	dispatchEvent(evt)
	{
		if(!evt){
			return;
		}
		var set = this.events[evt.type];
		if(!set){
			return;
		}
		set.forEach(s => s(evt));
	}
	
	addEventListener(evt, handler)
	{
		var set = this.events[evt];
		if(!set){
			this.events[evt] = [handler];
		}else{
			set.push(handler);
		}
	}
	
	removeEventListener(evt, handler)
	{
		var set = this.events[evt];
		if(!set){
			return;
		}
		this.events[evt] = set.filter(h => h!=handler);
	}
	
	joinFail()
	{
		this.dispatchEvent(
		{
			type : 'error',
			join : true,
			text : 'This meeting doesn\'t exist, or you weren\'t able to join it.'
		});
	}
	
	async join()
	{
		var url;
		var huddle;
		
		if(this.props.huddle && this.props.meetingUrl){
			// Use these instead.
			url = this.props.meetingUrl;
			huddle = this.props.huddle;
		}else{
			var joinUrl = this.props.joinUrl;
			
			if(!joinUrl){
				joinUrl = this.props.roomSlug ? 'huddle/' + this.props.roomSlug + '/slug/join' : 'huddle/' + this.props.roomId + '/join';
			}
			
			var huddleInfo = await webRequest(joinUrl);
			
			if(!huddleInfo || !huddleInfo.json){
				return this.joinFail();
			}
			
			url = huddleInfo.json.connectionUrl;
			huddle = huddleInfo.json.huddle;
		}
		
		if(!url){
			return this.joinFail();
		}
		
		if(this._closed){
			return;
		}
		
		// huddleType comes from here:
		this.room.huddle = huddle;
		
		var isHttps = !url.startsWith("localhost");
		const protooTransport = new protoo.WebSocketTransport((isHttps ? 'wss' : 'ws') + '://' + url.trim());
		this._protoo = new protoo.Peer(protooTransport);
		
		this.room.state = 'connecting';
		this.dispatchEvent({type: 'roomupdate', room: this.room});
		
		this._protoo.on('open', () => this._joinRoom());

		this._protoo.on('failed', () =>
		{
			this.dispatchEvent(
			{
				type : 'error',
				text : 'Unable to connect to the meeting. Please check your internet connection. To try and reconnect, please refresh the page.'
			});
		});

		this._protoo.on('disconnected', () =>
		{
			this.dispatchEvent(
			{
				type : 'error',
				text : 'Disconnected from the meeting. Your internet connection may have been interrupted. To try and reconnect, please refresh the page.'
			});

			// Close mediasoup Transports.
			if (this._sendTransport)
			{
				this._sendTransport.close();
				this._sendTransport = null;
			}

			if (this._recvTransport)
			{
				this._recvTransport.close();
				this._recvTransport = null;
			}
			
			this.room.state = 'closed';
			this.dispatchEvent({type: 'roomupdate', room: this.room});
			
		});

		this._protoo.on('close', () =>
		{
			if (this._closed)
				return;

			this.close();
		});

		// eslint-disable-next-line no-unused-vars
		this._protoo.on('request', async (request, accept, reject) =>
		{
			switch (request.method)
			{
				case 'newConsumer':
				{
					if (!this._consume)
					{
						reject(403, 'I do not want to consume');

						break;
					}

					const {
						peerId,
						producerId,
						id,
						kind,
						rtpParameters,
						type,
						appData,
						producerPaused
					} = request.data;

					try
					{
						const consumer = await this._recvTransport.consume(
							{
								id,
								producerId,
								kind,
								rtpParameters,
								appData : { ...appData, peerId } // Trick.
							});

						// Store in the map.
						this._consumers.set(consumer.id, consumer);

						consumer.on('transportclose', () =>
						{
							this._consumers.delete(consumer.id);
						});

						const { spatialLayers, temporalLayers } =
							mediasoup.parseScalabilityMode(
								consumer.rtpParameters.encodings[0].scalabilityMode);
						
						this.addConsumer(
						{
							id                     : consumer.id,
							type                   : type,
							locallyPaused          : false,
							remotelyPaused         : producerPaused,
							rtpParameters          : consumer.rtpParameters,
							spatialLayers          : spatialLayers,
							temporalLayers         : temporalLayers,
							preferredSpatialLayer  : spatialLayers - 1,
							preferredTemporalLayer : temporalLayers - 1,
							priority               : 1,
							codec                  : consumer.rtpParameters.codecs[0].mimeType.split('/')[1],
							track                  : consumer.track
						},
						peerId);
						
						// We are ready. Answer the protoo request so the server will
						// resume this Consumer (which was paused for now if video).
						accept();

						// If audio-only mode is enabled, pause it.
						if (consumer.kind === 'video' && this.me.audioOnly)
							this._pauseConsumer(consumer);
					}
					catch (error)
					{
						console.error('"newConsumer" request failed:%o', error);

						this.dispatchEvent(
						{
							type : 'error',
							minor: true,
							text : 'Error creating a Consumer: ' + error
						});

						throw error;
					}

					break;
				}
			}
		});
		
		this._protoo.on('notification', (notification) =>
		{
			switch (notification.method)
			{
				case 'producerScore':
				{
					const { producerId, score } = notification.data;
					var producer = this.producersById[producerId];
					producer.score = score;
					this.dispatchEvent({type:'producerscore', producer});
					break;
				}
				
				case 'peerUpdated':
				{
					const { peerId, fields } = notification.data;
					
					var target;
					
					if(peerId == this.me.peerId){
						target = this.me;
					}else{
						target = this.peersById[peerId];
					}
					
					if(!target || !target.profile){
						return;
					}
					
					for(var f in fields){
						var val = fields[f];
						target.profile[f] = val;
					}
					
					this.dispatchEvent({
						type: 'roomupdate',
						room: this.room
					});
					
					break;
				}
				
				case 'welcome':
				{
					const {id, profile} = notification.data;
					this.me.peerId = id;
					this.me.profile = profile;
					
					this.dispatchEvent({
						type: 'roomupdate',
						room: this.room
					});
					break;
				}
				
				case 'newPeer':
				{
					const peer = notification.data;
					this.addPeer({ ...peer, consumers: [], dataConsumers: [] });
					break;
				}
				
				case 'peerClosed':
				{
					const { peerId } = notification.data;
					this.removePeer(peerId);

					break;
				}
				
				case 'downlinkBwe':
				{
					break;
				}

				case 'consumerClosed':
				{
					const { consumerId } = notification.data;
					const consumer = this._consumers.get(consumerId);

					if (!consumer)
						break;

					consumer.close();
					this._consumers.delete(consumerId);

					const { peerId } = consumer.appData;
					this.removeConsumer(consumerId, peerId);
					break;
				}

				case 'consumerPaused':
				{
					const { consumerId } = notification.data;
					const consumer = this._consumers.get(consumerId);

					if (!consumer)
						break;

					consumer.pause();
					
					var c = this.consumersById[consumerId];
					c.remotelyPaused = true;
					this.dispatchEvent({
						type: 'consumerpaused',
						consumer: c
					});
					
					break;
				}

				case 'consumerResumed':
				{
					const { consumerId } = notification.data;
					const consumer = this._consumers.get(consumerId);

					if (!consumer)
						break;

					consumer.resume();

					var c = this.consumersById[consumerId];
					c.remotelyPaused = false;
					this.dispatchEvent({
						type: 'consumerresumed',
						consumer: c
					});
					
					break;
				}

				case 'consumerLayersChanged':
				{
					const { consumerId, spatialLayer, temporalLayer } = notification.data;
					const consumer = this._consumers.get(consumerId);

					if (!consumer)
						break;
					
					var c = this.consumersById[consumerId];
					if(c){
						c.spatialLayer = spatialLayer;
						c.temporalLayer = temporalLayer;
						this.dispatchEvent(
						{
							type : 'consumerpreferredlayers',
							consumer: c
						});
					}

					break;
				}

				case 'consumerScore':
				{
					const { consumerId, score } = notification.data;
					
					var c = this.consumersById[consumerId];
					if(c){
						c.score = score;
						this.dispatchEvent(
						{
							type : 'consumerscore',
							consumer: c
						});
					}
					
					break;
				}
				
				case 'activeSpeaker':
				{
					const { peerId } = notification.data;
					
					this.room.activeSpeakerId = peerId;
					
					this.dispatchEvent({
						type: 'activespeaker',
						peerId
					});
					
					this.dispatchEvent({
						type: 'roomupdate',
						room: this.room
					});
					
					break;
				}

				default:
				{
					console.error(
						'unknown protoo notification.method "%s"', notification.method);
				}
			}
		});
	}
	
	async requestToSpeak(flag){
		this.me.profile.requestedToSpeak = flag;
		
		await this._protoo.request('requestSpeaker', { active: flag });
		
		this.dispatchEvent({
			type: 'roomupdate',
			room: this.room
		});
	}
	
	async setAsSpeaker(peer, flag){
		await updatePeer(peer, {
			isPermittedSpeaker: flag
		});
	}
	
	async updatePeer(peer, fields){
		if(this.me.profile.huddleRole != 1){
			return;
		}
		
		// Initial local update:
		for(var f in fields){
			var val = fields[f];
			peer.profile[f] = val;
		}
		
		await this._protoo.request('peerUpdate', { remotePeerId: peer.id, fields });
		
		this.dispatchEvent({
			type: 'roomupdate',
			room: this.room
		});
	}
	
	addConsumer(consumer, peerId){
		var peer = this.peersById[peerId];
		if (!peer){
			throw new Error('no Peer found for new Consumer');
		}
		
		this.consumersById[consumer.id] = consumer;
		this.consumers.push(consumer);
		
		peer.consumers.push(consumer);
		this.dispatchEvent({
			type: 'consumeradd',
			consumer
		});
	}
	
	addPeer(peer){
		this.peersById[peer.id] = peer;
		this.peers.push(peer);
		this.dispatchEvent({
			type: 'peeradd',
			peer
		});
	}
	
	addProducer(producer){
		this.producersById[producer.id] = producer;
		this.producers.push(producer);
		this.dispatchEvent({
			type: 'produceradd',
			producer
		});
	}
	
	removeConsumer(consumerId, peerId){
		var peer = this.peersById[peerId];
		var consumer = this.consumersById[consumerId];
		if(!peer || !consumer){
			return;
		}
		peer.consumers = peer.consumers.filter(c => c.id != consumerId);
		
		delete this.consumersById[consumerId];
		this.consumers = this.consumers.filter(p => p.id != consumerId);
		
		this.dispatchEvent({
			type: 'consumerremove',
			consumer
		});
	}
	
	removePeer(peerId){
		var peer = this.peersById[peerId];
		if(!peer){
			return;
		}
		
		delete this.peersById[peerId];
		this.peers = this.peers.filter(p => p.id != peerId);
		
		this.dispatchEvent({
			type: 'peerremove',
			peer
		});
	}
	
	removeProducer(producerId){
		var producer = this.producersById[producerId];
		if(!producer){
			return;
		}
		
		delete this.producersById[producerId];
		this.producers = this.producers.filter(pro => pro.id != producerId);
		
		this.dispatchEvent({
			type: 'producerremove',
			producer
		});
	}
	
	async enableMic()
	{
		if (this._micProducer)
			return;

		if (!this._mediasoupDevice.canProduce('audio'))
		{
			console.error('enableMic() | cannot produce audio');

			return;
		}

		let track;

		try
		{
			if (!this._externalVideo)
			{
				const stream = await global.navigator.mediaDevices.getUserMedia({ audio: true });

				track = stream.getAudioTracks()[0];
			}
			else
			{
				const stream = await this._getExternalVideoStream();

				track = stream.getAudioTracks()[0].clone();
			}

			this._micProducer = await this._sendTransport.produce(
				{
					track,
					codecOptions :
					{
						opusStereo : 1,
						opusDtx    : 1
					}
					// NOTE: for testing codec selection.
					// codec : this._mediasoupDevice.rtpCapabilities.codecs
					// 	.find((codec) => codec.mimeType.toLowerCase() === 'audio/pcma')
				});
			
			var producer = {
					id            : this._micProducer.id,
					paused        : this._micProducer.paused,
					track         : this._micProducer.track,
					rtpParameters : this._micProducer.rtpParameters,
					codec         : this._micProducer.rtpParameters.codecs[0].mimeType.split('/')[1]
				};
			
			this.addProducer(producer);
			
			this._micProducer.on('transportclose', () =>
			{
				this._micProducer = null;
			});

			this._micProducer.on('trackended', () =>
			{
				this.dispatchEvent(
					{
						type : 'error',
						minor: true,
						text : `Microphone disconnected!`
					});

				this.disableMic()
					.catch(() => {});
			});
		}
		catch (error)
		{
			console.error('enableMic() | failed:%o', error);

			this.dispatchEvent(
				{
					type : 'error',
					minor: true,
					text : `Error enabling microphone: ` + error
				});

			if (track)
				track.stop();
		}
	}



	async disableMic()
	{
		if (!this._micProducer)
			return;

		this._micProducer.close();
		
		this.removeProducer(this._micProducer.id);
		
		try
		{
			await this._protoo.request('closeProducer', { producerId: this._micProducer.id });
		}
		catch (error)
		{
			this.dispatchEvent({
				type : 'error',
				minor: true,
				text : 'Error closing server-side mic Producer: ' + error
			});
		}
		this._micProducer = null;
	}

	async muteMic()
	{
		this._micProducer.pause();

		try
		{
			await this._protoo.request('pauseProducer', { producerId: this._micProducer.id });
			
			var producer = this.producersById[this._micProducer.id];
			producer.paused = true;
			
			this.dispatchEvent({
				type: 'producerpaused',
				producer
			});
			
		}
		catch (error)
		{
			console.error('muteMic() | failed: %o', error);
			this.dispatchEvent({
				type : 'error',
				minor: true,
				text : 'Error pausing server-side mic Producer: ' + error
			});
		}
	}

	async unmuteMic()
	{
		this._micProducer.resume();

		try
		{
			await this._protoo.request('resumeProducer', { producerId: this._micProducer.id });
			var producer = this.producersById[this._micProducer.id];
			producer.paused = false;
			
			this.dispatchEvent({
				type: 'producerresumed',
				producer
			});
		}
		catch (error)
		{
			console.error('unmuteMic() | failed: %o', error);

			this.dispatchEvent({
				type : 'error',
				minor: true,
				text : 'Error resuming server-side mic Producer: ' + error
			});
		}
	}
	
	async enableWebcam()
	{
		if (this._webcamProducer)
			return;
		else if (this._shareProducer)
			await this.disableShare();

		if (!this._mediasoupDevice.canProduce('video'))
		{
			console.error('enableWebcam() | cannot produce video');
			return;
		}

		let track;
		let device;
		
		this.me.webcamInProgress = true;
		this.dispatchEvent({
			type: 'webcamprogress',
			flag: true
		});
		
		try
		{
			if (!this._externalVideo)
			{
				await this._updateWebcams();
				device = this._webcam.device;

				const { resolution } = this._webcam;

				if (!device)
					throw new Error('no webcam devices');
				
				const stream = await global.navigator.mediaDevices.getUserMedia(
					{
						video :
						{
							deviceId : { ideal: device.deviceId },
							...VIDEO_CONSTRAINS[resolution]
						}
					});

				track = stream.getVideoTracks()[0];
			}
			else
			{
				device = { label: 'external video' };

				const stream = await this._getExternalVideoStream();

				track = stream.getVideoTracks()[0].clone();
			}

			let encodings;
			let codec;
			const codecOptions =
			{
				videoGoogleStartBitrate : 1000
			};

			if (this._forceH264)
			{
				codec = this._mediasoupDevice.rtpCapabilities.codecs
					.find((c) => c.mimeType.toLowerCase() === 'video/h264');

				if (!codec)
				{
					throw new Error('desired H264 codec+configuration is not supported');
				}
			}
			else if (this._forceVP9)
			{
				codec = this._mediasoupDevice.rtpCapabilities.codecs
					.find((c) => c.mimeType.toLowerCase() === 'video/vp9');

				if (!codec)
				{
					throw new Error('desired VP9 codec+configuration is not supported');
				}
			}

			if (this._useSimulcast)
			{
				// If VP9 is the only available video codec then use SVC.
				const firstVideoCodec = this._mediasoupDevice
					.rtpCapabilities
					.codecs
					.find((c) => c.kind === 'video');

				if (
					(this._forceVP9 && codec) ||
					firstVideoCodec.mimeType.toLowerCase() === 'video/vp9'
				)
				{
					encodings = WEBCAM_KSVC_ENCODINGS;
				}
				else
				{
					encodings = WEBCAM_SIMULCAST_ENCODINGS;
				}
			}

			this._webcamProducer = await this._sendTransport.produce(
				{
					track,
					encodings,
					codecOptions,
					codec
				});

			var producer = {
					id            : this._webcamProducer.id,
					deviceLabel   : device.label,
					type          : this._getWebcamType(device),
					paused        : this._webcamProducer.paused,
					track         : this._webcamProducer.track,
					rtpParameters : this._webcamProducer.rtpParameters,
					codec         : this._webcamProducer.rtpParameters.codecs[0].mimeType.split('/')[1]
				};
			
			this.addProducer(producer);
			
			this._webcamProducer.on('transportclose', () =>
			{
				this._webcamProducer = null;
			});

			this._webcamProducer.on('trackended', () =>
			{
				this.dispatchEvent({
					type : 'error',
					minor: true,
					text : 'Webcam disconnected!'
				});

				this.disableWebcam()
					.catch(() => {});
			});
		}
		catch (error)
		{
			console.error('enableWebcam() | failed:%o', error);

			this.dispatchEvent({
				type : 'error',
				minor: true,
				text : 'Error enabling webcam: ' + error
			});
			
			if (track)
				track.stop();
		}
		
		this.me.webcamInProgress = false;
		this.dispatchEvent({
			type: 'webcamprogress',
			flag: false
		});
	}
	
	async disableWebcam()
	{
		if (!this._webcamProducer)
			return;

		this._webcamProducer.close();
		this.removeProducer(this._webcamProducer.id);
		
		try
		{
			await this._protoo.request('closeProducer', { producerId: this._webcamProducer.id });
		}
		catch (error)
		{
			this.dispatchEvent(
			{
				type : 'error',
				minor: true,
				text : 'Error closing server-side webcam Producer: ' + error
			});
		}

		this._webcamProducer = null;
	}

	async changeWebcam()
	{
		this.me.webcamInProgress = true;
		this.dispatchEvent({
			type: 'webcamprogress',
			flag: true
		});
		
		try
		{
			await this._updateWebcams();

			const array = Array.from(this._webcams.keys());
			const len = array.length;
			const deviceId =
				this._webcam.device ? this._webcam.device.deviceId : undefined;
			let idx = array.indexOf(deviceId);

			if (idx < len - 1)
				idx++;
			else
				idx = 0;

			this._webcam.device = this._webcams.get(array[idx]);
			
			// Reset video resolution
			this._webcam.resolution = this.defaultCameraQuality;

			if (!this._webcam.device)
				throw new Error('no webcam devices');

			// Closing the current video track before asking for a new one (mobiles do not like
			// having both front/back cameras open at the same time).
			this._webcamProducer.track.stop();
			
			const stream = await global.navigator.mediaDevices.getUserMedia(
				{
					video :
					{
						deviceId : { exact: this._webcam.device.deviceId },
						...VIDEO_CONSTRAINS[this._webcam.resolution]
					}
				});

			const track = stream.getVideoTracks()[0];

			await this._webcamProducer.replaceTrack({ track });
			var producer = this.producersById[this._webcamProducer.id];
			producer.track = track;
			this.dispatchEvent({
				type: 'producertrack',
				producer
			});
		}
		catch (error)
		{
			this.dispatchEvent({
				type : 'error',
				minor: true,
				text : 'Could not change webcam: ' + error
			});
		}
		
		this.me.webcamInProgress = false;
		this.dispatchEvent({
			type: 'webcamprogress',
			flag: false
		});
	}

	async changeWebcamResolution()
	{
		this.me.webcamInProgress = true;
		this.dispatchEvent({
			type: 'webcamprogress',
			flag: true
		});
		
		try
		{
			switch (this._webcam.resolution)
			{
				case 'qvga':
					this._webcam.resolution = 'vga';
					break;
				case 'vga':
					this._webcam.resolution = 'hd';
					break;
				case 'hd':
					this._webcam.resolution = 'qvga';
					break;
				default:
					this._webcam.resolution = 'hd';
			}
			
			const stream = await global.navigator.mediaDevices.getUserMedia(
				{
					video :
					{
						deviceId : { exact: this._webcam.device.deviceId },
						...VIDEO_CONSTRAINS[this._webcam.resolution]
					}
				});

			const track = stream.getVideoTracks()[0];

			await this._webcamProducer.replaceTrack({ track });
			
			var producer = this.producersById[this._webcamProducer.id];
			producer.track = track;
			this.dispatchEvent({
				type: 'producertrack',
				producer
			});
		}
		catch (error)
		{
			console.error('changeWebcamResolution() | failed: %o', error);

			this.dispatchEvent({
				type : 'error',
				minor: true,
				text : 'Could not change webcam resolution: ' + error
			});
		}

		this.me.webcamInProgress = false;
		this.dispatchEvent({
			type: 'webcamprogress',
			flag: false
		});
	}

	async enableShare()
	{
		if (this._shareProducer)
			return;
		else if (this._webcamProducer)
			await this.disableWebcam();

		if (!this._mediasoupDevice.canProduce('video'))
		{
			console.error('enableShare() | cannot produce video');
			return;
		}

		let track;
		
		this.me.shareInProgress = true;
		this.dispatchEvent({
			type: 'shareprogress',
			flag: true
		});
		
		try
		{
			const stream = await global.navigator.mediaDevices.getDisplayMedia(
				{
					audio : this.shareAudio,
					video :
					{
						displaySurface : 'monitor',
						logicalSurface : true,
						cursor         : true,
						width          : { max: 1920 },
						height         : { max: 1080 },
						frameRate      : { max: 30 }
					}
				});

			// May mean cancelled (in some implementations).
			if (!stream)
			{
				this.me.shareInProgress = false;
				this.dispatchEvent({
					type: 'shareprogress',
					flag: false
				});
				return;
			}

			track = stream.getVideoTracks()[0];

			let encodings;
			let codec;
			const codecOptions =
			{
				videoGoogleStartBitrate : 1000
			};

			if (this._forceH264)
			{
				codec = this._mediasoupDevice.rtpCapabilities.codecs
					.find((c) => c.mimeType.toLowerCase() === 'video/h264');

				if (!codec)
				{
					throw new Error('desired H264 codec+configuration is not supported');
				}
			}
			else if (this._forceVP9)
			{
				codec = this._mediasoupDevice.rtpCapabilities.codecs
					.find((c) => c.mimeType.toLowerCase() === 'video/vp9');

				if (!codec)
				{
					throw new Error('desired VP9 codec+configuration is not supported');
				}
			}

			if (this._useSharingSimulcast)
			{
				// If VP9 is the only available video codec then use SVC.
				const firstVideoCodec = this._mediasoupDevice
					.rtpCapabilities
					.codecs
					.find((c) => c.kind === 'video');

				if (
					(this._forceVP9 && codec) ||
					firstVideoCodec.mimeType.toLowerCase() === 'video/vp9'
				)
				{
					encodings = SCREEN_SHARING_SVC_ENCODINGS;
				}
				else
				{
					encodings = SCREEN_SHARING_SIMULCAST_ENCODINGS
						.map((encoding) => ({ ...encoding, dtx: true }));
				}
			}

			this._shareProducer = await this._sendTransport.produce(
				{
					track,
					encodings,
					codecOptions,
					codec,
					appData :
					{
						share : true
					}
				});

			this.addProducer(
			{
				id            : this._shareProducer.id,
				type          : 'share',
				paused        : this._shareProducer.paused,
				track         : this._shareProducer.track,
				rtpParameters : this._shareProducer.rtpParameters,
				codec         : this._shareProducer.rtpParameters.codecs[0].mimeType.split('/')[1]
			});
			
			this._shareProducer.on('transportclose', () =>
			{
				this._shareProducer = null;
			});
			
			this._shareProducer.on('trackended', () =>
			{
				this.dispatchEvent(
				{
					type : 'error',
					minor: true,
					text : 'Share disconnected!'
				});

				this.disableShare()
					.catch(() => {});
			});
		}
		catch (error)
		{
			if (error.name !== 'NotAllowedError')
			{
				this.dispatchEvent(
				{
					type : 'error',
					minor: true,
					text : 'Error sharing: ' + error
				});
			}

			if (track)
				track.stop();
		}
		
		this.me.shareInProgress = false;
		this.dispatchEvent({
			type: 'shareprogress',
			flag: false
		});
	}

	async disableShare()
	{
		if (!this._shareProducer)
			return;

		this._shareProducer.close();
		this.removeProducer(this._shareProducer.id);

		try
		{
			await this._protoo.request('closeProducer', { producerId: this._shareProducer.id });
		}
		catch (error)
		{
			this.dispatchEvent(
			{
				type : 'error',
					minor: true,
				text : 'Error closing server-side share Producer: ' + error
			});
		}

		this._shareProducer = null;
	}

	async enableAudioOnly()
	{
		this.me.audioOnlyInProgress = true;
		this.dispatchEvent({
			type: 'audioonlyprogress',
			flag: true
		});
		this.disableWebcam();

		for (const consumer of this._consumers.values())
		{
			if (consumer.kind !== 'video')
				continue;

			this._pauseConsumer(consumer);
		}
		
		this.me.audioOnly = true;
		this.dispatchEvent({
			type: 'audioonly',
			flag: true
		});
		
		this.me.audioOnlyInProgress = false;
		this.dispatchEvent({
			type: 'audioonlyprogress',
			flag: false
		});
	}

	async disableAudioOnly()
	{
		this.me.audioOnlyInProgress = true;
		this.dispatchEvent({
			type: 'audioonlyprogress',
			flag: true
		});
		
		if (
			!this._webcamProducer &&
			this._produce
		)
		{
			this.enableWebcam();
		}

		for (const consumer of this._consumers.values())
		{
			if (consumer.kind !== 'video')
				continue;

			this._resumeConsumer(consumer);
		}
		
		this.me.audioOnly = false;
		this.dispatchEvent({
			type: 'audioonly',
			flag: false
		});
		
		this.me.audioOnlyInProgress = false;
		this.dispatchEvent({
			type: 'audioonlyprogress',
			flag: false
		});
	}
	
	async muteAudio()
	{
		this.me.audioMuted = true;
		this.dispatchEvent({
			type: 'audiomuted',
			enabled: true
		});
	}

	async unmuteAudio()
	{
		this.me.audioMuted = false;
		this.dispatchEvent({
			type: 'audiomuted',
			enabled: false
		});
	}

	async restartIce()
	{
		this.me.restartIceInProgress = true;
		this.dispatchEvent({
			type: 'restarticeprogress',
			flag: true
		});
		
		try
		{
			if (this._sendTransport)
			{
				const iceParameters = await this._protoo.request('restartIce',{ transportId: this._sendTransport.id });
				await this._sendTransport.restartIce({ iceParameters });
			}

			if (this._recvTransport)
			{
				const iceParameters = await this._protoo.request('restartIce',{ transportId: this._recvTransport.id });
				await this._recvTransport.restartIce({ iceParameters });
			}
			
			this.dispatchEvent({
				type : 'icerestart'
			});
		}
		catch (error)
		{
			console.error('restartIce() | failed:%o', error);

			this.dispatchEvent({
				type : 'error',
				text : 'Unable to communicate properly with the server. This often means your network is blocking the connection.'
			});
		}

		this.me.restartIceInProgress = false;
		this.dispatchEvent({
			type: 'restarticeprogress',
			flag: false
		});
		
	}

	async setMaxSendingSpatialLayer(spatialLayer)
	{
		try
		{
			if (this._webcamProducer)
				await this._webcamProducer.setMaxSpatialLayer(spatialLayer);
			else if (this._shareProducer)
				await this._shareProducer.setMaxSpatialLayer(spatialLayer);
		}
		catch (error)
		{
			console.error('setMaxSendingSpatialLayer() | failed:%o', error);

			this.dispatchEvent(
			{
				type : 'error',
				minor: true,
				text : 'Error setting max sending video spatial layer: ' + error
			});
		}
	}

	async setConsumerPreferredLayers(consumerId, spatialLayer, temporalLayer)
	{
		try
		{
			await this._protoo.request('setConsumerPreferredLayers', { consumerId, spatialLayer, temporalLayer });
			
			var consumer = this.consumers[consumerId];
			consumer.spatialLayer = spatialLayer;
			consumer.temporalLayer = temporalLayer;
			
			this.dispatchEvent(
			{
				type : 'consumerpreferredlayers',
				consumer
			});
		}
		catch (error)
		{
			console.error('setConsumerPreferredLayers() | failed:%o', error);

			this.dispatchEvent(
			{
				type : 'error',
				minor: true,
				text : 'Error setting Consumer preferred layers: ' + error
			});
		}
	}

	async setConsumerPriority(consumerId, priority)
	{
		try
		{
			await this._protoo.request('setConsumerPriority', { consumerId, priority });
			var consumer = this.consumers[consumerId];
			consumer.priority = priority;
			this.dispatchEvent(
			{
				type : 'consumerpriority',
				consumer
			});
		}
		catch (error)
		{
			console.error('setConsumerPriority() | failed:%o', error);

			this.dispatchEvent(
			{
				type : 'error',
				minor: true,
				text : 'Error setting Consumer priority: ' + error
			});
		}
	}

	async requestConsumerKeyFrame(consumerId)
	{
		try
		{
			await this._protoo.request('requestConsumerKeyFrame', { consumerId });
			
			this.dispatchEvent({
				type: 'keyframerequested'
			});
		}
		catch (error)
		{
			console.error('requestConsumerKeyFrame() | failed:%o', error);

			this.dispatchEvent(
			{
				type : 'error',
				minor: true,
				text : 'Error requesting key frame for Consumer: ' + error
			});
		}
	}
	
	async getSendTransportRemoteStats()
	{
		if (!this._sendTransport)
			return;

		return this._protoo.request('getTransportStats', { transportId: this._sendTransport.id });
	}

	async getRecvTransportRemoteStats()
	{
		if (!this._recvTransport)
			return;

		return this._protoo.request('getTransportStats', { transportId: this._recvTransport.id });
	}

	async getAudioRemoteStats()
	{
		if (!this._micProducer)
			return;

		return this._protoo.request('getProducerStats', { producerId: this._micProducer.id });
	}

	async getVideoRemoteStats()
	{
		const producer = this._webcamProducer || this._shareProducer;

		if (!producer)
			return;

		return this._protoo.request('getProducerStats', { producerId: producer.id });
	}

	async getConsumerRemoteStats(consumerId)
	{
		const consumer = this._consumers.get(consumerId);

		if (!consumer)
			return;

		return this._protoo.request('getConsumerStats', { consumerId });
	}
	
	async getSendTransportLocalStats()
	{
		if (!this._sendTransport)
			return;

		return this._sendTransport.getStats();
	}

	async getRecvTransportLocalStats()
	{
		if (!this._recvTransport)
			return;

		return this._recvTransport.getStats();
	}

	async getAudioLocalStats()
	{
		if (!this._micProducer)
			return;

		return this._micProducer.getStats();
	}

	async getVideoLocalStats()
	{
		const producer = this._webcamProducer || this._shareProducer;

		if (!producer)
			return;

		return producer.getStats();
	}

	async getConsumerLocalStats(consumerId)
	{
		const consumer = this._consumers.get(consumerId);

		if (!consumer)
			return;

		return consumer.getStats();
	}

	async applyNetworkThrottle({ uplink, downlink, rtt, secret })
	{
		try
		{
			await this._protoo.request(
				'applyNetworkThrottle',
				{ uplink, downlink, rtt, secret });
		}
		catch (error)
		{
			console.error('applyNetworkThrottle() | failed:%o', error);

			this.dispatchEvent(
			{
				type : 'error',
				minor: true,
				text : 'Error applying network throttle: ' + error
			});
		}
	}

	async resetNetworkThrottle({ silent = false, secret })
	{
		try
		{
			await this._protoo.request('resetNetworkThrottle', { secret });
		}
		catch (error)
		{
			if (!silent)
			{
				console.error('resetNetworkThrottle() | failed:%o', error);

				this.dispatchEvent(
				{
					type : 'error',
					minor: true,
					text : 'Error resetting network throttle: ' + error
				});
			}
		}
	}
	
	async _joinRoom()
	{
		if(this._closed){
			return;
		}
		
		try
		{
			this._mediasoupDevice = new mediasoup.Device(
				{
					handlerName : ''
				});

			const routerRtpCapabilities = await this._protoo.request('getRouterRtpCapabilities');

			await this._mediasoupDevice.load({ routerRtpCapabilities });
			
			if (this._produce)
			{
				// NOTE: Stuff to play remote audios due to browsers' new autoplay policy.
				//
				// Just get access to the mic and DO NOT close the mic track for a while.
				// Super hack!
				{
					var stream = null;
					
					try{
						stream = await global.navigator.mediaDevices.getUserMedia({ audio: true });
					}catch(e){
						console.log(e);
						this.dispatchEvent(
						{
							type : 'error',
							text : <div>
								<p>
									{`Unable to use your microphone - make sure you allow the microphone prompt. To try again, please refresh the page.`}
								</p>
								<p>
									{`If the prompt still doesn't appear, you may need to restart your browser and double check that you have a microphone plugged in.`}
								</p>
							</div>
						});
						return;
					}
					
					const audioTrack = stream.getAudioTracks()[0];

					audioTrack.enabled = false;
					this._permAudioTrack = audioTrack;
					setTimeout(() => audioTrack.stop(), 120000);
				}

				// Create mediasoup Transport for sending (unless we don't want to produce).
				const transportInfo = await this._protoo.request(
					'createWebRtcTransport',
					{
						forceTcp         : this._forceTcp,
						producing        : true,
						consuming        : false,
						sctpCapabilities : this._useDataChannel
							? this._mediasoupDevice.sctpCapabilities
							: undefined
					});

				const {
					id,
					iceParameters,
					iceCandidates,
					dtlsParameters,
					sctpParameters
				} = transportInfo;

				this._sendTransport = this._mediasoupDevice.createSendTransport(
					{
						id,
						iceParameters,
						iceCandidates,
						dtlsParameters,
						sctpParameters,
						iceServers             : [],
						proprietaryConstraints : PC_PROPRIETARY_CONSTRAINTS
					});

				this._sendTransport.on(
					'connect', ({ dtlsParameters }, callback, errback) => // eslint-disable-line no-shadow
					{
						this._protoo.request(
							'connectWebRtcTransport',
							{
								transportId : this._sendTransport.id,
								dtlsParameters
							})
							.then(callback)
							.catch(errback);
					});

				this._sendTransport.on(
					'produce', async ({ kind, rtpParameters, appData }, callback, errback) =>
					{
						try
						{
							// eslint-disable-next-line no-shadow
							const { id } = await this._protoo.request(
								'produce',
								{
									transportId : this._sendTransport.id,
									kind,
									rtpParameters,
									appData
								});

							callback({ id });
						}
						catch (error)
						{
							errback(error);
						}
					});

				this._sendTransport.on('producedata', async (
					{
						sctpStreamParameters,
						label,
						protocol,
						appData
					},
					callback,
					errback
				) =>
				{
					try
					{
						// eslint-disable-next-line no-shadow
						const { id } = await this._protoo.request(
							'produceData',
							{
								transportId : this._sendTransport.id,
								sctpStreamParameters,
								label,
								protocol,
								appData
							});

						callback({ id });
					}
					catch (error)
					{
						errback(error);
					}
				});
			}

			// Create mediasoup Transport for sending (unless we don't want to consume).
			if (this._consume)
			{
				const transportInfo = await this._protoo.request(
					'createWebRtcTransport',
					{
						forceTcp         : this._forceTcp,
						producing        : false,
						consuming        : true,
						sctpCapabilities : this._useDataChannel
							? this._mediasoupDevice.sctpCapabilities
							: undefined
					});
				
				if(this._closed){
					return;
				}
				
				const {
					id,
					iceParameters,
					iceCandidates,
					dtlsParameters,
					sctpParameters
				} = transportInfo;

				this._recvTransport = this._mediasoupDevice.createRecvTransport(
					{
						id,
						iceParameters,
						iceCandidates,
						dtlsParameters,
						sctpParameters,
						iceServers : []
					});

				this._recvTransport.on(
					'connect', ({ dtlsParameters }, callback, errback) => // eslint-disable-line no-shadow
					{
						
						if(this._closed){
							return;
						}
						
						this._protoo.request(
							'connectWebRtcTransport',
							{
								transportId : this._recvTransport.id,
								dtlsParameters
							})
							.then(callback)
							.catch(errback);
					});
			}
			
			if(this._closed){
				return;
			}
			
			// Join now into the room.
			// NOTE: Don't send our RTP capabilities if we don't want to consume.
			const { peers, peerId } = await this._protoo.request(
				'join',
				{
					device          : this.device,
					filters			: this.filters,
					rtpCapabilities : this._consume
						? this._mediasoupDevice.rtpCapabilities
						: undefined,
					sctpCapabilities : this._useDataChannel && this._consume
						? this._mediasoupDevice.sctpCapabilities
						: undefined
				});
			
			this.me.peerId = peerId;
			
			this.room.state = 'connected';
			this.dispatchEvent({type: 'roomupdate', room: this.room});
			
			for (const peer of peers)
			{
				this.addPeer({ ...peer, consumers: [], dataConsumers: [] });
			}
			
			// Enable mic/webcam.
			if (this._produce)
			{
				// Set our media capabilities.
				
				this.mediaCapabilities = {
					canSendMic    : this._mediasoupDevice.canProduce('audio'),
					canSendWebcam : this._mediasoupDevice.canProduce('video')
				};
				
				this.me.canSendMic = this.mediaCapabilities.canSendMic;
				this.me.canSendWebcam = this.mediaCapabilities.canSendWebcam;
				
				this.dispatchEvent({
					type: 'mediacapabilities',
					mediaCapabilities: this.mediaCapabilities
				});
				
				this.enableMic();
				
				// If we know for sure it's a busy meeting, or will likely be a busy meeting
				// don't turn the cam on straight away.
				var busyMeeting = this.isBusy();
				
				if(!busyMeeting){
					this.enableWebcam();
				}

				this._sendTransport.on('connectionstatechange', (connectionState) =>
				{
					if (connectionState === 'connected')
					{
						this.enableChatDataProducer();
						this.enableBotDataProducer();
					}
				});
			}
		}
		catch (error)
		{
			console.error('_joinRoom() failed:%o', error);

			this.dispatchEvent(
			{
				type : 'error',
				text : `An error occurred whilst trying to connect to the meeting. To connect, please make sure you have at least a microphone and are using an up-to-date web browser.`
			});

			this.close();
		}
	}
	
	isBusy()
	{
		return (this.peers && this.peers.length > CAM_MAX) || this._busyMeeting;
	}
	
	async _updateWebcams()
	{
		// Reset the list.
		this._webcams = new Map();
		
		const devices = await global.navigator.mediaDevices.enumerateDevices();

		for (const device of devices)
		{
			if (device.kind !== 'videoinput')
				continue;

			this._webcams.set(device.deviceId, device);
		}

		const array = Array.from(this._webcams.values());
		const len = array.length;
		const currentWebcamId =
			this._webcam.device ? this._webcam.device.deviceId : undefined;
		
		if (len === 0)
			this._webcam.device = null;
		else if (!this._webcams.has(currentWebcamId))
			this._webcam.device = array[0];
		
		var flag = this._webcams.size > 1;
		this.me.canChangeWebcam = flag;
		
		this.dispatchEvent({
			type: 'canchangewebcam',
			flag
		});
	}
	
	_getWebcamType(device)
	{
		if (/(back|rear)/i.test(device.label))
		{
			return 'back';
		}
		else
		{
			return 'front';
		}
	}
	
	async _pauseConsumer(consumer)
	{
		if (consumer.paused)
			return;

		try
		{
			await this._protoo.request('pauseConsumer', { consumerId: consumer.id });

			consumer.pause();
			
			var c = this.consumersById[consumer.id];
			c.locallyPaused = true;
			this.dispatchEvent({
				type: 'consumerpaused',
				consumer: c
			});
		}
		catch (error)
		{
			console.error('_pauseConsumer() | failed:%o', error);
			
			this.dispatchEvent(
			{
				type : 'error',
				minor: true,
				text : 'Error pausing Consumer: ' + error
			});
		}
	}

	async _resumeConsumer(consumer)
	{
		if (!consumer.paused)
			return;

		try
		{
			await this._protoo.request('resumeConsumer', { consumerId: consumer.id });

			consumer.resume();
			
			var c = this.consumersById[consumer.id];
			c.locallyPaused = false;
			this.dispatchEvent({
				type: 'consumerresumed',
				consumer: c
			});
		}
		catch (error)
		{
			console.error('_resumeConsumer() | failed:%o', error);

			this.dispatchEvent(
			{
				type : 'error',
				minor: true,
				text : 'Error resuming Consumer: ' + error
			});
		}
	}
	
	async _getExternalVideoStream()
	{
		if (this._externalVideoStream)
			return this._externalVideoStream;

		if (this._externalVideo.readyState < 3)
		{
			await new Promise((resolve) => (
				this._externalVideo.addEventListener('canplay', resolve)
			));
		}

		if (this._externalVideo.captureStream)
			this._externalVideoStream = this._externalVideo.captureStream();
		else if (this._externalVideo.mozCaptureStream)
			this._externalVideoStream = this._externalVideo.mozCaptureStream();
		else
			throw new Error('video.captureStream() not supported');

		return this._externalVideoStream;
	}
}

var activeRings = {};

export function ringReject(slug, userId){
	if(activeRings[slug]){
		var ring = activeRings[slug];
		ring.userIds = ring.userIds.filter(id => id != userId); 
	}
}

export function sendRing(slug, mode, userId){
	if(webSocket.Writer){
		// Bolt mode
		var writer = new webSocket.Writer(40);
		writer.writeUtf8(slug);
		writer.writeByte(mode);
		writer.writeUInt32(parseInt(userId));
		webSocket.send(writer);
	}else{
		// JSON mode
		webSocket.send({
			type: 'huddle/ring',
			slug,
			mode,
			userId
		});
	}
}

// TODO: This will be deprecated when huddle uses the room presence setup.
export function joinHuddle(huddleId, huddleStatus){
	if(webSocket.Writer){
		// Bolt mode
		var writer = new webSocket.Writer(41);
		writer.writeUInt32(parseInt(huddleId));
		writer.writeByte(huddleStatus ? 1 : 0);
		webSocket.send(writer);
	}else{
		// JSON mode
		webSocket.send({
			type: 'huddle/join',
			huddleId,
			status: huddleStatus ? 1 : 0
		});
	}
}

export function ring(userIds, slug){
	if(activeRings[slug]){
		return activeRings[slug];
	}
	
	var ring = {
		userIds
	};
	
	ring.i = setInterval(() => {
		ring.userIds.forEach(userId => {
			sendRing(slug, 1, userId)
		});
	}, 1000);
	
	ring.stop = () => {
		delete activeRings[slug];
		clearInterval(ring.i);
	};
	
	activeRings[slug] = ring;
	return ring;
}