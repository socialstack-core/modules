/*
* Gets the total number of upvote reactions minus downvote reactions for a given piece of content.
* Can be negative.
*/
export default (content) => {
	
	var upvotes = 0;
	var downvotes = 0;
	
	if(content && content.reactions){
		var upvoteCount = content.reactions.find(r => r.reactionType && r.reactionType.key == "upvote");
		var downvoteCount = content.reactions.find(r => r.reactionType && r.reactionType.key == "downvote");
		
		if(upvoteCount){
			upvotes = upvoteCount.total;
		}
		
		if(downvoteCount){
			downvotes = downvoteCount.total;
		}
	}
	
	return upvotes-downvotes;
}