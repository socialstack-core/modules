import omit from 'UI/Functions/Omit';
import { useSession } from 'UI/Session';
import { useState, useEffect } from 'react';

export default function Link (props) {
	const { session, setSession } = useSession();

	var attribs = omit(props, ['children', 'href', '_rte' ,'hreflang']);
	attribs.alt = attribs.alt || attribs.title;
	attribs.ref = attribs.rootRef;
	
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

		if (url != "/") {

			// strip any trailing slashes
			while (url.endsWith("/")) {
				url = url.slice(0, -1);
			}

		}

	}
	
	return <a href={url} {...attribs}>
		{children}
	</a>;
}

Link.editable = true;

Link.propTypes = {
	title: 'string',
	href: 'string',
	children: 'jsx'
};
