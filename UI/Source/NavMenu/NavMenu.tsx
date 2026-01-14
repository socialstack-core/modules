import Loop from 'UI/Loop';
import Canvas from 'UI/Canvas';
import navMenuItemApi, { NavMenuItem } from 'Api/NavMenuItem';

interface NavMenuProps {

    /**
     * Either provide the menu ID or its key.
     */
    id?: number,

    /**
     * Either provide the menu ID or its key.
     */
    menuKey?: string

    /**
     * Optional filter.
     */
    filter?: Partial<Record<keyof (NavMenuItem), string | number | boolean>>

    /**
     * Custom child render function.
     * @param item The item itself.
     * @param index Current iteration index.
     * @param fragmentCount The number of results in the current array. Not the same as the total number of results.
     * @returns
     */
    children?: (item: NavMenuItem, index: number, fragmentCount: number) => React.ReactNode;
}

/**
 * A nav menu. This is a very thin wrapper over Loop so it essentially does everything that Loop can (i.e. <NavMenu inline .. etc).
 */
const NavMenu: React.FC<NavMenuProps> = (props) => {
    var filter = props.filter || {};

    if (props.id) {
        filter.navMenuId = props.id;
    } else if(props.menuKey){
        filter.menuKey = props.id;
    }

    return <Loop over={navMenuItemApi} {...props} filter={{where: filter}}>
        {props.children || (item =>
            <a href={item.target}>
                <Canvas>{item.bodyJson}</Canvas>
            </a>)}
    </Loop>
}

export default NavMenu;
