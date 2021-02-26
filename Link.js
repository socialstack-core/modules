import omit from 'UI/Functions/Omit';

export default class Link extends React.Component {
    constructor(props) {
        super(props);
    }
	
    render() {
		var attribs = omit(this.props, ['text', 'url', 'children']);
		attribs.alt = attribs.alt || attribs.title;

		var url = (this.props.url || this.props.href);
		
		if(url){
			// if url contains :// it must be as-is (which happens anyway).
			if(url[0] == '/'){
				if(url.length>1 && url[1] == '/'){
					// as-is
				}else{
					var prefix = window.urlPrefix || '';

					if(prefix) {
						if(prefix[0] != '/'){
							// must return an absolute url
							prefix = '/' + prefix;
						 }

						if(prefix[prefix.length - 1] == "/") {
							url = url.substring(1);
						}
			
						url = prefix + url;
					}
				}
			}
		}

		if(this.props.text){
			return <a href={url} 
				dangerouslySetInnerHTML={{__html: (this.props.text)}}
				{...attribs}
			/>;
		}else{
			return <a href={url}
				{...attribs}
				>
					{this.props.children}
			</a>;
		}
    }
}

Link.propTypes = {
	text: 'string',
	title: 'string',
	url: 'string'
};
