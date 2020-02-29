/**
 * A specific width container for content (maps directly to bootstrap 'container' by default).
 */

const container = (props) => <div className="container">{props.children}</div>;
export default container;
container.propTypes={children: true};
container.icon = 'cube';