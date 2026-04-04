import Tile from 'Admin/Tile';
import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import Link from 'UI/Link';
import Button from 'UI/Button';
import Dialog from 'UI/Dialog';
import ConfirmDialog from 'UI/Dialog/ConfirmDialog';
import { useState, useEffect } from 'react';
import monitoringApi from 'Api/StdOutController';
import AdminPage from "Admin/AdminPage";
import Footer from "Admin/Footer";

interface Confirmer {
	message: string,
	running?: boolean,
	action: () => Promise<void>
}

const Developer: React.FC<{}> = () => {

	var [confirmer, setConfirmer] = useState<Confirmer | null>(null);
	var [confirmerDone, setConfirmerDone] = useState(0);

	// Default developer role dashboard
	var WhoAmI = () => {

		var [who, setWho] = useState<int>();

		useEffect(() => {
			monitoringApi.whoAmI().then((response) => setWho(response.id))
		}, []);

		if (!who) {
			return <Loading />;
		}

		return <h3>{`Server #${who}`}</h3>;
	};

	var confirmAction = (message: string, action: () => Promise<void>) => {
		setConfirmerDone(0);
		setConfirmer({ message, action });
	};

	const showConfirmerModal = (confirmer: Confirmer) => {
		return <>
			<ConfirmDialog variant="primary"
				title={`Are You Sure?`}
				isOpen={confirmer}
				onClose={() => setConfirmer(null)}
				confirmText={`Yes, I know what I am doing`}
				confirmCallback={() => {
					// mandatory, but handled within Dialog.Footer below
				}}>
				{confirmer.running ? <Loading /> :
					confirmerDone ? <>
						<Alert variant='success'>
							{`Done`}
						</Alert>
					</> : <>
						<p>
							{confirmer.message}
						</p>
					</>}
				<Dialog.Footer>
					<Button outlined onClick={() => setConfirmer(null)}>
						{`Cancel`}
					</Button>
					{!confirmer.running && <>
						<Button onClick={() => {
							return confirmer.action()
								.then(() => {
									setConfirmer({ ...confirmer, running: true });
									setConfirmerDone(1);
								});
						}}>
							{`Yes, I know what I am doing`}
						</Button>
					</>}
				</Dialog.Footer>
			</ConfirmDialog>
		</>;
	};

	const renderNotifications = () => {
		return <>
			<Tile title={`Notifications`} className="admin-tile--dev-notifications">
				{`This is the default developer role dashboard. Suggestions for ideal things available here would be much appreciated! 
				In the meantime, here's some developer facing functionality for poking at your site instance.`}
			</Tile>
		</>;
	};

	const renderMetrics = () => {
		return <>
			<Tile title={`Metrics & Health`} className="admin-tile--dev-metrics">
				{`Metrics and realtime health monitoring coming soon`}
			</Tile>
		</>;
	}

	const renderMaintenance = () => {
		return <>
			<Tile title={`Maintenance Links`} className="admin-tile--dev-maintenance">
				<WhoAmI />
				<ul>
					<li>
						<a href='/en-admin/stdout'>
							{`View the API output from the current server`}
						</a>
					</li>
					<li>
						<a href='/en-admin/database'>
							{`Query the database`}
						</a>
					</li>
					<li>
						<a href='#' onClick={() => confirmAction(
							`Force the C# garbage collector to run inside the API`,
							async () => {
								await monitoringApi.gC()
							}
						)}>
							{`Run the garbage collector (will prompt first)`}
						</a>
					</li>
					<li>
						<a href='#' onClick={() => confirmAction(
							`This will tell the application to halt. On a deployed server, the service runner will then automatically start again. Note that the restart won't happen in a debug environment.`,
							async () => {
								await monitoringApi.halt()
							}
						)}>
							{`Restart the API (will prompt first)`}
						</a>
					</li>
					<li>
						<a href='/en-admin/stress'>
							{`Stress tester`}
						</a>
					</li>
				</ul>
			</Tile>
		</>;
	};

	return <>
		<AdminPage.SubHeader title={`Dashboard`} breadcrumbs={[
				{
					title: `Dashboard`
				}
			]} />
		<AdminPage.ContentWrapper>
			<AdminPage.Content>
				<div className="admin-dashboard admin-dashboard--developer">
					{renderNotifications()}
					{renderMetrics()}
					{renderMaintenance()}
					{confirmer && showConfirmerModal(confirmer)}
				</div>
			</AdminPage.Content>
		</AdminPage.ContentWrapper>
		<Footer>
			{/*
			<Footer.BulkActions>
				<Button>Test</Button>
			</Footer.BulkActions>
			*/}
			<Footer.CallsToAction>
				<Link href="/" variant="primary">
					{`Return to site`}
				</Link>
			</Footer.CallsToAction>
		</Footer>
	</>;
}

export default Developer;