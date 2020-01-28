import apiEndpoint from 'UI/Functions/ApiEndpoint';

export default function mapUrl(url) {
	if(url && !url.startsWith("http:") && !url.startsWith("https:") && !url.startsWith("//")){
		// Site relative URL. Api URL?
		if(url[0] != '/'){
			// All site page URLs should be root relative except API requests which are not.
			url = apiEndpoint(url);
		}
	}
	
	return url;
}