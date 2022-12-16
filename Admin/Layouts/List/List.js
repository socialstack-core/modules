import Tile from 'Admin/Tile';
import AutoList from 'Admin/AutoList';
import Loop from 'UI/Loop';
import Default from 'Admin/Layouts/Default';
import {useRouter} from 'UI/Session';


export default class List extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		var { pageState } = useRouter();
		var endpoint = this.props.endpoint;
		var entityName = this.props.singular;
		var entityNamePlural = this.props.plural;

		if (!endpoint && pageState.tokenNames) {
			for(var i = 0; i < pageState.tokenNames.length; i++) {
				if (pageState.tokenNames[i] == 'entity') {
					endpoint = pageState.tokens[i];
					entityName = endpoint.replace(/([A-Z])/g, ' $1').trim();
					entityNamePlural = entityName + "s";
				}
			}
		}

		return (
			<Default>
				<AutoList endpoint={endpoint} {...this.props} singular={entityName} title={'Edit or create ' + entityNamePlural} create={!this.props.noCreate} searchFields={this.props.searchFields || ['title']} />
				{this.props.children}
			</Default>	
		);
	}
	
}

List.propTypes = {
	children: true
};