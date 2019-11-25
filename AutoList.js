import Tile from 'Admin/Tile';
import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';


export default class AutoList extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		return <Tile>
			<Loop asTable over={this.props.endpoint + "/list"} {...this.props}>
			{
				entry => {
					return this.props.fields.map(field => <td><a href={this.props.path + '' + entry.id}>{
							field.endsWith("Json") ? <Canvas>{entry[field]}</Canvas> : entry[field]
						}</a></td>);
				}
			}
			</Loop>
		</Tile>;
	}
}