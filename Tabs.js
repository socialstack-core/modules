/*
	tab collection.
*/

export default class Tabs extends React.Component {

    state = {
        activeIndex: this.props.defaultIndex || 0
    };

	render(){
        let { activeIndex } = this.state;

        let tabs = this.props.children.map((child, index) => {
            let className = activeIndex === index ? 'tab-link tab-link-active' : 'tab-link';
            let icon;
            let criticalTotal = child.props.criticalTotal > 99 ? "99+" : child.props.criticalTotal;
            let standardTotal = child.props.standardTotal > 99 ? "99+" : child.props.standardTotal;

            if (child.props.icon) {
                icon = (
                    <i className={child.props.icon}
                        data-total={child.props.total > 99 ? "99+" : child.props.total}
                    />
                );
            }

            return (
                <li className={className} data-critical-total={criticalTotal} data-standard-total={standardTotal}>
                    <button
                        className="tab-link-btn"
                        onClick={() => this.setState({ activeIndex: index })}
                    >
                        {icon}
                        {child.props.label}
                    </button>
                </li>
            );
        });
        return (
            <div>
                <ul className="tab-wrapper">{tabs}</ul>
                {
                    this.props.children.map((child, index) => {
                        return (
                            <div className="tab" style={{ display: index === this.state.activeIndex ? '' : 'none' }}>
                                {child}
                            </div>
                        );
                    })
                }
            </div>
        );
    }
}
