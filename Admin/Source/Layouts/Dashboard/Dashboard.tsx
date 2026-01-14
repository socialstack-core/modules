import Tile from 'Admin/Tile';
import Canvas from 'UI/Canvas';
import { useSession } from 'UI/Session';
import roleApi from 'Api/Role';

/**
 * The main landing dashboard.
 * @param props
 * @returns
 */
const Dashboard: React.FC<{}> = () => {
	
	const { session } = useSession();
	var { role, user } = session;

	return role && user && <>
		{role.adminDashboardJson ?
			<Canvas>
				{role.adminDashboardJson}
			</Canvas> : <div className="container">
				<Tile>
					<center>
						<p>
							<i className="fa fa-hand-peace" />
						</p>
						{`Hey there!`}
					</center>
					<center>
						{`You're in the administration area. Click on the 3 bars in the top left to choose something to do.`}
					</center>
				</Tile>
			</div>
		}
	</>;
}

export default Dashboard;