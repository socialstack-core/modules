import store from 'UI/Functions/Store';
import { User } from 'Api/User';
import {expandIncludes} from 'UI/Functions/WebRequest';

var te8: TextEncoder | undefined;
var de8: TextDecoder | undefined;

type WebSocketHandlerOpts = {
	reconnectOnUserChange?: boolean,
	addDefaults?: boolean,
	url: string,
	globalMessage?: boolean,
	startOnLoad?: boolean
};

type OpCodeHandler = {
	onReceive: (msg: Reader) => void,
	size: boolean,
	unregister: () => void
};

type WebsocketMessageEvent = Event & {
	message: any
};

class Reader {
	// @ts-ignore
	view: DataView;
	// @ts-ignore
	bytes: Uint8Array;
	i: int;

	viaNetRoom?: boolean;
	roomType?: int;
	roomId?: int;

	constructor(bytes: Uint8Array) {
		this.i = 0 as int;
		this.reset(bytes);
	}

	reset(bytes: Uint8Array) {
		this.bytes = bytes.buffer ? bytes : new Uint8Array(bytes);
		this.i = 0 as int;
		this.view = new DataView(this.bytes.buffer);
	}

	next() {
		return this.bytes[this.i++];
	}

	readCompressed() {
		var first = this.next();
		switch (first) {
			case 251:
				// 2 bytes:
				return this.readUInt16();
			case 252:
				// 3 bytes:
				return this.readUInt24();
			case 253:
				// 4 bytes:
				return this.readUInt32();
			case 254:
				// 8 bytes:
				return this.readUInt64();
			default:
				return first as uint;
		}
	}

	readBytes(size: int) {
		var next = this.i + size as int;
		var set = this.bytes.subarray(this.i, next);
		this.i = next;
		return set;
	}

	readByte() {
		return this.next() as int;
	}

	readInt16() {
		var val = this.view.getInt16(this.i, true);
		this.i = (this.i + 2) as int;
		return val as int;
	}

	readUInt16() {
		return ((this.next()) | (this.next() << 8)) as int;
	}

	/*
	readInt24()
	{
		// todo: use views
		return this.readUInt24();
	}
	*/

	readUInt24() {
		return ((this.next()) | (this.next() << 8) | (this.next() << 16)) as int;
	}

	readInt32() {
		var val = this.view.getInt32(this.i, true);
		this.i = (this.i + 4) as int;
		return val as int;
	}

	readUInt32() {
		var val = this.view.getUint32(this.i, true);
		this.i = (this.i + 4) as int;
		return val as uint;
	}

	readInt64() {
		var val = this.view.getBigInt64(this.i, true);
		this.i = (this.i + 8) as int;
		return val;
	}

	readUInt64() {
		var val = this.view.getBigUint64(this.i, true);
		this.i = (this.i + 8) as int;
		return val;
	}

	readUtf8() {
		var size = this.readCompressed();
		return this.readUtf8SizedPlus1(size as int);
	}

	readUtf8SizedPlus1(size: int) {
		if (size == 0) {
			return null;
		}
		var bytesArr = this.readBytes((size - 1) as int);
		return this.bytesToUtf8(bytesArr);
	}

	bytesToUtf8(bytes: Uint8Array) {
		if (!de8) {
			de8 = new TextDecoder("utf-8");
		}
		return de8.decode(bytes);
	}

}

class Writer {
	bytes: int[];
	constructor(opcode?: int) {
		this.bytes = [];

		if (opcode) {
			this.writeCompressed(opcode);
			this.writeUInt32(0 as uint); // payload size
		}
	}

	writeView(dv: DataView) {
		var n = new Uint8Array(dv.buffer);
		for (var i = 0; i < n.length; i++) {
			this.bytes.push(n[i] as int);
		}
	}

	writeUInt32(value: uint) {
		var dataView = new DataView(new ArrayBuffer(4));
		dataView.setUint32(0, value, true);
		this.writeView(dataView);
	}

	writeUInt64(value: bigint) {
		var dataView = new DataView(new ArrayBuffer(8));
		dataView.setBigUint64(0, value, true);
		this.writeView(dataView);
	}

	writeUInt16(value: uint) {
		var dataView = new DataView(new ArrayBuffer(2));
		dataView.setUint16(0, value, true);
		this.writeView(dataView);
	}

