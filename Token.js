import Content from 'UI/Content';

/*
* Contextual token. 
* Available values either come from the primary type on the page, or the global state. The RTE establishes the options though.
*/

export default function Token (props, context) {
	// If editor, display the thing and its children:
	if(props._rte){
		return <span className="context-token" ref={props.rootRef}>
			{props.children}
		</span>;
	}
	
	// Resolved value. No wrapper - just plain value.
	var content;
	
	if(props.mode == 'primary' || props.mode == 'p'){
		content = Content.getPrimary();
	}else{
		content = context[props.mode];
	}
	
	if(!content){
		return null;
	}
	
	var fields = props.fields;
	
	if(Array.isArray(fields) && fields.length){
		try{
			for(var i=0;i<fields.length;i++){
				content = content[fields[i]];
				if(content === undefined || content === null){
					return null;
				}
			}
		}catch(e){
			console.log(e);
			content = null;
		}
		
		return content;
	}else if(typeof fields == 'string'){
		return content[fields];
	}
}

Token.editable = {inline: true};

Token.propTypes = {
	children: 'jsx'
};
