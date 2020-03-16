import Tile from 'Admin/Tile';
import Loop from 'UI/Loop';
import AutoForm from 'Admin/AutoForm';
import Default from 'Admin/Pages/Default';


export default class AutoEdit extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		console.log(this.props);
		return (
			<Default>
				<Tile>
					<AutoForm endpoint={this.props.endpoint} id={this.props.id} />
				</Tile>
				{this.props.children}
			</Default>	
		);
	}
	
}

AutoEdit.propTypes = {
	children: true
};