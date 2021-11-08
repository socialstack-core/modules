import Tile from 'Admin/Tile';
import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import Row from 'UI/Row';
import Modal from 'UI/Modal';
import Input from 'UI/Input';
import webRequest from 'UI/Functions/WebRequest';


export default function Developer() {
	
	var [confirmer, setConfirmer] = React.useState();
	var [confirmerDone, setConfirmerDone] = React.useState();
	
	/* Default developer role dashboard. */
	
	var whoAmI = () => {
		
		var [who, setWho] = React.useState();
		
		React.useEffect(() => webRequest('monitoring/whoami').then((response) => setWho(response.json.id)), []);
		
		if(!who){
			return <Loading />;
		}
		
		return <h3>{`Server #${who}`}</h3>;
	};
	
	var confirmAction = (message, action) => {
		setConfirmerDone(0);
		setConfirmer({message, action});
	};
	
	return <div className="dashboards-developer">
		<Row>
			<Tile row={2} title={`Notifications`}>
				This is the default developer role dashboard. Suggestions for ideal things available here would be much appreciated! In the meantime, here's some developer facing functionality for poking at your site instance.
			</Tile>
			<Tile row={2} title={`Metrics & Health`}>
				Metrics and realtime health monitoring coming soon
			</Tile>
			<Tile title={`Maintenance links`}>
				{whoAmI()}
				<ul>
					<li>
						<a href='/en-admin/stdout'>
							View the API output from the current server
						</a>
					</li>
					<li>
						<a href='/en-admin/database'>
							Query the database
						</a>
					</li>
					<li>
						<a href='#' onClick={() => confirmAction(
							`This will tell this server to reload the UI (not all servers in the cluster currently)`,
							() => webRequest("monitoring/ui-reload")
						)}>
							Hot reload the UI (will prompt first)
						</a>
					</li>
					<li>
						<a href='#' onClick={() => confirmAction(
							`Force the C# garbage collector to run inside the API`,
							() => webRequest("monitoring/gc")
						)}>
							Run the garbage collector (will prompt first)
						</a>
					</li>
					<li>
						<a href='#' onClick={() => confirmAction(
							`This will tell the application to halt. On a deployed server, the service runner will then automatically start again. Note that the restart won't happen in a debug environment.`,
							() => webRequest("monitoring/halt")
						)}>
							Restart the API (will prompt first)
						</a>
					</li>
				</ul>
			</Tile>
			{confirmer && <Modal visible onClose={() => setConfirmer(null)} title={`Are you sure?`}>
				{confirmerDone ? (
					<Alert type='success'>Done</Alert>
				) : (<>
					<p>
						{confirmer.message}
					</p>
					{confirmer.running ? <Loading /> : <Input type='button' onClick={() => {
						confirmer.action().then(() => setConfirmerDone(1));
						setConfirmer({...confirmer, running: true});
					}} defaultValue='Yes, I know what I am doing'/>}
				</>)}
			</Modal>}
		</Row>
	</div>;
	
}