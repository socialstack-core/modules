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

            if (child.props.icon) {
                icon = <i className={child.props.icon} data-total={child.props.total} />;
            }

            return (
                <li className={className}>
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
