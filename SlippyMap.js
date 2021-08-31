import getRef from 'UI/Functions/GetRef'; 
import Icon from 'UI/Icon'; 
import MapInteraction from './Map';

export default function SlippyMap(props) {
	
	var [openHotspot, setOpenHotspot] = React.useState(-1);
	
	var onHotspotClick = props.onHotspotClick ? props.onHotspotClick : (hotspot, isOpen, index, setOpenHotspot) => {
		setOpenHotspot(isOpen ? -1 : index);
	};
	
	var onHotspotContent = props.onHotspotContent ? props.onHotspotContent : (hotspot) => {
		return <>
			{hotspot.title && <h1>
					{hotspot.title}
			</h1>}
			{hotspot.description && <p>
				{hotspot.description}
			</p>}
			{hotspot.ctaUrl && <a href={hotspot.ctaUrl}>
				{hotspot.cta}
			</a>}
		</>;
	};
	
	var onHotspot = props.onHotspotDraw ? props.onHotspotDraw : (hotspot, isOpen, index) => {
		
		return <div className={isOpen ? "hotspot is-open" : "hotspot"} style={{
			top: hotspot.y + '%',
			left: hotspot.x + '%'
		}}>
			{isOpen ? <>
					<div className="open-marker" data-theme={props['open-hotspot-theme'] || 'map-open-hotspot-theme'}>
						{onHotspotContent(hotspot)}
					</div>
					<button className="spot-marker" onClick={e => onHotspotClick(hotspot, isOpen, index, setOpenHotspot)}>
						{getRef(hotspot.iconRef)}
						<Icon type='times' light />
					</button>
				</>
				:
				<>
					<div className="open-marker" data-theme={props['open-hotspot-theme'] || 'map-open-hotspot-theme'}>
						{onHotspotContent(hotspot)}
					</div>
					<button className="spot-marker" onClick={e => onHotspotClick(hotspot, isOpen, index, setOpenHotspot)}>
						{getRef(hotspot.iconRef)}
						<Icon type='plus' light />
					</button>
				</>
			}
		</div>;
		
	};
	
    return (
		<div className="slippy-map" data-theme={props['data-theme']}>
			<MapInteraction
			  showControls
			>
			  {
				({ translation, scale }) => {
				  // Translate first and then scale.  Otherwise, the scale would affect the translation.
				  const transform = `translate(${translation.x}px, ${translation.y}px) scale(${scale})`;
				  return (
					<div
					  style={{
						height: '100%',
						width: '100%',
						position: 'relative', // for absolutely positioned children
						overflow: 'hidden',
						touchAction: 'none', // Not supported in Safari :(
						msTouchAction: 'none',
						cursor: 'all-scroll',
						WebkitUserSelect: 'none',
						MozUserSelect: 'none',
						msUserSelect: 'none'
					  }}
					>
					  <div
						style={{
						  display: 'inline-block', // size to content
						  transform: transform,
						  transformOrigin: '0 0 '
						}}
					  >
						{getRef(props.imageRef, {attribs: {style: {pointerEvents: 'none'}}})}
						
						{props.hotspots && <div className="action-hotspots">
							{props.hotspots.map((hs, i) => onHotspot(hs, openHotspot == i, i))}
						</div>}
					  </div>
					</div>
				  );
				}
			  }
			</MapInteraction>
		</div>
	  );
}

SlippyMap.propTypes = {
	imageRef: 'image'
};