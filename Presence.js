import webSocket from 'UI/Functions/WebSocket';

function send(type, evt){
	webSocket.send({
		type: "Pres",
		c: type,
		m: JSON.stringify(evt)
	});
}

function pageChange(page){
	webSocket.send({
		type: "Pres",
		c: "page",
		m: JSON.stringify({url: global.location.pathname + global.location.search}),
		id: page.id
	});
}

if(global.addEventListener){
	global.addEventListener("xpagechange", e => e.pageInfo && e.pageInfo.page && pageChange(e.pageInfo.page));
}

var presence = {
	event: (type, meta) => {
		send(type,meta);
	}
};

export default presence;