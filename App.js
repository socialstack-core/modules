import PageRouter from 'UI/PageRouter';
import webRequest from 'UI/Functions/WebRequest';

/**
 * The root component. It stores state for us, such as the currently logged in user.
 * The instance of this component is available in the global scope as simply 'app'.
 * You can also navigate via app.setState({url: ..}) too, or alternatively, use a relative path anchor tag.
 */
export default class App extends React.Component{
	
	constructor(props){
		super(props);
		this.state = {
			user: null,
			url: global.location.pathname
		};
		global.app = this;
		
		
		webRequest('user/self').then(response => {
			if(response && response.json && response.json.id){
				global.app.setState({user: response.json, realUser: response.json});
			}
		}).catch(e=>{
			// Not logged in
		});
	}
	
	render(){
		return (
			<PageRouter 
				url={this.state.url} 
				onNavigate={url => {
					this.setState({url, pageChange: this.state.pageChange++});
				}} 
			/>
		);
	}
	
}