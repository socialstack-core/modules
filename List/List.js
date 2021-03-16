import Tile from 'Admin/Tile';
import AutoList from 'Admin/AutoList';
import Loop from 'UI/Loop';
import Default from 'Admin/Layouts/Default';


export default class List extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		return (
			<Default>
				<AutoList endpoint={this.props.endpoint} {...this.props} title={'Edit or create ' + this.props.plural} create={!this.props.noCreate} searchFields={this.props.searchFields || ['title']} />
				{this.props.children}
			</Default>	
		);
	}
	
}

List.propTypes = {
	children: true
};