import webSocket from 'UI/Functions/WebSocket';

function send(evt){
	webSocket.send({
		type: "Pres",
		payload: evt
	});
}

function pageChange(url){
	if(!url){
		return;
	}
	
	send({
		type: "page",
		name: url
	});
}

if(global.addEventListener){
	global.addEventListener("popstate", () => pageChange(document.location.pathname));
	global.addEventListener("xpushstate", e => pageChange(e.url));
}

document.location && document.location.pathname && pageChange(document.location.pathname);

module.exports = {
	event: (type, name, meta) => {
		send({
			type,
			name,
			meta
		});
	}
};