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

		var metaTitle = document.createElement('meta');
		metaTitle.setAttribute('property', 'og:title');
		metaTitle.content = page.title;
		document.head.appendChild(metaTitle);

		var metaType = document.createElement('meta');
		metaType.setAttribute('property', 'og:type');
		metaType.content = "website";
		document.head.appendChild(metaType);

		var metaUrl = document.createElement('meta');
		metaUrl.setAttribute('property', 'og:url');
		metaUrl.content = global.location.href;
		document.head.appendChild(metaUrl);

		var metaImage = document.createElement('meta');
		metaImage.setAttribute('property', 'og:image');
		metaImage.content = global.location.origin + getRef(page.imageRef, {url:true});
		document.head.appendChild(metaImage);




		var metaDescription = document.createElement('meta');
		metaDescription.setAttribute('property', 'og:description');
		metaDescription.content = page.title;
		document.head.appendChild(metaDescription);

		var metaSiteName = document.createElement('meta');
		metaSiteName.setAttribute('property', 'og:site_name');
		metaSiteName.content = '4-Roads';
		document.head.appendChild(metaSiteName);

		return <head>
			<meta property="og:title" content={page.title} />
			<meta property="og:type" content="website" />
			<meta property="og:url" content={global.location.href} />
			<meta property="og:image" content={global.location.origin + getRef(page.imageRef, {url:true})}/>
		</head>;	
	}  
}