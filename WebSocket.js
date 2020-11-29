import store from 'UI/Functions/Store';
var eventTarget = global.events.get('App');
var __user = null;
var waitMode = 0;

eventTarget.add('onState', () => {
	var {user, loadingUser} = global.app.state;
	
	if(!waitMode){
		if(loadingUser){
			waitMode=1;
			loadingUser.then(() => {
				waitMode=2;
				__user = global.app.state.user;
			});
		}else{
			waitMode=2;
			__user = global.app.state.user;
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
			// Just an empty message will ping the server:
			ws.send('{}');
		}
		
	}, 30000);
	
}

// Connects the websocket
function connect(){
	
	if(typeof WebSocket === "undefined"){
		return;
	}
	
	// Fire up the websocket:
	var sk = new WebSocket((global.wsHost || global.apiHost || global.location.origin).replace("http", "ws") + "/live-websocket/");
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
			sk.send(JSON.stringify({
				type: 'Auth',
				token: store.get('context')
			}));
		}
		
		// Default pres:
		var { page } = global.pageRouter.state;
		sk.send(JSON.stringify({
			type: "Pres",
			c: "page",
			m: JSON.stringify({url: global.location.pathname + global.location.search}),
			id: page ? page.id : 0
		}));
		
		for(var i=0;i<msgs.length;i++){
			sk.send(JSON.stringify(msgs[i]));
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
			sk.send(JSON.stringify({type: '+*', set}));
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
			
			if(handlers && handlers.length){
				for(var i=0;i<handlers.length;i++){
					handlers[i].method(message);
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

var refId = 1;

module.exports = {
    addEventListener:(type, method, filter) => {
		
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
			ws.send(JSON.stringify(msg));
		}
	},
	removeEventListener:(type, method) => {
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
			ws.send(JSON.stringify({type: '-', i: entry.id}));
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
	},
	send: message => {
		start();
		send(message);
	}
};