	writeCompressed(value: uint) {
		var b = this.bytes;

		if (value < 251) {

			// Single byte:
			b.push(value);

		} else if (value <= 65535) {

			// Status 251 for a 2 byte num:
			b.push(251 as int);
			b.push((value & 255) as int);
			b.push(((value >> 8) & 255) as int);

		} else if (value < 16777216) {

			// Status 252 for a 3 byte num:
			b.push(252 as int);
			b.push((value & 255) as int);
			b.push(((value >> 8) & 255) as int);
			b.push(((value >> 16) & 255) as int);

		} else if (value <= 4294967295) {

			// Status 253 for a 4 byte num:
			b.push(253 as int);
			this.writeUInt32(value);

		} else {

			// Status 254 for an 8 byte num:
			b.push(254 as int);
			this.writeUInt64(BigInt(value));

		}
	}

	writeByte(i: int) {
		this.bytes.push(i);
	}

	writeUtf8(str: string) {
		var buf = this.getUtf8Bytes(str);
		if (buf === null) {
			this.writeByte(0 as int);
			return;
		}
		this.writeCompressed((buf.length + 1) as int);
		for (var i = 0; i < buf.length; i++) {
			this.bytes.push(buf[i] as int);
		}
	}

	writeBytes(arr: ArrayLike<int>) {
		for (var i = 0; i < arr.length; i++) {
			this.bytes.push(arr[i]);
		}
	}

	getUtf8Bytes(str: string) {
		if (str === null) {
			return null;
		}
		if (!te8) {
			te8 = new TextEncoder();
		}
		return te8.encode(str);
	}

	toBuffer() {
		return Uint8Array.from(this.bytes);
	}

	setSize() {
		var b = this.bytes;

		// Size of the byte array minus opcode (assumed 1 byte) and the payload size itself
		var opcodeSize = 1;
		var value = b.length - (4 + opcodeSize);

		b[opcodeSize] = (value & 255) as int;
		b[opcodeSize + 1] = ((value >> 8) & 255) as int;
		b[opcodeSize + 2] = ((value >> 16) & 255) as int;
		b[opcodeSize + 3] = ((value >> 24) & 255) as int;
	}
}

type MessageFilter = (entity: any) => boolean;
type MessageHandlerMethod = (msg: any) => void;

type MessageHandler = {
	method: MessageHandlerMethod,
	register: boolean,
	customId?: uint,
	onFilter?: MessageFilter,
	id?: uint
};

class WebSocketHandler {

	started = false;
	typeCount = 0;
	ws: WebSocket | undefined;
	onConnectedMessages: ArrayBufferLike[] = [];
	messageTypes: Record<string, MessageHandler[]> = {};
	pingInterval: number | undefined;
	_opcodes: Record<uint, OpCodeHandler> = {};
	opts: WebSocketHandlerOpts;
	refId = 1;

	constructor(srcOpts?: WebSocketHandlerOpts) {
		const opts = srcOpts || {} as WebSocketHandlerOpts;
		this.opts = opts;

		if (opts.reconnectOnUserChange) {
			var __user: User | undefined;
			var waitMode = 0;

			document.addEventListener('xsession', (e) => {
				// @ts-ignore
				var { user, loadingUser } = e.detail.state;

				if (!waitMode) {
					if (loadingUser) {
						waitMode = 1;
						loadingUser.then((state: Session) => {
							waitMode = 2;
							__user = state.user;
						});

						return;
					} else {
						waitMode = 2;
					}
				}

				if (waitMode == 2 && user != __user) {
					__user = user;
					if (this.ws) {
						try {
							// setting ws to null prevents the close handler from running here.
							var _ws = this.ws;
							this.ws = undefined;
							_ws.close();
							setTimeout(() => {
								this.start();
							}, 100);
						} catch (e) {
							console.log(e);
						}
					}
				}
			});
		}

		if (opts.addDefaults) {
			this.registerOpcode(21 as int, reader => this.syncUpdate('create', reader), false);
			this.registerOpcode(22 as int, reader => this.syncUpdate('update', reader), false);
			this.registerOpcode(23 as int, reader => this.syncUpdate('delete', reader), false);

			this.registerOpcode(8 as int, r => {
				var payloadSize = r.readUInt32();
				// Skip 2 compressed numbers (the network room type + id)
				// The rest is the actual payload to do something with.

				r.viaNetRoom = true;
				r.roomType = r.readCompressed() as int;
				r.roomId = r.readCompressed() as int;

				// read OC:
				var opcode = r.readCompressed() as int;

				var handler = this._opcodes[opcode];
				if (handler) {
					handler.onReceive(r);
				}

			}, false);
		}

		if (opts.startOnLoad) {
			window.addEventListener('load', () => this.start());
		}
	}

