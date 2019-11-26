import PageRouter from 'UI/PageRouter';

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
			url: window.location.pathname
		};
		global.app = this;
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