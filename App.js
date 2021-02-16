import PageRouter from 'UI/PageRouter';
import webRequest from 'UI/Functions/WebRequest';

/**
 * The root component. It stores state for us, such as the currently logged in user.
 * The instance of this component is available in the global scope as simply 'app'.
 * You can also navigate via app.setState({url: ..}) too, or alternatively, use a relative path anchor tag.
 */
export default class App extends React.Component{
	
	constructor(props, context){
		super(props, context);
		var url = global.hashRouter ? (location.hash ? location.hash.substring(1) : '/') : (location.pathname + location.search);
		global.app = this;
		context.app = this;
		var initState = props.init || global.gsInit;
		
		if(initState){
			this.state = {url, ...initState, loadingUser: false};
		}else{
			this.state = {
				user: null,
				url,
				loadingUser: true
			};
			this.state.loadingUser = webRequest('user/self').then(response => {
				if(response && response.json){
					this.setState({...response.json, loadingUser: false});
				}else{
					this.setState({loadingUser: false});
				}
				return response;
			}).catch(e=>{
				// Not logged in
				this.setState({user: null, realUser: null, loadingUser: false});
			});
		}
	}
	
	componentDidUpdate(){
		document.dispatchEvent(new Event('App/state'));
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