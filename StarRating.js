/*
* Specialised reaction component - shows up to a 10 star rating mechanism. Defaults to 5. <StarRating max={5} on={content} /> where the content is an IHaveReactions type.
*/
export default (props) => {
	
	let { on } = props;
	
	if(!on){
		return null;
	}
	
	if(typeof on.reactions != 'undefined'){
		on = on.reactions;
	}
	
	var max = props.max || 5;
	
	if(max>10){
		console.warn("10 stars max");
		max=10;
	}
	
	// First we need to get the ratings:
	var stars = {};
	
	if(on){
		on.forEach(entry => {
			if(entry && entry.reactionType && entry.reactionType.key.endsWith("_star")){
				var starCount = parseInt(entry.reactionType.key.split('_')[0]);
				if(!isNaN(starCount)){
					stars[starCount] = entry;
				}
			}
		});
	}
	
	var totalRating=0;
	var totalRates=0;
	
	for(var i=1;i<=max;i++){
		if(!stars[i]){
			continue;
		}
		
		var rates = stars[i].total;
		totalRating += i*rates;
		totalRates += rates;
	}
	
	var avg = totalRates == 0 ? 0 : (totalRating/totalRates);
	
	if(props.compact){
		return <div className="star-rating-compact">
			{totalRates == 0 ? (
				<i className="fa fa-star star-unrated"/>
			) : (
				<div>
					<i className="fa fa-star star"/> <span className="rating-average">{avg}</span>
				</div>
			)}
		</div>
	}
	
	var starsUnrated = [];
	var starsRated = [];
	
	for(var i=1;i<=max;i++){
		starsUnrated.push(<i className="fa fa-star star-unrated"/>);
		starsRated.push(<i className="fa fa-star star"/>);
	}
	
	var width = (avg * 100 / max) + '%';
	
	return <div className="star-rating">
		<div className="star-rating-container">
			<div className="unrated">
				{starsUnrated}
			</div>
			<div className="rated" style={{width}}>
				{starsRated}
			</div>
		</div>
		<span className="rating-counter">
			{totalRates == 1 ? '1 vote' : totalRates + ' votes'}
		</span>
	</div>;
};
