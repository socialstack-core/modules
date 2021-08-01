import store from 'UI/Functions/Store';
import {expandIncludes} from 'UI/Functions/WebRequest';

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
		}else{
			waitMode=2;
			__user = user;
		}
		return;
	}
	
	if(waitMode==2 && user != __user){
		__user = user;
		if(ws){
			console.log('Reconnecting websocket for new user');
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

/*
* Handles setting up the websocket.
*/
var started = false;
var typeCount = 0;
var ws;
var onConnectedMessages = [];

var messageTypes = {
	
};

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

var te8 = new TextEncoder("utf-8");

class Writer{
	
	constructor(){
		this.bytes = [];
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
			b.push(value & 255);
			b.push((value>>8) & 255);
			b.push((value>>16) & 255);
			b.push((value>>24) & 255);
			
		}else{
			
			// Status 254 for an 8 byte num:
			b.push(254);
			b.push(value & 255);
			b.push((value>>8) & 255);
			b.push((value>>16) & 255);
			b.push((value>>24) & 255);
			b.push((value>>32) & 255);
			b.push((value>>40) & 255);
			b.push((value>>48) & 255);
			b.push((value>>56) & 255);
			
		}
	}
	
	writeByte(i){
		this.bytes.push(i);
	}
	
	writeUtf8(str){
		if(str === null){
			this.writeByte(0);
			return;
		}
		var buf = te8.encode(str);
		this.writeCompressed(buf.length + 1);
		for(var i=0;i<buf.length;i++){
			this.bytes.push(buf[i]);
		}
	}
	
	toBuffer(){
		return Uint8Array.from(this.bytes);
	}
}

function getAsBuffer(obj){
	if(obj && obj.toBuffer){
		// It was already a writer.
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
	var sk = new WebSocket(global.wsUrl);
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
				set.push({n: name, f: entry.filter, i: entry.id});
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
	sk.addEventListener("error", onClose);
	
	sk.addEventListener("message", e => {
		var message = JSON.parse(e.data);
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
		}else if(message.type){
			var handlers = messageTypes[message.type];
			
			if(message.entity){
				message.entity = expandIncludes(message.entity);
			}
			
			if(handlers && handlers.length){
				for(var i=0;i<handlers.length;i++){
					handlers[i].method(message);
				}
			}
		}
		
		var e = document.createEvent('Event');
		e.initEvent('websocketmessage', true, true);
		e.message = message;
		
		// Dispatch the event:
		document.dispatchEvent(e);
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

function addEventListener (type, method, filter){
	
	if(!method){
		method = type;
		type = '_all_';
	}
	
	if(type != '_all_'){
		start();
	}
	
	// Already got this listener?
	// If so, must re-add it, essentially re-registering the filter.
	// Note that we assume the user called this because it changed - we don't check for non-change here.
	var entry;
	
	if(messageTypes[type]){
		entry = messageTypes[type].find(mf => mf.method == method);
		if(entry){
			// Actually an update of existing one
			entry.filter = filter;
		}else{
			entry = {method,filter, id: refId++};
			messageTypes[type].push(entry);
		}
	}else{
		typeCount++;
		entry = {method,filter, id: refId++};
		messageTypes[type] = [entry];
	}
	
	if(type == '_all_'){
		return;
	}
	
	var msg = {type: '+', n: type, f: filter, i: entry.id};
	
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
	
	if(!messageTypes[type]){
		return;
	}
	
	var entry = messageTypes[type].find(mf => mf.method == method);
	if(!entry){
		return;
	}
	
	messageTypes[type] = messageTypes[type].filter(a => a != entry);
	
	if(ws && ws.readyState == WebSocket.OPEN){
		ws.send(getAsBuffer({type: '-', i: entry.id}));
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

export default {
	getSocket,
    addEventListener,
	removeEventListener,
	send,
	Writer
};