import Canvas from 'UI/Canvas';
import webRequest from 'UI/Functions/WebRequest';

var templateCache = {
	
};

/**
 * This component renders a template with a given ID, and substitutes one or more named subs.
 */
export default class Template extends React.Component {
	
	constructor(props){
		super(props);
		this.state={
			id: props.id,
			template: templateCache['' + props.id]
		};
		
		if(!this.state.template){
			this.load(props.id);
		}
	}
	
	componentWillReceiveProps(props){
		if(this.state.id == props.id){
			return;
		}
		
		this.load(props.id);
	}
	
	load(id){
		if(!id){
			return;
		}
		var template = templateCache['' + id];
		
		if(template){
			this.setState({
				id,
				template
			});
		}else{
		
			webRequest('template/' + id).then(result => {
				
				// Template meta is..
				var template = result.json;
				templateCache['' + id] = template;
				
				this.setState({
					id,
					template
				});
				
			}).catch(console.error);
		}
		
	}
	
	render(){
		if(!this.state.template){
			return null;
		}
		return (<Canvas onSubstitute={(name) => {
			
			if(name == 'content' || !name){
				// The direct kids of the template.
				return this.props.children;
			}
			
			return this.props.tokens ? this.props.tokens[name] : null;
		}}>
			{
				this.state.template.bodyJson
			}
		</Canvas>);
		
	}
	
}

Template.propTypes = {
	id: 'int',
	children: true
};