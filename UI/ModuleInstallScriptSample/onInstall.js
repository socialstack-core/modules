module.exports = (config, toolsApi, currentModuleFilePath) => {
	
	/*
		This will run when the module is installed, because it's referenced inside the "scripts" section of package.json.
		
		If you export a function, Socialstack tools will give you the config of the current install run.
		
		If your function returns a promise, tools will wait for your promise to complete before doing anything else.
		
		To see various available config values:
		console.log(config);
	*/
	
	console.log("Hello Socialstack tools installer!");
	console.log("The project is at: " + config.projectRoot);
	console.log("The install request was run from here: " + config.calledFromPath);
	
	// Optional - return a promise to do some heavier work.
	return new Promise((success, reject) => {
		
		console.log("e.g. manipulate files, download things  - anything really.");
		
		// Just make sure you tell the promise you're done (or failed by reject()ing):
		success();
	});
	
};