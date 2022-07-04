import fontColor from 'UI/Functions/FontColor';

/* 
* Displays an inline list of tags (i.e. "Book", "Movie", "News")
*/

export default (props) => {
    let { on } = props;
	
	if(!on){
		return null;
    }
    
    if(on.tags){
		on = on.tags;
    }
    
    if(!on.length){
		return null;
    }
    
    return <div className="tags">
		{
			on.map(tag => <div style = {{background: tag.hexColor ? tag.hexColor : "#ffffff", color: fontColor(tag.hexColor) }}className="tag">{tag.name}</div>)
        }
	</div>;
}