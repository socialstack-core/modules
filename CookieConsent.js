import Container from 'UI/Container';
import Modal from 'UI/Modal';
import store from 'UI/Functions/Store';

var _response = store.get('consent');

function run(m){
	try{
		m(_response);
	}catch(e){
		console.error(e);
	}
}

function flush(){
	if(global.cookieLayer){
		global.cookieLayer.map(run);
	}
	global.cookieState=run
}

if(_response && _response.mode){
	flush();
}

export default class CookieConsent extends React.Component {
	
	constructor(props){
		super(props);
		
		this.state=_response || {};
		
		this.cookies = [
			{title: 'Logging In', names: ['user'], necessary: true, description: 'This one lets us log you in.'},
			{title: 'Consent', names: ['consent'], necessary: true, description: 'This helps us remember which type of cookies you\'re ok with.'},
			{title: 'Statistics', names: ['_ga', '_gid'], description: 'This is created by Google Analytics. We\'d like to use Google Analytics to figure out how many people are using our website and what parts of it they\'re using so we can make them even better. It does, however, potentially give Google the ability to track you on the web in general.'}
		];
	}
	
	renderDetails(){
		return <Modal isExtraLarge className="consent-info" visible={this.state.details} onClose={() => this.setState({details: false})}>
			<h1>Cookies - what are they?</h1>
			<p>
				Cookies are little bits of data we store on your computer or mobile device to remember things. They're great for things like letting you log in - we'll remember who you are next time you come back - but they're also good for tracking where you go on the internet which can be quite invasive, so that's why these prompts exist on lots of websites.
			</p>
			<h1>How we use them</h1>
			<p>
				We use cookies for the following things:
			</p>
			<ul>
				{this.cookies.map(cookie => {
					
					return <li>
						<h2>{cookie.title + ' - ' + (cookie.necessary ? 'Necessary' : 'Not Necessary')}</h2>
						<p>
							Cookie name(s): {cookie.names.join(', ')}
						</p>
						<p>
							{cookie.description}
						</p>
					</li>;
					
				})}
			</ul>
		</Modal>;
	}
	
	set(mode){
		var response = {mode, all: mode == 'all'};
		_response = response;
		store.set('consent',response);
		this.setState(response);
		flush();
	}
	
	render(){
		if(this.state.mode){
			return;
		}
		
		return <div className="cookie-consent">
			<Container>
				<h2>
					We'd like to use cookies
				</h2>
				<p>
					We use <a href='#' className="show-details" onClick={() => this.setState({
						details: true
					})}>cookies</a> to give you a great online experience. Please let us know if you agree to them.
				</p>
				<div>
					<button className="btn btn-secondary necessary-only" onClick={() => this.set('necessary')}>Use necessary cookies only</button>
					<button className="btn btn-primary" onClick={() => this.set('all')}>I agree</button>
				</div>
			</Container>
			{this.renderDetails()}
		</div>;
		
	}
	
}