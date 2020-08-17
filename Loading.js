
/**
 * Standalone component which displays a loader (typically a spinner).
 */
export default class Loading extends React.Component {

    render() {
        let message = this.props.message ? this.props.message : "Loading ... ";

        return (
            <div className="alert alert-info loading">
                {message}
                <i className="fas fa-spinner fa-spin" />
            </div>
        );
    }

}