import Html from 'UI/Html';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';
import Debounce from 'UI/Functions/Debounce';

/**
 * Used to search for things.
 */
export default class SiteSearch extends React.Component {
    constructor(props) {
        super(props);
        this.search = this.search.bind(this);
        this.state = {
            loading: false,
            results: null,
            aggregations: null,
            hidden: this.props.startHidden,
            debounce: new Debounce(this.search)
        };
    }

    selectResult(result) {
        this.setState({
            results: null,
            result,
            id: result ? result.id : null
        });

        if (this.input) {
            this.input.value = '';
        }

        this.props.onFind && this.props.onFind(result);
    }

    search(query) {
        if (this.props.onSiteSearch) {
            this.props.onSiteSearch(query);
            return;
        }

        // do we have a minimum length to activate query (0 will show all for emoty query)
        if (this.props.minLength && query.length < this.props.minLength) {
            var results = null;
            this.setState({ loading: false, results });
            return;
        }

        webRequest("/v1/sitesearch/query", { queryString: query, aggregations: 'tags', pageSize: (this.props.limit || 10) }, this.props.requestOpts)
            .then(response => {
                console.log('results', response.json);

                var results = response.json && response.json.results ? response.json.results : [];
                var aggregations = response.json && response.json.aggregations ? response.json.aggregations : {}
                this.setState({ loading: false, results , aggregations});
            });
    }

    display(result) {
        return (
            <>
                <div className="title">
                    <a href={result.url}><Html>{result.title}</Html></a>
                </div>

                {result.highlights.length > 0 ? (
                    <Html>{result.highlights}</Html>
                ) : (
                    <Html>{result.content}</Html>
                )}
            </>
        );
    }

    renderAggregation(aggregation) {
        return (
            <div className='aggregations {aggregation.key}'>

                {aggregation.buckets.map((bucket, i) => (
                    <div className='aggregation'>{bucket.key}<div className='count'>{bucket.count}</div></div>
                ))}

            </div>
        );
    }

    render() {
        var searchClass = ['sitesearch'];

        if (this.props.className) {
            searchClass.push(this.props.className);
        }

        if (this.state.hidden) {
            return <div className={searchClass.join(' ')} onClick={() => {
                this.setState({ hidden: false });
            }}>
                {
                    this.props.onHidden ? this.props.onHidden() : <i className="fa fa-search" />
                }
            </div>;
        }

        return <div className={searchClass.join(' ')} data-theme={this.props['data-theme'] || 'search-theme'}>

            <input ref={
                ele => {
                    this.input = ele
                }
            }
                autoComplete="false" className="form-control" defaultValue={this.props.defaultSiteSearch} value={this.props.searchText}
                placeholder={this.props.placeholder || `Search...`} type="text"
                onKeyDown={(e) => {
                    if (e.keyCode == 13) {
                        this.state.debounce.handle(e.target.value);
                        e.preventDefault();
                    }
                    if (e.keyCode == 27) {
                        this.setState({ results: null })
                    }

                }} />

            {this.state.loading &&
                <div className="feed__loading">
                    <i className="fas fa-spinner fa-spin" />
                </div>
            }

            {this.state.results && (
                <div className="suggestions">

                    <div className="aggregations">
                        {this.state.aggregations.length > 0 &&
                            this.state.aggregations.map((aggregation, i) => (
                                <>
                                    {this.renderAggregation(aggregation)}
                                </>
                            ))
                        }
                    </div>

                    {this.state.results.length > 0 ? (
                        this.state.results.map((result, i) => (
                            <button type="button" key={i} onMouseDown={() => this.selectResult(result)} className="btn suggestion">
                                {this.display(result)}
                            </button>
                        ))
                    ) : (
                        <div className="no-results">
                            {`No results found`}
                        </div>
                    )}
                </div>
            )}
        </div>;
    }
}

SiteSearch.propTypes = {
    limit: "int"
}

SiteSearch.defaultProps = {
    limit: 10
};


// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
SiteSearch.icon = "fa-magnifying-glass"; 