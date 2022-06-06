
var collectDevices = () => {
	
	return new Promise((s, r) => {
		
		navigator.mediaDevices.enumerateDevices().then(devices => {
		
			var deviceSets = {audio: [], video: []};
			
			devices.forEach(device => {
			
				if (device.kind == 'audioinput' || device.kind == 'audiooutput'){
					deviceSets.audio.push(device);
				} else if (device.kind == 'videoinput' || device.kind == 'videooutput'){
					deviceSets.video.push(device);
				}
				
			});
			
			s(deviceSets);
			
		}).catch(e => {
			s({failed: true, error: e});
		});
	});
};

/*
Mic/ camera test UI. Appears on startup and is available via the cog in the corner at any point onwards.
*/
export default function AvTest(props){
	
	var [devices, setDevices] = React.useState();
	var [selectedMic, setSelectedMic] = React.useState();
	var [selectedCam, setSelectedCam] = React.useState();
	
	var onDevicesChanged = () => {
		collectDevices().then(devices => setDevices(devices));
	};
	
	React.useEffect(() => {
		navigator.mediaDevices.addEventListener('devicechange', onDevicesChanged); 
		
		// Initial run too:
		onDevicesChanged();
		
		return () => {
			navigator.mediaDevices.removeEventListener('devicechange', onDevicesChanged); 
		};
		
	}, []);
	
	if(!devices){
		// Collecting device set.
		return `Searching for a camera and microphone..`;
	}
	
	if(devices.failed){
		
		return <center>
			Unable to access your camera or microphone devices. 
			This is usually because either none were plugged in or because the permission prompt was rejected. 
			To participate in the meeting you'll need at least a microphone.
			<p>
				<button className="btn btn-primary" onClick={() => {
					// Run the func again:
					setDevices(null);
					collectDevices().then(devices => setDevices(devices));
				}}>
					Search again
				</button>
			</p>
		</center>;
		
	}
	
	// Got a device list. Display the UI now:
	return <div className="av-test">
		<div>
			<label for='av-test_microphone_select'>
				Which microphone would you like to use?
			</label>
			<select name='microphone' id='av-test_microphone_select'>
				{devices.audio.forEach(device => {
					return <option value={device.id} selected={device.id == selectedMic}>{device.label || 'Unnamed audio device'}</option>
				})}
			</select>
		</div>
		<div>
			<label for='av-test_camera_select'>
				Which camera would you like to use?
			</label>
			<select name='camera' id='av-test_camera_select'>
				{devices.camera.forEach(device => {
					return <option value={device.id} selected={device.id == selectedCam}>{device.label || 'Unnamed video device'}</option>
				})}
			</select>
		</div>
	</div>;
	
}