import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import isNumeric from 'UI/Functions/IsNumeric';

/**
 * A nav menu. This is a very thin wrapper over Loop so it essentially does everything that Loop can (i.e. <NavMenu inline .. etc).
 */

export default (props) => {
    var filter = props.filter || {};

    if (!filter.where) {
        filter.where = {};
    }

    if (isNumeric(props.id)) {
        filter.where.NavMenuId = props.id;
    } else {
        filter.where.MenuKey = props.id;
    }

    if (!filter.sort) {
        filter.sort = {
            field: 'Order'
        };
    }

    return <Loop over={'navmenuitem/list'} {...props} filter={filter}>
        {(props.children && props.children.length) ? props.children : item =>
            <a href={item.target}>
                <Canvas>{item.bodyJson}</Canvas>
            </a>
        }
    </Loop>
}
