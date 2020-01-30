/*
* Specialised reaction component - shows upvote/ downvote indicator like SO.
*/
export default (props) => {
	
	let { on } = props;
	
	if(!on){
		return null;
	}
	
	if(typeof on.reactions != 'undefined'){
		on = on.reactions;
	}
	
	// First we need to get the upvotes/ downvotes:
	var upvotes = on && on.find(reaction=>reaction.reactionType && reaction.reactionType.key == 'upvote');
	var downvotes = on && on.find(reaction=>reaction.reactionType && reaction.reactionType.key == 'downvote');
	
	var total = (upvotes ? upvotes.total : 0) - (downvotes ? downvotes.total : 0);
	
	return <div className="up-down-vote">
		<div className="upvote">
			<i className="fa fa-caret-up"/>
		</div>
		<div className="count">
		{
			total
		}
		</div>
		<div className="downvote">
			<i className="fa fa-caret-down"/>
		</div>
	</div>;
};
