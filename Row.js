/**
 * A responsive row.
 */

export default (props) => <div className={"row " + (props.noGutters ? "no-gutters" : "") + " " + (props.className || '')} >{props.children}</div>