	informStatus(state: boolean){
		this.tellAllHandlers({type: 'status', connected: state});
	}

	tellAllHandlers(msg: any){
		msg.all = true;
		for(var typeName in this.messageTypes){
			var handlers = this.messageTypes[typeName];
			for(var i=0;i<handlers.length;i++){
				handlers[i].method(msg);
			}
		}
	}

	/*
	* Attempts to keep the websocket link alive. If it drops, waits for 5 seconds and goes again.
	*/
	start(){
		if(this.ws){
			return;
		}
	
		this.connect();
	}

	goAgain(){
		setTimeout(() => {
			// Try again
			this.ws=undefined;
			this.start();
		}, 5000);
	}

	setPing(){
	
		if(this.pingInterval){
			return;
		}
		
		this.pingInterval = setInterval(() => {
		
			// Check if the server is still there every 30s.
			// If this fails the socket disconnects and we get informed about it that way.
			if (this.ws && this.ws.readyState == WebSocket.OPEN)
			{
				// Opcode 1 (ping, no pong):
				this.ws.send(Uint8Array.from([1]));
			}
		
		}, 30000);
	
	}

	getAsBuffer(obj: any){
		if(obj && obj.toBuffer){
			// It was already a writer. Just set its payload size:
			obj.setSize();
			return obj.toBuffer();
		}
		var json = JSON.stringify(obj);
	
		var w = new Writer();
		w.writeByte(2 as int); // Wrapped JSON
		w.writeUtf8(json);
	
		return w.toBuffer();
	}

	/** 
	 * Connects the websocket
	 * */
	connect(){
	
		if(typeof WebSocket === "undefined"){
			return;
		}
	
		// Fire up the websocket:
		var sk = new WebSocket(this.opts.url);
		sk.binaryType = "arraybuffer";
		this.ws = sk;
		this.setPing();
	
		sk.addEventListener("open", () => {
			if (this.ws != sk){
				return;
			}
			this.informStatus(true);
			var msgs = this.onConnectedMessages;
			this.onConnectedMessages = [];
		
			if(window.storedToken){
				// Auth msg:
				sk.send(this.getAsBuffer({
					type: 'Auth',
					token: store.get('context')
				}));
			}
			
			for(var i=0;i<msgs.length;i++){
				sk.send(msgs[i]);
			}
		
			var set = [];
		
			for (var name in this.messageTypes){
				if(name == '_all_'){
					continue;
				}
			
				var s = this.messageTypes[name];
				for(var type in s){
					var entry = s[type];
					if(entry.register === false){
						continue;
					}
					set.push({n: name, id: entry.id, ci: entry.customId, f: entry.onFilter});
				}
			
			}
		
			if(set.length){
				sk.send(this.getAsBuffer({type: '+*', set}));
			}
		});
	
		const onClose = () => {
			if (this.ws!=sk){
				return;
			}
			this.ws=undefined;
			this.informStatus(false);
			this.goAgain();
		}

		sk.addEventListener("close", onClose);
		sk.addEventListener("error", (e) => {
			console.log(e);
			try{
				sk.close();
			}catch(r){}
		});
	
		sk.addEventListener("message", e => {
			// standard bolt message(s)
			var r = new Reader(e.data);
			var opcode = r.readCompressed() as int;
			var handler = this._opcodes[opcode];
			if(handler){
				if(handler.size){
					r.readUInt32();
				}
				handler.onReceive(r);
			}else{
				r.readUInt32();
			}
		});
	}

	send(msg : any){
		this.start();
	
		msg = this.getAsBuffer(msg);
		if (this.ws && this.ws.readyState == WebSocket.OPEN){
			this.ws.send(msg);
		}else{
			this.onConnectedMessages.push(msg);
		}
	}

