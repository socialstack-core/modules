import Tile from 'Admin/Tile';
import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import Row from 'UI/Row';
import Modal from 'UI/Modal';
import Input from 'UI/Input';
import { useState, useEffect } from 'react';
import monitoringApi from 'Api/StdOutController';

interface Confirmer {
	message: string,
	running?:boolean,
	action:()=>Promise<void>
}

const Developer: React.FC<{}> = () => {
	
	var [confirmer, setConfirmer] = useState<Confirmer | null>(null);
	var [confirmerDone, setConfirmerDone] = useState(0);
	
	/* Default developer role dashboard. */
	
	var WhoAmI = () => {
		
		var [who, setWho] = useState<int>();
		
		useEffect(() => {
			monitoringApi.whoAmI().then((response) => setWho(response.id))
		}, []);
		
		if(!who){
			return <Loading />;
		}
		
		return <h3>{`Server #${who}`}</h3>;
	};
	
	var confirmAction = (message : string, action : () => Promise<void>) => {
		setConfirmerDone(0);
		setConfirmer({message, action});
	};

	const showConfirmerModal = (confirmer : Confirmer) => {
		return <Modal visible onClose={() => setConfirmer(null)} title={`Are you sure?`}>
			{confirmerDone ? (
				<Alert variant='success'>Done</Alert>
			) : (<>
				<p>
					{confirmer.message}
				</p>
				{confirmer.running ? <Loading /> : <Input type='button' onClick={() => {
					confirmer.action().then(() => setConfirmerDone(1));
					setConfirmer({ ...confirmer, running: true });
				}} defaultValue='Yes, I know what I am doing' />}
			</>)}
		</Modal>
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
				<WhoAmI />
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
							`Force the C# garbage collector to run inside the API`,
							async () => {
								await monitoringApi.gC()
							}
						)}>
							Run the garbage collector (will prompt first)
						</a>
					</li>
					<li>
						<a href='#' onClick={() => confirmAction(
							`This will tell the application to halt. On a deployed server, the service runner will then automatically start again. Note that the restart won't happen in a debug environment.`,
							async () => {
								await monitoringApi.halt()
							}
						)}>
							Restart the API (will prompt first)
						</a>
					</li>
					<li>
						<a href='/en-admin/stress'>
							Stress tester
						</a>
					</li>
				</ul>
			</Tile>
			{confirmer && showConfirmerModal(confirmer)}
		</Row>
	</div>;
	
}

export default Developer;