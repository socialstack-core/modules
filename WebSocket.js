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
	var handlers = messageTypes['status'];
	for(var i=0;i<handlers.length;i++){
		handlers[i]({connected: state});
	}
}

function onClose(){
	if(!ws){
		return;
	}
	ws=null;
	informStatus(false);
	goAgain();
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
		if(ws && ws.readyState == WebSocket.OPEN && ws.ping)
		{
			ws.ping();
		}
		
	}, 30000);
	
}

// Connects the websocket
function connect(){
	var isHttps = global.location.protocol == "https:";
	
	// Fire up the websocket:
	ws = new WebSocket((global.apiHost || global.location.origin).replace("http", "ws") + "/live-websocket/");
	setPing();
	
	ws.addEventListener("open", () => {
		informStatus(true);
		var msgs = onConnectedMessages;
		onConnectedMessages = [];
		
		for(var i=0;i<msgs.length;i++){
			ws.send(JSON.stringify(msgs[i]));
		}
		
		var types = [];
		
		for(var name in messageTypes){
			if(name != 'status'){
				types.push(name);
			}
		}
		
		if(types.length){
			ws.send(JSON.stringify({type: 'AddSet', names: types}));
		}
	});
	
	ws.addEventListener("close", onClose);
	ws.addEventListener("error", onClose);
	
	ws.addEventListener("message", e => {
		var message = JSON.parse(e.data);
		console.log(message);
		
		if(message && message.type){
			var handlers = messageTypes[message.type];
			
			if(handlers && handlers.length){
				for(var i=0;i<handlers.length;i++){
					handlers[i](message);
				}
			}
		}
	});
    
}

function send(msg){
	if(!ws){
		connect();
	}
	if(ws.readyState == WebSocket.OPEN){
		ws.send(JSON.stringify(msg));
	}else{
		onConnectedMessages.push(msg);
	}
}

module.exports = {
    addEventListener:(type, method) => {
		if(type != 'status'){
			start();
		}
		
		if(messageTypes[type]){
			messageTypes[type].push(method);
		}else{
			typeCount++;
			messageTypes[type] = [method];
			
			if(type == 'status'){
				return;
			}
			
			var msg = {type: 'Add', name: type};
			
			if(!ws){
				connect();
			}
			
			if(ws.readyState == WebSocket.OPEN){
				ws.send(JSON.stringify(msg));
			}
		}
	},
	removeEventListener:(type, method) => {
		
		if(!messageTypes[type]){
			return;
		}
		
		messageTypes[type] = messageTypes[type].filter(mtd => mtd != method);
		
		if(!messageTypes[type].length){
			typeCount--;
			
			if(typeCount<=0){
				typeCount=0;
			}
			
			if(ws && ws.readyState == WebSocket.OPEN){
				ws.send(JSON.stringify({type: 'Remove', name: type}));
			}
			
			// Todo: Do this after a timeout:
			/*
				ws.addEventListener("close", e => {e.stopPropagation()});
				ws.close();
				ws=null;
			}
			*/
			
			delete messageTypes[type];
		}
	},
	send: message => {
		start();
		send(message);
	}
};