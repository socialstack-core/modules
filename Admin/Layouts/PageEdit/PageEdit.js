import {TokenResolver} from 'UI/Token';
import PageForm from 'Admin/PageForm';
import Default from 'Admin/Layouts/Default';
import omit from 'UI/Functions/Omit';


export default class PageEdit extends React.Component {
	
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
					<PageForm {...(omit(this.props, ['children']))} id={id} />
					{this.props.children}
				</Default>	
			}
		</TokenResolver>;
	}
	
}

PageEdit.propTypes = {
	children: true,
	id: 'token',
	endpoint: 'string',
	deletePage: 'string'
};