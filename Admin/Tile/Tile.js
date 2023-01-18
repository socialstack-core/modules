// a tile in the admin area
export default function Tile(props) {
	const { title, row, className, empty, children, fixedFooter } = props;
    var tileClass = ['tile'];
    tileClass.push('col-md-' + (12 / (row || 1)));
    tileClass.push(className);

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

Tile.propTypes = {
	children: true
};