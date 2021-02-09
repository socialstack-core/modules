// Module import examples - none are required:
// import webRequest from 'UI/Functions/WebRequest';
// import Loop from 'UI/Loop';
import getRef from 'UI/Functions/GetRef';

export default class OpenGraphTags extends React.Component {

	// If you want to use state in your react component, uncomment this constructor:
	constructor(props){
		super(props);
		this.state = {
		};
	}

	
	render(){
		var { page } = this.props;
		
		if(!page) {
			return;
		}

		console.log(page);

		return <head>
			<meta property="og:title" content={page.title} />
			<meta property="og:type" content="website" />
			<meta property="og:url" content={page.url} />
			<meta property="og:image" content={getRef(page.imageRef, {url:true})}/>
		</head>;	
	}
}