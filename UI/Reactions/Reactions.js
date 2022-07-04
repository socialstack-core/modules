import getRef from 'UI/Functions/GetRef';

/*
* Displays an inline list of reactions (the little like/ heart etc buttons with a number).
*/
export default (props) => {
	
	let { on } = props;
	
	if(!on){
		return null;
	}
	
	if(typeof on.reactions != 'undefined'){
		on = on.reactions;
	}
	
	if(!on || !on.length){
		return null;
	}
	
	return <div className="reactions">
		{
			on.map(reaction => <div className="reaction">{getRef(reaction.reactionType.iconRef)} <span className="count">{reaction.total}</span></div>)
		}
	</div>;
};
