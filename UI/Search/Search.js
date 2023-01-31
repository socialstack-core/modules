import Loading from 'UI/Loading';
import Input from 'UI/Input';
import webRequest from 'UI/Functions/WebRequest';
import getRef from 'UI/Functions/GetRef';
import Debounce from 'UI/Functions/Debounce';

/**
 * Used to search for things.
 */
export default class Search extends React.Component {
	constructor(props){
		super(props);
		this.search = this.search.bind(this);
		this.state = {
			loading: false,
			results: null,
			hidden: this.props.startHidden,
			debounce: new Debounce(this.search)
		};
	}
	
    componentDidMount(){
        var result = this.props.result;
        if(result){
            this.setState({
                result
            });
        }else{
			var id = this.props.value || this.props.defaultValue;
			
			var url = this.props.for;
			
			if(!id || !url){
				return;
			}
			
			if(this.props.host){
				url = (url.indexOf('http') === 0 || url[0] == '/') ? url : this.props.host + '/v1/' + url;
			}
			
			webRequest(url + '/' + id).then(response => {
				
				if(response && response.json){
					
					this.setState({
						result: response.json
					});
					
				}
				
			});
		}
    }
    
	selectResult(result){
		this.setState({
			results: null,
			result,
			id: result ? result.id : null
		});
		
		if(this.input){
			this.input.value='';
		}
		
		this.props.onFind && this.props.onFind(result);
	}
	
	search(query){
		if(this.props.onSearch){
			this.props.onSearch(query);
			return;
		}
		
		// do we have a minimum length to activate query (0 will show all for emoty query)
		if (this.props.minLength && query.length < this.props.minLength) {
			var results = null;
			this.setState({loading: false, results});
			return;
		}

		var excludeIds = this.props.exclude ? this.props.exclude : [];

		var where = {};

		var field = this.props.field || 'name';
		var fieldNameUcFirst = field.charAt(0).toUpperCase() + field.slice(1);
		
		where[fieldNameUcFirst]={contains: query};
		
		if(this.props.onQuery){
			where = this.props.onQuery(where, query);
		}

		var url = this.props.for ? this.props.for + '/list' : this.props.endpoint + '?q=' + encodeURIComponent(query);
		
		if(this.props.host){
			url = (url.indexOf('http') === 0 || url[0] == '/') ? url : this.props.host + '/v1/' + url;
		}
		
		if(this.props.for){
			// Otherwise it just exports the query
			this.setState({loading: true});
			webRequest(url, {where, pageSize: (this.props.limit || 50)}, this.props.requestOpts).then(response => {
				var results = response.json ? response.json.results : [];
				if (excludeIds && excludeIds.length > 0) {
					results = results.filter(t => !excludeIds.includes(t.id));
				}
				this.setState({loading: false, results});
			});
		}else if(this.props.endpoint){
			webRequest(url, null, this.props.requestOpts).then(response => {
				var results = response.json ? response.json.results : [];
				this.props.onResults && this.props.onResults(results);
				this.setState({loading: false, results});
			});
		}
	}
	
	avatar(result) {
		if(result.avatarRef === undefined){
			return '';
		}
		
		return getRef(result.avatarRef);
	}
	
	display(result, isSuggestion){
		if(this.props.onDisplay){
			return this.props.onDisplay(result, isSuggestion);
		} 
		
		var field = this.props.field || 'name';
		field = field.charAt(0).toLowerCase() + field.slice(1);
		
		if(field == 'fullName' && result.email){
			return result[field] + ' (' + result.email + ')';
		}
		
		return result[field];
	}
	
	render() {
		var searchClass = ['search'];

		if (this.props.className) {
			searchClass.push(this.props.className);
        }
		
		if (this.state.hidden) {
			return <div className={searchClass.join(' ')} onClick={() => {
				this.setState({hidden: false});
			}}>
				{
					this.props.onHidden ? this.props.onHidden() : <i className="fa fa-search" />
				}
			</div>;
		}
		
		return <div className={searchClass.join(' ')} data-theme={this.props['data-theme'] || 'search-theme'}>
			<input ref={
				ele =>{
					this.input = ele
				}
			}
			onblur = {() => {
				this.setState({results: null})
			}}
				autoComplete="false" className="form-control" defaultValue={this.props.defaultSearch} value={this.props.searchText}
				placeholder={this.props.placeholder || `Search...`} type="text"
			onKeyUp={(e) => {
				if(e.keyCode != 27){
					this.state.debounce.handle(e.target.value);
				}
			}} 
			onFocus={(e) =>{
				if(e.target.value.length > 0) {
					this.state.debounce.handle(e.target.value);
				}
			}}
			onKeyDown={(e) => {
				if (e.keyCode == 13){
					if (this.state.results && !this.props.onResults && this.state.results.length == 1) {
						this.selectResult(this.state.results[0]);
					}	
					e.preventDefault();
				}
				if (e.keyCode == 27){
					this.setState({results: null})
				}

			}}/>
			{this.state.results && !this.props.onResults && (
				<div className="suggestions">
					{this.state.results.length ? (
						this.state.results.map((result, i) => (
							<button type="button" key={i} onMouseDown={() => this.selectResult(result)} className="btn suggestion">
								{this.display(result, true)}
							</button>
						))
					) : (
							<div className="no-results">
								{`No results found`}
							</div>
						)}
				</div>
			)}
			{
				this.props.name && <input type="hidden" value={this.state.id || this.props.value || this.props.defaultValue} name={this.props.name} />
			}
		</div>;
	}
}