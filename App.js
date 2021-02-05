import PageRouter from 'UI/PageRouter';
import webRequest from 'UI/Functions/WebRequest';

var eventTarget;

/**
 * The root component. It stores state for us, such as the currently logged in user.
 * The instance of this component is available in the global scope as simply 'app'.
 * You can also navigate via app.setState({url: ..}) too, or alternatively, use a relative path anchor tag.
 */
export default class App extends React.Component{
	
	constructor(props){
		super(props);
		var url = global.location.pathname + global.location.search;
		global.app = this;
		
		if(global.gsInit){
			this.state = {url, ...global.gsInit, loadingUser: false};
		}else{
			this.state = {
				user: null,
				url,
				loadingUser: true
			};
			this.state.loadingUser = webRequest('user/self').then(response => {
				if(response && response.json){
					global.app.setState({...response.json, loadingUser: false});
				}else{
					global.app.setState({loadingUser: false});
				}
				return response;
			}).catch(e=>{
				// Not logged in
				global.app.setState({user: null, realUser: null, loadingUser: false});
			});
		}
		eventTarget = global.events.get('App');
	}
	
	componentDidUpdate(){
		eventTarget.onState && eventTarget.onState();
	}
	
	render(){
		return (
			<PageRouter 
				url={this.state.url} 
				onNavigate={url => {
					this.setState({url});
				}} 
			/>
		);
	}
	
}