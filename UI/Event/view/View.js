import webRequest from 'UI/Functions/WebRequest';
import Canvas from 'UI/Canvas';
import getRef from 'UI/Functions/GetRef';
import Col from "UI/Column";
import Row from "UI/Row";
import FormatDate from "UI/Functions/FormatDate";

export default class View extends React.Component {
    
    constructor(props){
		super(props);
		
		this.state = {};
		
		this.load(props);
	}
	
	componentWillReceiveProps(props){
		this.load(props);
	}
	
	load(props){
		webRequest('event/' + props.eventId).then(response => {
			this.setState({
				error: null,
				event: response.json
			});
		}).catch(e => {
			console.error(e);
			
			this.setState({
				event: null,
				error: e
			});
		});
	}
    
    render(){
        if(!this.state.event){
			return null;
        }
        
        if(this.props.children.length){
			// Child render func:
			return this.props.children[0](this.state.event);
        }
        
        return (
			<div>
				<div className="event">
                    <Row>
                        <Col size = "6">
                            <img src={getRef(this.state.event.featureRef, {url: true})} style={{width: '100%'}} />
                        </Col>
                        <Col size = "4">
                            <h1>
                                {this.state.event.name}
                            </h1>
                            <h5>
                                {"Start Date: "}<FormatDate date = {this.state.event.startUtc}/>
                            </h5>
                            <h5>
                                {"End Date: "}<FormatDate date = {this.state.event.endUtc}/>
                            </h5>
                            <h6>
                                {this.state.event.description}
                            </h6>
                        </Col>
                    </Row>
                    <Row>
                        <Canvas>
                            {this.state.event.bodyJson}
                        </Canvas>
                    </Row>
				</div>
			</div>
		);
    }
}