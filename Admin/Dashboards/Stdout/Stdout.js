import webRequest from 'UI/Functions/WebRequest';
import Tile from 'Admin/Tile';
import Alert from 'UI/Alert';
import Html from 'UI/Html';
import {isoConvert} from 'UI/Functions/DateTools';

/**
 * Server stdout.
 */
export default function StdOut() {
	
	var [log, setLog] = React.useState([]);
	
	React.useEffect(() => {
		var update = () => {
			webRequest('monitoring/log', {}).then(response => {
				var results = response.json.results.reverse();
				
				results.forEach(res => {
					res.createdUtc = isoConvert(res.createdUtc);
				});
				
				console.log(results);
				setLog(results);
				
				if(window.scrollY >= (window.scrollMaxY - 100)){
					window.scroll(0,window.scrollMaxY);
				}
			});
		};
		
		update();
		
		setInterval(update, 3000);
	},[]);
	
	var renderTag = (msg) => {
		var className = 'tag-' + msg.type;
		var text = '';
		
		switch(msg.type){
			case 'ok':
				text='OK';
			break;
			case 'info':
				text ='INFO';
			break;
			case 'warn':
				text='WARN';
			break;
			case 'error':
				text='ERROR';
			break;
			case 'fatal':
				text='FATAL';
			break;
		}
		
		return <span class={'tag-' + msg.type}>{text} {msg.createdUtc.toLocaleString()}</span>;
	};
	
	return <div>
		<Tile>
			<Alert type='info'>
				The following log is partially realtime. It will poll the current server for its latest log entries every 3 seconds.
			</Alert>
		</Tile>
		<div className="dashboards-stdout">
			{log.map(entry => {
				
				return <div>
					{renderTag(entry)}
					{entry.messages.map(message => {
						if(message.trace){
							return <>
								<p>{message.entry}</p>
								<p>{message.trace}</p>
							</>;
						}
						return <p>{message.entry}</p>;
					})}
				</div>;
				
			})}
		</div>
	</div>;
}