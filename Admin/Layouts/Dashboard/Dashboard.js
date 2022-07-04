import Tile from 'Admin/Tile';
import Content from 'UI/Content';
import Canvas from 'UI/Canvas';
import Default from 'Admin/Layouts/Default';
import { useSession } from 'UI/Session';

export default function Dashboard (props, context) {
	
	const { session } = useSession();
	var { role, user } = session;
	
	return (
		<Default>
			{role && user && <Content type='role' id={role.id}>
				{userRole => {
					if(!userRole){
						return null;
					}
					
					if(!userRole.adminDashboardJson){
						return <div className="container">
							<Tile title={
								<center>
									<p>
										<i className="fa fa-hand-peace" />
									</p>
									{`Hey there!`}
								</center>}
							>
								<center>
									{`You're in the administration area. Click on the 3 bars in the top left to choose something to do.`}
								</center>
							</Tile>
						</div>;
					}
					
					return <Canvas>
						{userRole.adminDashboardJson}
					</Canvas>;
				}}
			</Content>}
		</Default>	
	);
	
}