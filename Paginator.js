var id = 1;

export default class Paginator extends React.Component {

	constructor(props) {
		super(props);
		this.newId();
        this.paginator = React.createRef();
	}

	componentWillReceiveProps(props) {
		this.newId();
    }

    componentDidMount() {
        this.checkVisible();
    }

	newId() {
		this.fieldId = 'paginator_' + (id++);
	}

    checkVisible() {
        var paginator = this.paginator.current;

        if (!paginator) {
			// will be unavailable on the initial pass
            return;
        }

        var bounding = paginator.getBoundingClientRect();

        if (
            bounding.top >= 0 &&
            bounding.left >= 0 &&
            bounding.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
            bounding.right <= (window.innerWidth || document.documentElement.clientWidth)
        ) {
            return;
        }

        paginator.scrollIntoView({ block: "end" });
    }

    changePage(newPageId) {
        try {
			var nextPage = parseInt(newPageId);

			if (!nextPage || nextPage <= 0) {
				nextPage = 1;
			}

			var totalPages = this.getTotalPages();

			if (totalPages && nextPage > totalPages) {
				nextPage = totalPages;
			}

            this.props.onChange && this.props.onChange(nextPage, this.props.pageIndex);
		} catch{
			// E.g. user typed in something that isn't a number
			return;
		}
	}

	getTotalPages() {
		var { totalResults, pageSize } = this.props;
		if (totalResults) {
			return Math.ceil(totalResults / pageSize);
		}
		return 0;
	}

	renderPage(page, currentPage, totalPages) {
		var isCurrentPage = page == currentPage;
		var pageClass = isCurrentPage ? "page-item active" : "page-item";
		var isEmpty = page < 1 || page > totalPages;

		return <li className={pageClass}>
			{!isCurrentPage && !isEmpty &&
				<button type="button" className="page-link" onClick={() => this.changePage(page)}>
					{page}
				</button>
			}
			{isCurrentPage && !isEmpty &&
				<span className="page-link">
					{page}
				</span>
			}
			{isEmpty &&
				<span className="page-link empty">
					&nbsp;
				</span>
			}
		</li>;
	}

	renderPageLinks(pageRange, currentPage, totalPages) {
		return pageRange.map((page) => this.renderPage(page, currentPage, totalPages));
	}

	render() {
		var { pageIndex } = this.props;

		var totalPages = this.getTotalPages();

		// if we only have a single page then optionally hide
		if (!this.props.always && totalPages && totalPages < 2) {
			return;
		}

		var currentPage = this.props.pageIndex || 1;

		if (!pageIndex || pageIndex <= 0) {
			pageIndex = 1;
		}

		if (totalPages && pageIndex > totalPages) {
			pageIndex = totalPages;
		}

		var description = this.props.description || "Results";
		var firstIcon = this.props.firstIcon || "fas fa-fast-backward";
		var prevIcon = this.props.prevIcon || "fas fa-backward";
		var nextIcon = this.props.nextIcon || "fas fa-forward";
		var lastIcon = this.props.lastIcon || "fas fa-fast-forward";

		var showInput = this.props.showInput !== undefined ? this.props.showInput : true;
		var maxLinks = this.props.maxLinks || 5;
		var fromPage, toPage;

		if (maxLinks % 2 == 0) {
			fromPage = currentPage - ((maxLinks / 2) - 1);
			toPage = currentPage + (maxLinks / 2);
		} else {
			fromPage = currentPage - ((maxLinks-1) / 2);
			toPage = currentPage + ((maxLinks - 1) / 2);
		}

		while (fromPage < 1) {
			fromPage++;
			toPage++
		}

		while (toPage > totalPages) {
			toPage--;
		}

		while ((totalPages >= maxLinks) && (toPage - fromPage + 1 < maxLinks)) {
			fromPage--;
		}

		var pageRange = [];

		for (var i = fromPage; i <= toPage; i++) {
			pageRange.push(i);
		}

		// .paginator so we can differentiate one of our components
		// .pagination so we inherit Bootstrap styling
        return <nav className="paginator" aria-label={description} ref={this.paginator}>
			<ul className="pagination">
				{/* first page */}
				<li className="page-item first-page">
					<button type="button" className="page-link" onClick={() => this.changePage(1)} disabled={currentPage <= 1}>
						<i className={firstIcon}></i>
						<span className="sr-only">
							First page
						</span>
					</button>
				</li>
				{/* previous page */}
				<li className="page-item prev-page">
					<button type="button" className="page-link" onClick={() => this.changePage(currentPage - 1)} disabled={currentPage <= 1}>
						<i className={prevIcon}></i>
						<span className="sr-only">
							Previous page
						</span>
					</button>
				</li>
				{/* individual page links */}
				{
					this.renderPageLinks(pageRange, currentPage, totalPages)
				}
				{/* next page */}
				<li className="page-item next-page">
					<button type="button" className="page-link" onClick={() => this.changePage(currentPage + 1)} disabled={currentPage == totalPages}>
						<i className={nextIcon}></i>
						<span className="sr-only">
							Next page
						</span>
					</button>
				</li>
				{/* last page */}
				<li className="page-item last-page">
					<button type="button" className="page-link" onClick={() => this.changePage(totalPages)} disabled={currentPage == totalPages}>
						<i className={lastIcon}></i>
						<span className="sr-only">
							Last page
						</span>
					</button>
				</li>
			</ul>
			<div className="pagination-overview">
				{showInput && <>
					<label className="page-label" for={this.props.id || this.fieldId}>
						Viewing page
					</label>
					<input className="form-control" type="text" id={this.props.id || this.fieldId} value={this.props.pageIndex || '1'}
						onkeyUp={e => {
							if (e.keyCode == 13) {
								this.changePage(e.target.value);
							}
						}} />

					{!!totalPages &&
						<span className="field-label"> of {totalPages}</span>
					}
				</>}

				{!showInput && <p className="field-label">
					Viewing page {currentPage}
					{!!totalPages &&
						<span> of {totalPages}</span>
					}
				</p>
				}

			</div>
		</nav>;
	}
}

// propTypes are used to describe configuration on your component in the editor.
// Just setting it to an empty object will make your component appear as something that can be added.
// Define your available props like the examples below.

Paginator.propTypes = {
	always: 'bool'
};