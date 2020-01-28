import Loop from 'UI/Loop';
import getRef from 'UI/Functions/GetRef';
import url from 'UI/Functions/Url';

/**
 * A tiled list of gallery entries.
 */

export default (props) =>	<div className="gallery-entry-list">
		<Loop asCols size={4} over='gallery/entry/list' filter={props.galleryId ? {where: {GalleryId: props.galleryId}} : undefined} {...props}>
		{props.children.length ? props.children : entry => <a href={url(entry)}>
				<div className='gallery-entry' style={{backgroundImage: 'url(' + getRef(entry.imageRef, {url: true}) + ')'}}></div>
			</a> 
		}
		</Loop>
	</div>