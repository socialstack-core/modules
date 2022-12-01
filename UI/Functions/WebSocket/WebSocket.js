import store from 'UI/Functions/Store';
import {expandIncludes} from 'UI/Functions/WebRequest';

function websocketHandler(opts){
opts = opts || {};

if(opts.reconnectOnUserChange){
	var __user = null;
	var waitMode = 0;

	document.addEventListener('xsession', (e) => {
		var {user, loadingUser} = e.state;
		
		if(!waitMode){
			if(loadingUser){
				waitMode=1;
				loadingUser.then(state => {
					waitMode=2;
					__user = state.user;
				});

				return;
			}else{
				waitMode=2;
			}
		}
		
		if(waitMode==2 && user != __user){
			__user = user;
			if(ws){
				try{
					// setting ws to null prevents the close handler from running here.
					var _ws = ws;
					ws=null;
					_ws.close();
					setTimeout(() => {
						start();
					}, 100);
				}catch(e){
					console.log(e);
				}
			}
		}
	});
}

/*
* Handles setting up the websocket.
*/
var started = false;
var typeCount = 0;
var ws;
var onConnectedMessages = [];
var messageTypes = {};

function informStatus(state){
	tellAllHandlers({type: 'status', connected: state});
}

function tellAllHandlers(msg){
	msg.all = true;
	for(var typeName in messageTypes){
		var handlers = messageTypes[typeName];
		for(var i=0;i<handlers.length;i++){
			handlers[i].method(msg);
		}
	}
}

/*
* Attempts to keep the websocket link alive. If it drops, waits for 5 seconds and goes again.
*/
function start(){
	if(ws){
		return;
	}
	
    connect();
}

function goAgain(){
    setTimeout(function(){
        // Try again
		ws=null;
        start();
    }, 5000);
}

var pingInterval = null;

function setPing(){
	
	if(pingInterval){
		return;
	}
	
	pingInterval = setInterval(function(){
		
		// Check if the server is still there every 30s.
		// If this fails the socket disconnects and we get informed about it that way.
		if(ws && ws.readyState == WebSocket.OPEN)
		{
			// Opcode 1 (ping, no pong):
			ws.send(Uint8Array.from([1]));
		}
		
	}, 30000);
	
}

var te8 = null;
var de8 = null;

class Reader{
	constructor(bytes){
		this.reset(bytes);
	}
	
	reset(bytes){
		this.bytes = bytes.buffer ? bytes : new Uint8Array(bytes);
		this.i = 0;
		this.view = new DataView(this.bytes.buffer);
	}
	
	next(){
		return this.bytes[this.i++];
	}
	
	readCompressed()
	{
		var first = this.next();
		switch (first)
		{
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
				return first;
		}
	}
	
	readBytes(size){
		var set = this.bytes.subarray(this.i, this.i + size);
		this.i+=size;
		return set;
	}
	
	readByte()
	{
		return this.next();
	}
	
	readInt16()
	{
		var val = this.view.getInt16(this.i, true);
		this.i+=2;
		return val;
	}

	readUInt16()
	{
		return (this.next()) | (this.next() << 8);
	}
	
	/*
	readInt24()
	{
		// todo: use views
		return this.readUInt24();
	}
	*/
	
	readUInt24()
	{
		return (this.next()) | (this.next() << 8) | (this.next() << 16);
	}

	readInt32()
	{
		var val = this.view.getInt32(this.i, true);
		this.i+=4;
		return val;
	}

	readUInt32()
	{
		var val = this.view.getUint32(this.i, true);
		this.i+=4;
		return val;
	}

	readInt64()
	{
		var val = this.view.getBigInt64(this.i, true);
		this.i+=8;
		return val;
	}

	readUInt64()
	{
		var val = this.view.getBigUint64(this.i, true);
		this.i+=8;
		return val;
	}

	readUtf8(){
		var size = this.readCompressed();
		return this.readUtf8SizedPlus1(size);
	}
	
	readUtf8SizedPlus1(size){
		if(size == 0){
		 	return null;
		}
		var bytesArr = this.readBytes(size - 1);
		return this.bytesToUtf8(bytesArr);
	}
	
	bytesToUtf8(bytes){
		if(!de8){
			de8 = new TextDecoder("utf-8");
		}
		return de8.decode(bytes);
	}
	
}

class Writer{
	
	constructor(opcode){
		this.bytes = [];
		
		if(opcode){
			this.writeCompressed(opcode);
			this.writeUInt32(0); // payload size
		}
	}
	
	writeView(dv) {
		var n = new Uint8Array(dv.buffer);
        for(var i=0;i<n.length;i++){
           this.bytes.push(n[i]);
        }
	}

	writeUInt32(value){
		var dataView =  new DataView(new ArrayBuffer(4));
		dataView.setUint32(0, value, true);
		this.writeView(dataView);
	}
	
	writeUInt64(value){
		var dataView =  new DataView(new ArrayBuffer(8));
		dataView.setBigUint64(0, value, true);
		this.writeView(dataView);
	}
	
	writeUInt16(value){
		var dataView =  new DataView(new ArrayBuffer(2));
		dataView.setUint16(0, value, true);
		this.writeView(dataView);
	}
	
	writeCompressed(value){
		var b = this.bytes;
		
		if (value< 251){
			
			// Single byte:
			b.push(value);
			
		}else if (value <= 65535){
			
			// Status 251 for a 2 byte num:
			b.push(251);
			b.push(value & 255);
			b.push((value>>8) & 255);
			
		}else if (value < 16777216){
			
			// Status 252 for a 3 byte num:
			b.push(252);
			b.push(value & 255);
			b.push((value>>8) & 255);
			b.push((value>>16) & 255);
			
		}else if(value <= 4294967295){
			
			// Status 253 for a 4 byte num:
			b.push(253);
			this.writeUInt32(value);
			
		}else{
			
			// Status 254 for an 8 byte num:
			b.push(254);
			this.writeUInt64(value);
			
		}
	}
	
	writeByte(i){
		this.bytes.push(i);
	}
	
	writeUtf8(str){
		var buf = this.getUtf8Bytes(str);
		if(buf === null){
			this.writeByte(0);
			return;
		}
		this.writeCompressed(buf.length + 1);
		for(var i=0;i<buf.length;i++){
			this.bytes.push(buf[i]);
		}
	}
	
	writeBytes(arr){
		for(var i=0;i<arr.length;i++){
			this.bytes.push(arr[i]);
		}
	}
	
	getUtf8Bytes(str){
		if(str === null){
			return null;
		}
		if(!te8){
			te8 = new TextEncoder("utf-8");
		}
		return te8.encode(str);
	}
	
	toBuffer(){
		return Uint8Array.from(this.bytes);
	}
	
	setSize(){
		var b = this.bytes;
		
		// Size of the byte array minus opcode (assumed 1 byte) and the payload size itself
		var opcodeSize = 1;
		var value = b.length - (4 + opcodeSize);
		
		b[opcodeSize] = value & 255;
		b[opcodeSize + 1] = (value>>8) & 255;
		b[opcodeSize + 2] = (value>>16) & 255;
		b[opcodeSize + 3] = (value>>24) & 255;
	}
}

function getAsBuffer(obj){
	if(obj && obj.toBuffer){
		// It was already a writer. Just set its payload size:
		obj.setSize();
		return obj.toBuffer();
	}
	var json = JSON.stringify(obj);
	
	var w = new Writer();
	w.writeByte(2); // Wrapped JSON
	w.writeUtf8(json);
	
	return w.toBuffer();
}

// Connects the websocket
function connect(){
	
	if(typeof WebSocket === "undefined"){
		return;
	}
	
	// Fire up the websocket:
	var sk = new WebSocket(opts.url);
	sk.binaryType = "arraybuffer";
	ws = sk;
	setPing();
	
	sk.addEventListener("open", () => {
		if(ws != sk){
			return;
		}
		informStatus(true);
		var msgs = onConnectedMessages;
		onConnectedMessages = [];
		
		if(global.storedToken){
			// Auth msg:
			sk.send(getAsBuffer({
				type: 'Auth',
				token: store.get('context')
			}));
		}
		
		for(var i=0;i<msgs.length;i++){
			sk.send(msgs[i]);
		}
		
		var set = [];
		
		for(var name in messageTypes){
			if(name == '_all_'){
				continue;
			}
			
			var s = messageTypes[name];
			for(var type in s){
				var entry = s[type];
				if(entry.register === false){
					continue;
				}
				set.push({n: name, id: entry.id, ci: entry.customId, f: entry.onFilter});
			}
			
		}
		
		if(set.length){
			sk.send(getAsBuffer({type: '+*', set}));
		}
	});
	
	function onClose(){
		if(ws!=sk){
			return;
		}
		ws=null;
		informStatus(false);
		goAgain();
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
		var opcode = r.readCompressed();
		var handler = _opcodes[opcode];
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

function send(msg){
	start();
	
	msg = getAsBuffer(msg);
	if(ws.readyState == WebSocket.OPEN){
		ws.send(msg);
	}else{
		onConnectedMessages.push(msg);
	}
}

var refId = 1;

function addEventListener (type, method, id, onFilter, register){
	
	if(id !== undefined && typeof id != 'number'){
		// Change the 3rd arg to a room ID.
		throw new Error('Old websocket event listener usage detected.');
	}
	
	if(!method){
		method = type;
		type = '_all_';
	}
	
	if(type != '_all_'){
		start();
	}
	
	type = type.toLowerCase();
	
	// Already got this listener?
	// If so, must re-add it, essentially re-registering the ID.
	// Note that we assume the user called this because it changed - we don't check for non-change here.
	var entry;
	
	if(messageTypes[type]){
		entry = messageTypes[type].find(mf => mf.method == method);
		if(entry){
			// Actually an update of existing one
			entry.id = id;
			entry.onFilter = onFilter;
		}else{
			entry = {method, id, customId: refId++, onFilter, register};
			messageTypes[type].push(entry);
		}
	}else{
		typeCount++;
		entry = {method,id, customId: refId++, onFilter, register};
		messageTypes[type] = [entry];
	}
	
	if(type == '_all_'){
		return;
	}
	
	if(register === false){
		return;
	}
	
	var msg = {type: '+', n: type, id, ci: entry.customId, f: entry.onFilter};
	
	if(!ws){
		connect();
	}
	
	if(ws && ws.readyState == WebSocket.OPEN){
		ws.send(getAsBuffer(msg));
	}
}

function removeEventListener(type, method) {
	if(!method){
		method = type;
		type = '_all_';
	}
	
	type = type.toLowerCase();
	
	if(!messageTypes[type]){
		return;
	}
	
	var entry = messageTypes[type].find(mf => mf.method == method);
	if(!entry){
		return;
	}
	
	messageTypes[type] = messageTypes[type].filter(a => a != entry);
	
	if(ws && ws.readyState == WebSocket.OPEN && !(entry.register === false)){
		ws.send(getAsBuffer({type: '-', ci: entry.customId}));
	}
	
	if(!messageTypes[type].length){
		typeCount--;
		
		if(typeCount<=0){
			typeCount=0;
		}
		
		/*
			Tends to lead to a scenario where the socket 
			disconnects then very shortly after reconnects - a timer would help debounce that
			
			ws.addEventListener("close", e => {e.stopPropagation()});
			ws.close();
			ws=null;
		}
		*/
		
		delete messageTypes[type];
	}
}

function getSocket(){
	start();
	return ws;
}

var _opcodes = {};

function registerOpcode(id, onReceive, size){
	var oc = {onReceive, size: size === undefined ? true : size, unregister: () => {
		delete _opcodes[id];
	}};
	_opcodes[id] = oc;
	return oc;
}

function receiveJson(json, method){
	var message = JSON.parse(json);
	if(!message){
		return;
	}
	
	if(message.host){
		if(message.reload && global.location && global.location.reload){
			global.location.reload(true);
		}
		return;
	}
	
	if(message.all){
		tellAllHandlers(message);
	}else if(message.result && message.result.type){
		var handlers = messageTypes[message.result.type.toLowerCase()];
		
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
	
	if(opts.globalMessage){
		var e = document.createEvent('Event');
		e.initEvent('websocketmessage', true, true);
		e.message = message;
		
		// Dispatch the event:
		document.dispatchEvent(e);
	}
}

function syncUpdate(method, reader){
	var size = reader.readUInt32();
	var bytesArr = reader.readBytes(size);
	if(!de8){
		de8 = new TextDecoder("utf-8");
	}
	var json = de8.decode(bytesArr);
	receiveJson(json, method);
}

function close(){
	// Permanently close the ws
	if(!ws){
		return;
	}
	var socket = ws;
	ws = null; // blocks auto reconnect
	socket.close();
}

if(opts.addDefaults){
	registerOpcode(21, reader => syncUpdate('create', reader), false);
	registerOpcode(22, reader => syncUpdate('update', reader), false);
	registerOpcode(23, reader => syncUpdate('delete', reader), false);

	registerOpcode(8, r => {
		var payloadSize = r.readUInt32();
		// Skip 2 compressed numbers (the network room type + id)
		// The rest is the actual payload to do something with.
		
		r.viaNetRoom = true;
		r.roomType = r.readCompressed();
		r.roomId = r.readCompressed();
		
		// read OC:
		var opcode = r.readCompressed();
		
		var handler = _opcodes[opcode];
		if(handler){
			handler.onReceive(r);
		}
		
	}, false);
}

return {
	registerOpcode,
	getSocket,
	close,
    addEventListener,
	removeEventListener,
	send,
	Writer,
	Reader,
	start,
	syncUpdate,
	receiveJson
};

};

var dflt = global.wsUrl ? websocketHandler({reconnectOnUserChange: 1, addDefaults: 1, url: global.wsUrl, globalMessage: 1}) : {};
dflt.create = websocketHandler;
export default dflt;

window.addEventListener('load', () => dflt.start && dflt.start());