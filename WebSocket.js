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
	for(var type in messageTypes){
		var handlers = messageTypes[type];
		for(var i=0;i<handlers.length;i++){
			handlers[i]({type: 'status', connected: state});
		}
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

// Connects the websocket
function connect(){
	var isHttps = global.location.protocol == "https:";
	
	console.log('Connecting');
	
	// Fire up the websocket:
	ws = new WebSocket((global.apiHost || window.location.origin).replace("http", "ws") + "/live-websocket/");
	
	ws.addEventListener("open", () => {
		informStatus(true);
		var msgs = onConnectedMessages;
		onConnectedMessages = [];
		
		for(var i=0;i<msgs.length;i++){
			ws.send(JSON.stringify(msgs[i]));
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
	if(ws.readyState == WebSocket.OPEN){
		ws.send(JSON.stringify(msg));
	}else{
		onConnectedMessages.push(msg);
	}
}

module.exports = {
    addEventListener:(type, method) => {
		start();
		
		if(messageTypes[type]){
			messageTypes[type].push(method);
		}else{
			typeCount++;
			messageTypes[type] = [method];
			console.log('Todo: if this fails to send and RemoveEventListener is called, make sure its not in the send queue');
			send({type: 'AddEventListener', name: type});
		}
	},
	removeEventListener:(type, method) => {
		
		if(!messageTypes[type]){
			return;
		}
		
		messageTypes[type] = messageTypes[type].filter(mtd => mtd != method);
		
		if(!messageTypes[type].length){
			typeCount--;
			send({type: 'RemoveEventListener', name: type});
			
			if(typeCount<=0){
				typeCount=0;
				ws.addEventListener("close", e => {e.stopPropagation()});
				ws.close();
				ws=null;
			}
			
			delete messageTypes[type];
		}
	},
	send: message => {
		start();
		send(message);
	}
};