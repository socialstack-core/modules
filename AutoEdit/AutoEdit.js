import Tile from 'Admin/Tile';
import Loop from 'UI/Loop';
import {TokenResolver} from 'UI/Token';
import AutoForm from 'Admin/AutoForm';
import Default from 'Admin/Layouts/Default';
import omit from 'UI/Functions/Omit';


export default class AutoEdit extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		var id = this.props.id;
		
		// legacy support - mapping old urlTokens:
		if(typeof id != 'string' && id.type == 'urlToken'){
			id = '${url.' + id.name + '}';
		}
		
		return <TokenResolver value={id}>
			{id => 
				<Default>
					<AutoForm {...(omit(this.props, ['children']))} id={id} />
					{this.props.children}
				</Default>	
			}
		</TokenResolver>;
	}
	
}

AutoEdit.propTypes = {
	children: true,
	id: 'token',
	endpoint: 'string',
	deletePage: 'string'
};