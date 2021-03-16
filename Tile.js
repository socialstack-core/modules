/**
 * A tile in the admin area.
 */
export default class Tile extends React.Component {
	
	render() {

        const className = (12 / (this.props.row || 1));
		
        return (
            <div className={'col-md-' + className + ' ' + (this.props.className || '')}>
                <div className={this.props.empty ? "" : "component-tile"}>
                    {!this.props.empty && (
                        <h3 className="admin-heading">
                            {this.props.title}
                        </h3>
                    )}
                    <article>
                        {this.props.children}
                    </article>
                </div>
            </div>
        );
    }
	
}

Tile.propTypes = {
	children: true
};