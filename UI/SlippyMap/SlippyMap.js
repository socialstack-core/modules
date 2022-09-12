import getRef from 'UI/Functions/GetRef'; 
import Icon from 'UI/Icon'; 
import MapInteraction from './Map';

export default function SlippyMap(props) {
	
    return <>
		<div className="slippy-map" data-theme={props['data-theme']}>
			<MapInteraction
			  controls={props.controls}
			  minScale={props.screenScale || 1}
			  screenScale={props.screenScale || 1}
			  target={props.target}
			  initialTranslation={props.initialTranslation}
			>
			  {
				({ translation, scale, rotation }) => {
					
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
						{props.children || getRef(props.imageRef, {attribs: {style: {pointerEvents: 'none', opacity: '.5'}}})}
					  </div>
					</div>
				  );
				}
			  }
			</MapInteraction>
		</div>
	</>;
}

SlippyMap.propTypes = {
	imageRef: 'image'
};