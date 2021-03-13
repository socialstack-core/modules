import Tile from 'Admin/Tile';
import Content from 'UI/Content';
import Canvas from 'UI/Canvas';
import Default from 'Admin/Layouts/Default';


export default function Dashboard (props, context) {
	
	var { role } = context.app.state;
	
	return (
		<Default>
			<Content type='userrole' id={role.id}>
				{userRole => {
					if(!userRole){
						return null;
					}
					
					if(!userRole.dashboardJson){
						return <Tile>{`Welcome to the administration area. Use the menu in the top left to pick what you would like to do.`}</Tile>;
					}
					
					return <Canvas>
						{userRole.dashboardJson}
					</Canvas>;
				}}
			</Content>
		</Default>	
	);
	
}