	addEventListener(type: string, method: MessageHandlerMethod, id?: uint, onFilter?: MessageFilter, register?: boolean) {
	
		if(id !== undefined && typeof id != 'number'){
			// Change the 3rd arg to a room ID.
			throw new Error('Old websocket event listener usage detected.');
		}
	
		if (!method) {
			// @ts-ignore
			method = type;
			type = '_all_';
		}
		
		if(type != '_all_'){
			this.start();
		}
	
		type = type.toLowerCase();
	
		// Already got this listener?
		// If so, must re-add it, essentially re-registering the ID.
		// Note that we assume the user called this because it changed - we don't check for non-change here.
		var entry: MessageHandler | undefined;
	
		if (this.messageTypes[type]){
			entry = this.messageTypes[type].find(mf => mf.method == method);
			if(entry){
				// Actually an update of existing one
				entry.id = id;
				entry.onFilter = onFilter;
			}else{
				entry = { method, id, customId: this.refId++, onFilter, register} as MessageHandler;
				this.messageTypes[type].push(entry);
			}
		}else{
			this.typeCount++;
			entry = { method, id, customId: this.refId++, onFilter, register } as MessageHandler;
			this.messageTypes[type] = [entry];
		}
	
		if(type == '_all_'){
			return;
		}
	
		if(register === false){
			return;
		}
	
		var msg = {type: '+', n: type, id, ci: entry.customId, f: entry.onFilter};
	
		if (!this.ws){
			this.connect();
		}
	
		if (this.ws && this.ws.readyState == WebSocket.OPEN){
			this.ws.send(this.getAsBuffer(msg));
		}
	}

	removeEventListener(type: string, method?: MessageHandlerMethod) {
		if (!method) {
			// @ts-ignore
			method = type;
			type = '_all_';
		}
	
		type = type.toLowerCase();
	
		if (!this.messageTypes[type]){
			return;
		}
	
		var entry = this.messageTypes[type].find(mf => mf.method == method);
		if(!entry){
			return;
		}
	
		this.messageTypes[type] = this.messageTypes[type].filter(a => a != entry);
	
		if (this.ws && this.ws.readyState == WebSocket.OPEN && !(entry.register === false)){
			this.ws.send(this.getAsBuffer({type: '-', ci: entry.customId}));
		}
	
		if (!this.messageTypes[type].length){
			this.typeCount--;
		
			if (this.typeCount<=0){
				this.typeCount=0;
			}
		
			/*
				Tends to lead to a scenario where the socket 
				disconnects then very shortly after reconnects - a timer would help debounce that
			
				ws.addEventListener("close", e => {e.stopPropagation()});
				ws.close();
				ws=null;
			}
			*/
		
			delete this.messageTypes[type];
		}
	}

	getSocket(){
		this.start();
		return this.ws;
	}

	registerOpcode(id: uint, onReceive: (msg: Reader) => void, size?: boolean){
		var oc = {
			onReceive,
			size: size === undefined ? true : size,
			unregister: () => {
				delete this._opcodes[id];
			}
		} as OpCodeHandler;
		this._opcodes[id] = oc;
		return oc;
	}

	receiveJson(json: string, method: string){
		var message = JSON.parse(json);
		if(!message){
			return;
		}
	
		if(message.host){
			if (message.reload && window.location && window.location.reload){
				window.location.reload();
			}
			return;
		}
	
		if(message.all){
			this.tellAllHandlers(message);
		}else if(message.result && message.result.type){
			var handlers = this.messageTypes[message.result.type.toLowerCase()];
		
			expandIncludes(message);
		
			message = {
				entity: message.result,
				method
			};
		
			if(handlers && handlers.length){
				for(var i=0;i<handlers.length;i++){
					handlers[i].method(message);
				}
			}
		}

		if (this.opts.globalMessage) {
			const e = new CustomEvent<any>('websocketmessage', {
				detail: message,
				bubbles: true,
				cancelable: true
			});

			// Dispatch the event:
			document.dispatchEvent(e);
		}
	}

	syncUpdate(method: string, reader: Reader){
		var size = reader.readUInt32();
		var bytesArr = reader.readBytes(size);
		if(!de8){
			de8 = new TextDecoder("utf-8");
		}
		var json = de8.decode(bytesArr);
		this.receiveJson(json, method);
	}

	close(){
		// Permanently close the ws
		if (!this.ws){
			return;
		}
		var socket = this.ws;
		this.ws = undefined; // blocks auto reconnect
		socket.close();
	}
}

var wsUrl = (window as any).wsUrl;

var dflt: WebSocketHandler | undefined = wsUrl && !window.SERVER ? new WebSocketHandler(
	{
		reconnectOnUserChange: true,
		startOnLoad: true,
		addDefaults: true,
		url: wsUrl,
		globalMessage: true
	}) : undefined;

export default dflt;

export { Reader, Writer, WebSocketHandler };