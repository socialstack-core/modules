import omit from 'UI/Functions/Omit';

export default function Link (props) {
	var attribs = omit(props, ['children', 'href', '_rte']);
	attribs.alt = attribs.alt || attribs.title;
	
	var children = props.children || props.text;
	var url = (props.url || props.href);
	
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
	
	return <a href={url}
		{...attribs}
		>
			{children}
	</a>;
}

Link.editable = true;

Link.propTypes = {
	title: 'string',
	href: 'string',
	children: 'jsx'
};
