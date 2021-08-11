import webRequest from 'UI/Functions/WebRequest';
import Alert from 'UI/Alert';
import Html from 'UI/Html';

/**
 * Server stdout.
 */
export default function StdOut() {
	
	var [log, setLog] = React.useState();
	
	React.useEffect(() => {
		webRequest('monitoring/stdout').then(response => {
			setLog(response.json.log.replace(/\r/gi, '').replace(/\n/gi, '<br/>'));
		});
	},[]);
	
	return <div className="dashboards-stdout">
		<Html>{log}</Html>
	</div>;
}