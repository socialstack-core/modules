import Content from 'UI/Content';
import { useSession } from 'UI/Session';
import { useContent } from 'UI/Content';

/*
* Contextual token. 
* Available values either come from the primary type on the page, or the global state. The RTE establishes the options though.
*/

export default function Token (props) {
	// If editor, display the thing and its children:
	var {session} = useSession();
	var localContent = useContent();
	
	if(props._rte){
		return <span className="context-token" ref={props.rootRef}>
			{props.children}
		</span>;
	}
	
	// Resolved value. No wrapper - just plain value.
	var token;
	
	if(props.mode == "content"){
		token = localContent ? localContent.content : null;
	}else{
		token = session;
	}
	
	if(!token){
		return null;
	}
	
	var fields = props.fields;
	
	if(Array.isArray(fields) && fields.length){
		try{
			for(var i=0;i<fields.length;i++){
				token = token[fields[i]];
				if(token === undefined || token === null){
					return null;
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
		console.log('onSave', nodeInfo);
		
		// Ensure children root is pure text:
		var childRoot = nodeInfo.c && typeof nodeInfo.c === 'string';
		
		if(nodeInfo.c && typeof nodeInfo.c === 'string'){
			// good! The root is a pure text node.
			var pieces = nodeInfo.c.split('.');
			var first = pieces[0].toLowerCase();
			
			nodeInfo.d = {};
			
			if(first == 'session' || first == 'content'){
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
