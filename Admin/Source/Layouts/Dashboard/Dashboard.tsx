import Landing from 'Admin/Layouts/Landing';
import Tile from 'Admin/Tile';
import Canvas from 'UI/Canvas';
import { useSession } from 'UI/Session';

/**
 * The main landing dashboard.
 * @param props
 * @returns
 */
const Dashboard: React.FC = (): React.ReactNode => {
	const { session } = useSession();
	var { role, user } = session;

	if (!role || !user) {
		return;
	}

	var greeting = user.firstName || user.username || `there`;

	return <>
		{role.adminDashboardJson ? <>
			<Canvas>
				{role.adminDashboardJson}
			</Canvas>
		</> : <>
				<Landing>
					<Tile title={`👋 Hey ${greeting}!`}>
						<p>
							{`You're in the administration area. Click on the 3 bars in the top left to choose something to do.`}
						</p>
					</Tile>
				</Landing>
			</>}
	</>;

}

export default Dashboard;