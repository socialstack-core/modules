
if(global.addEventListener){
	global.addEventListener("xpagechange", e => {
		if(global.ga){
			try{
				global.ga('set', 'page', global.location.pathname);
				global.ga('send', 'pageview');
			}catch(e){
				console.error(e);
			}
		}
	});
}
