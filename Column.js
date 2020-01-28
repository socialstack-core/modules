/**
 * A 12 segment responsive column. Usually used within a <Row>.
 */

export default (props) => <div className={"col-md-" + (props.size || 6) + " " + (props.noGutters ? "no-gutters" : "")}>{props.children}</div>