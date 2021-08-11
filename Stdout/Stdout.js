import webRequest from 'UI/Functions/WebRequest';
import Tile from 'Admin/Tile';
import Alert from 'UI/Alert';
import Html from 'UI/Html';

/**
 * Server stdout.
 */
export default function StdOut() {
	
	var [log, setLog] = React.useState();
	
	React.useEffect(() => {
		var update = () => {
			webRequest('monitoring/stdout').then(response => {
				setLog(response.json.log.replace(/\r/gi, '').replace(/\n/gi, '<br/>'));
				setTimeout(() => {
					window.scroll(0,window.scrollMaxY);
				}, 100);
			});
		};
		
		update();
		
		setInterval(update, 3000);
	},[]);
	
	return <div>
		<Tile>
			<Alert type='info'>
				The following log is partially realtime. It will poll the current server for its current log fragment every 3 seconds.
			</Alert>
		</Tile>
		<div className="dashboards-stdout">
			<Html>{log}</Html>
		</div>
	</div>;
}