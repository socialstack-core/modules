import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import isNumeric from 'UI/Functions/IsNumeric';

/**
 * A nav menu. This is a very thin wrapper over Loop so it essentially does everything that Loop can (i.e. <NavMenu inline .. etc).
 */

export default (props) =>
	<Loop over={'navmenuitem/' + (isNumeric(props.id) ? '' : 'key/') + props.id} {...props}>
	{(props.children && props.children.length) ? props.children : item => 
		<a href={item.target}>
			<Canvas>{item.bodyJson}</Canvas>
		</a>
	}
	</Loop>