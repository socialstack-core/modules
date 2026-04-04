/**
 * Props for the tile component.
 */
interface TileProps {

    /**
     * Optional title to display on the header.
     */
    title?: string;

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
	const { title, className, children, fixedFooter } = props;
    var tileClass = ['admin-tile'];

	if (className) {
        tileClass.push(className);
    }

    if (fixedFooter) {
        tileClass.push('admin-tile--fixed-footer');
    }

	return (
        <div className={tileClass.join(' ')}>
            {title && (
                <h2 className="admin-page__subtitle">
                    {title}
                </h2>
			)}
			<div className="admin-tile__content">
				{children}
			</div>
		</div>
	);
}

export default Tile;