import webSocket from 'UI/Functions/WebSocket';

function send(evt){
	webSocket.send({
		type: "Pres",
		payload: evt
	});
}

function pageChange(page){
	send({
		type: "page",
		url: page.url,
		id: page.id
	});
}

if(global.addEventListener){
	global.addEventListener("xpagechange", e => e.pageInfo && e.pageInfo.page && pageChange(e.pageInfo.page));
}

module.exports = {
	event: (type, name, meta) => {
		send({
			type,
			name,
			meta
		});
	}
};