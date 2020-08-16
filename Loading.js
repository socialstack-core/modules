/**
 * Standalone component which displays a loader (typically a spinner).
 */
export default class Loading extends React.Component {
	
    render() {
        let message = this.props.message ? this.props.message : "Loading ... ";

        return (
            <div className="alert alert-info loading">
                {message}

                {this.props.svg && 
                    <div className="fa-spin">
                        this.props.svg
                    </div>
                }

                {!this.props.svg &&
                    <i className="fas fa-spinner fa-spin" />
                }

            </div>
        );
	}
	
}
