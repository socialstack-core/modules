export default class Paginator extends React.Component {
	
	changePage(newPageId){
		
		try{
			var nextPage = parseInt(newPageId);
			
			if(!nextPage || nextPage<=0){
				nextPage = 1;
			}
			
			var totalPages = this.getTotalPages();
			
			if(totalPages && nextPage>totalPages){
				nextPage = totalPages;
			}
			
			this.props.onChange && this.props.onChange(nextPage, this.props.pageIndex);
			
		}catch{
			// E.g. user typed in something that isn't a number
			return;
		}
	}
	
	getTotalPages(){
		var { totalResults, pageSize } = this.props;
		if(totalResults){
			return Math.ceil(totalResults / pageSize);
		}
		return 0;
	}
	
	render(){
		
		var {pageIndex} = this.props;
		
		var totalPages = this.getTotalPages();
		
		var currentPage = this.props.pageIndex || 1;
		
		if(!pageIndex || pageIndex<=0){
			pageIndex = 1;
		}
		
		if(totalPages && pageIndex>totalPages){
			pageIndex = totalPages;
		}
		
		return <div className="paginator">
			{currentPage > 1 && (
				<span className="nav-before">
					<i className="fa fa-step-backward first-page" onClick={() => this.changePage(1)} />
					<i className="fa fa-chevron-left prev-page" onClick={() => this.changePage(currentPage-1)} />
				</span>
			)}
			Page <input type="text" onkeyUp={e => {
				if(e.keyCode == 13){
					this.changePage(e.target.value);
				}
			}} value={this.props.pageIndex || '1'}/> 
			{!!totalPages && (
				' of ' + totalPages
			)}
			{currentPage < totalPages && (
				<span className="nav-after">
					<i className="fa fa-chevron-right next-page" onClick={() => this.changePage(currentPage+1)} />
					<i className="fa fa-step-forward last-page" onClick={() => this.changePage(totalPages)} />
				</span>
			)}
		</div>;
		
	}
	
}

// propTypes are used to describe configuration on your component in the editor.
// Just setting it to an empty object will make your component appear as something that can be added.
// Define your available props like the examples below.

Paginator.propTypes = {
	
};