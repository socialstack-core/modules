import Loop from 'UI/Loop';
import Spacer from 'UI/Spacer';
import Html from 'UI/Html';
import StarRating from 'UI/StarRating';
import getRef from 'UI/Functions/GetRef';
import Tags from 'UI/Tags';

/**
 * Loads the contents of a gallery entry.
 */

export default (props) =>
	<Loop over='gallery/entry/list' filter={{where: {Id: props.id}}} {...props}>
	{props.children.length ? props.children : entry => 
		<div className = "gallery-entry-view">
			<h1><Html>{entry.name}</Html></h1>
			<div>
				<StarRating on={entry} />
			</div>
			<div className="gallery-image">
				{getRef(entry.imageRef)}
			</div>
			<div>
				<Html>{entry.description}</Html>
			</div>
			<Tags on={entry} />
		</div>
	}
	</Loop>