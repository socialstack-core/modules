/**
 * Props for the tile component.
 */
interface TileProps {

    /**
     * Optional column width for this tile.
     */
    row?: number;

    /**
     * Optional title to display on the header.
     */
    title?: string;

    /**
     * True if the tile should have no base classname.
     */
    empty?: boolean;

    /**
     * True if the footer of the tile should be fixed positioned.
     */
    fixedFooter?: boolean;

    /**
     * Optional custom class name(s).
     */
    className?: string;
}

/**
 * A tile in the admin area.
 * @param props
 * @returns
 */
const Tile: React.FC<React.PropsWithChildren<TileProps>> = (props) => {
	const { title, row, className, empty, children, fixedFooter } = props;
    var tileClass = ['tile'];
    tileClass.push('col-md-' + (12 / (row || 1)));
    if (className) {
        tileClass.push(className);
    }

    if (fixedFooter) {
        tileClass.push('tile--fixed-footer');
    }

	return (
        <div className={tileClass.join(' ')}>
            <div className={empty ? "" : "component-tile"}>
                {!empty && title && (
                    <h3 className="admin-heading">
                        {title}
                    </h3>
                )}
                <article>
                    {children}
                </article>
            </div>
		</div>
	);
}

export default Tile;