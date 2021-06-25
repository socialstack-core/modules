import Content from 'UI/Content';
import { useSession, useRouter } from 'UI/Session';
import { useContent } from 'UI/Content';

var modes = {'content': 1, 'session': 1, 'url': 1};

export function TokenResolver(props){
	var {session} = useSession();
	var localContent = useContent();
	var {pageState} = useRouter();
	
	var text = (props.value || '').replace(/\$\{(\w|\.)+\}/g, function(textToken) {
		var fields = textToken.substring(2, textToken.length - 1).split('.');
		
		var mode = '';
		
		if(modes[fields[0]]){
			mode = fields.shift();
		}
		
		return resolveValue(mode,fields,session, localContent, pageState);
	});
	
	return props.children(text);
}

export function resolveValue(mode, fields, session, localContent, pageState){
	var token;
	
	if(mode == "content"){
		token = localContent ? localContent.content : null;
	}else if(mode == "url"){
		if(!pageState || !pageState.tokenNames){
			return '';
		}
		var index = pageState.tokenNames.indexOf(fields.join('.'));
		return (index == null || index == -1) ? '' : pageState.tokens[index];
	}else{
		token = session;
	}
	
	if(!token){
		return '';
	}
	
	var fields = fields;
	
	if(Array.isArray(fields) && fields.length){
		try{
			for(var i=0;i<fields.length;i++){
				token = token[fields[i]];
				if(token === undefined || token === null){
					return '';
				}
			}
		}catch(e){
			console.log(e);
			token = null;
		}
		
		return token;
	}else if(typeof fields == 'string'){
		return token[fields];
	}
}

/*
* Contextual token. 
* Available values either come from the primary type on the page, or the global state. The RTE establishes the options though.
*/

export default function Token (props) {
	// If editor, display the thing and its children:
	var {session} = useSession();
	var localContent = useContent();
	var {pageState} = useRouter();
	
	if(props._rte){
		return <span className="context-token" ref={props.rootRef}>
			{props.children}
		</span>;
	}
	
	// Resolved value. No wrapper - just plain value.
	return resolveValue(props.mode, props.fields, session, localContent, pageState);
}

Token.editable = {
	inline: true,
	onLoad: nodeInfo => {
		// Convert mode and fields to children root
		const data = nodeInfo.d || {};
		
		let str = data.mode || '';
		let fieldStr = data.fields ? data.fields.join('.') : '';
		
		if(str && fieldStr){
			str += '.' + fieldStr;
		}else{
			str += fieldStr;
		}
		
		if(!str){
			str='unnamed token';
		}
		
		nodeInfo.r = {
			children: {s: str}
		};
		
		nodeInfo.d = null;
	},
	onSave: nodeInfo => {
		// Ensure children root is pure text:
		var childRoot = nodeInfo.c && typeof nodeInfo.c === 'string';
		
		if(nodeInfo.c && typeof nodeInfo.c === 'string'){
			// good! The root is a pure text node.
			var pieces = nodeInfo.c.split('.');
			var first = pieces[0].toLowerCase();
			
			nodeInfo.d = {};
			
			if(modes[first]){
				nodeInfo.d.mode = first;
				pieces.shift();
			}
			
			nodeInfo.d.fields = pieces;
			delete nodeInfo.c;
		}
	}
};

Token.propTypes = {
	children: 'jsx'
};
