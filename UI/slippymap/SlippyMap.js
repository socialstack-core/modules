import getRef from 'UI/Functions/GetRef'; 
import Icon from 'UI/Icon'; 
import MapInteraction from './Map';
import Modal from 'UI/Modal';
import store from 'UI/Functions/Store';

const SHOW_MAP_MODAL = 'show_map_modal';

export default function SlippyMap(props) {
	var [showingModal, setShowingModal] = React.useState(store.get(SHOW_MAP_MODAL) ?? true);
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
				<span className="hotspot-cta">{hotspot.cta}</span>
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
					<button type="button" className="spot-marker" 
						onClick={e => onHotspotClick(hotspot, isOpen, index, setOpenHotspot)}
						onTouchEnd={e => onHotspotClick(hotspot, isOpen, index, setOpenHotspot)}>
						{getRef(hotspot.iconRef)}
						<Icon type='times' light />
					</button>
				</>
				:
				<>
					<div className="open-marker" data-theme={props['open-hotspot-theme'] || 'map-open-hotspot-theme'}>
						{onHotspotContent(hotspot)}
					</div>
					<button type="button" className="spot-marker" 
						onClick={e => onHotspotClick(hotspot, isOpen, index, setOpenHotspot)}
						onTouchEnd={e => onHotspotClick(hotspot, isOpen, index, setOpenHotspot)}>
						{getRef(hotspot.iconRef)}
						<Icon type='plus' light />
					</button>
				</>
			}
		</div>;
		
	};
	
	function closeModal() {
		setShowingModal(false);
		store.set(SHOW_MAP_MODAL, false);
	}

    return <>
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
						{getRef(props.imageRef, {attribs: {style: {pointerEvents: 'none', opacity: '.5'}}})}
						
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

        <Modal className="modal--blue" isLarge={true}
                visible={showingModal}
                onClose={() => closeModal()}>
			<h2 className="slippy-map__instructions">
				{`Click on a hotspot to discover the IDS stand`}
			</h2>
        </Modal>

	</>;
}

SlippyMap.propTypes = {
	imageRef: 'image'
};