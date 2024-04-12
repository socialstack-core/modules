import { useState , useEffect } from "react";

const MAX_PAGES = 5;
const MAX_PAGES_MOBILE = 3;

let lastId = 0;

function newId() {
	lastId++;
	return `paginator_${lastId}`;
}

export default function Paginator(props) {
	var { pageIndex, totalResults, pageSize } = props;
	const dropdownId = props.id ? props.id : useState(newId())[0];

	const [currentPage, setCurrentPage] = useState(pageIndex || 1);

	let totalPages = getTotalPages();

	// if we only have a single page then optionally hide
	if (!props.always && totalPages && totalPages < 2) {
		return;
	}

	if (!pageIndex || pageIndex <= 0) {
		pageIndex = 1;
	}

	if (totalPages && pageIndex > totalPages) {
		pageIndex = totalPages;
	}

	var description = props.description || `Results`;
	var firstIcon = props.firstIcon || "fas fa-fast-backward";
	var prevIcon = props.prevIcon || "fas fa-play fa-xs fa-flip-horizontal";
	var nextIcon = props.nextIcon || "fas fa-play fa-xs";
	var lastIcon = props.lastIcon || "fas fa-fast-forward";

	/*
	// as a workaround for having to potentially send rebranded icons for each instance of <Paginator>
	// (or, more than likely, each instance of a paged <Loop>), check for overrides defined in a global context
	var globalSettings = useContext(PaginatorContext); // var PaginatorContext = React.createContext(); above

	if (typeof globalSettings == "object") {
		firstIcon = globalSettings.firstIcon || firstIcon;
		prevIcon = globalSettings.prevIcon || prevIcon;
		nextIcon = globalSettings.nextIcon || nextIcon;
		lastIcon = globalSettings.lastIcon || lastIcon;
	}
	*/

	var showInput = props.showInput !== undefined ? props.showInput : true;
	var showSummary = props.showSummary !== undefined ? props.showSummary : !showInput;
	var maxLinksMobile = props.maxLinksMobile || MAX_PAGES_MOBILE;
	var maxLinks = props.maxLinks || MAX_PAGES;

	var showFirstLastNav = true;
	var showPrevNextNav = true;

	function getTotalPages() {

		if (totalResults) {
			return Math.ceil(totalResults / pageSize);
		}

		return 0;
	}

	function changePage(newPageId) {

		try {
			var nextPage = parseInt(newPageId, 10);

			if (!nextPage || nextPage <= 0) {
				nextPage = 1;
			}

			//var totalPages = getTotalPages();

			if (totalPages && nextPage > totalPages) {
				nextPage = totalPages;
			}

			if (typeof props.onChange == 'function') {
				props.onChange(nextPage, props.pageIndex);
			}

			setCurrentPage(nextPage);
		} catch {
			// E.g. user typed in something that isn't a number
			return;
		}

	}

	function renderPaginator(description, maxLinks, mobile) {
		let paginatorClass = ['paginator'];

		if (mobile) {
			paginatorClass.push('paginator--mobile');
		}

		var fromPage, toPage;

		if (maxLinks % 2 == 0) {
			fromPage = currentPage - ((maxLinks / 2) - 1);
			toPage = currentPage + (maxLinks / 2);
		} else {
			fromPage = currentPage - ((maxLinks - 1) / 2);
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

		return <>
			<nav className={paginatorClass.join(' ')} aria-label={description}>
				<ul className="pagination">
					{/* first page */}
					{showFirstLastNav &&
						<li className="page-item first-page">
							<button type="button" className="page-link" onClick={() => changePage(1)} disabled={currentPage <= 1} title={`First page`}>
								<i className={firstIcon}></i>
								<span className="sr-only">
									{`First page`}
								</span>
							</button>
						</li>
					}
					{/* previous page */}
					{showPrevNextNav &&
						<li className="page-item prev-page">
							<button type="button" className="page-link" onClick={() => changePage(currentPage - 1)} disabled={currentPage <= 1} title={`Previous page`}>
								<i className={prevIcon}></i>
								<span className="sr-only">
									{`Previous page`}
								</span>
							</button>
						</li>
					}

					{/* individual page links */}
					{renderPageLinks(pageRange)}

					{/* next page */}
					{showPrevNextNav &&
						<li className="page-item next-page">
							<button type="button" className="page-link" onClick={() => changePage(currentPage + 1)} disabled={currentPage == totalPages} title={`Next page`}>
								<i className={nextIcon}></i>
								<span className="sr-only">
									{`Next page`}
								</span>
							</button>
						</li>
					}
					{/* last page */}
					{showFirstLastNav &&
						<li className="page-item last-page">
							<button type="button" className="page-link" onClick={() => changePage(totalPages)} disabled={currentPage == totalPages} title={`Last page`}>
								<i className={lastIcon}></i>
								<span className="sr-only">
									{`Last page`}
								</span>
							</button>
						</li>
					}
				</ul>

				<div className="pagination-overview">
					{showInput && <>
						<label className="page-label" for={dropdownId}>
							{`Viewing page`}
						</label>
						<input className="form-control" type="text" id={dropdownId} value={pageIndex || '1'}
							onkeyUp={e => {
								if (e.keyCode == 13) {
									changePage(e.target.value);
								}
							}} />

						{!!totalPages &&
							<span className="field-label">{`of ${totalPages}`}</span>
						}
					</>}

					{showSummary && <>
						<p className="field-label">
							{`Viewing page ${currentPage}`}
							{!!totalPages &&
								<span>{` of ${totalPages}`}</span>
							}
						</p>
					</>}
				</div>

			</nav>
		</>;

	}

	function renderPageLinks(pageRange) {
		return pageRange.map((page) => renderPage(page));
	}

	function renderPage(page) {
		var isCurrentPage = page == currentPage;
		var pageClass = isCurrentPage ? "page-item active" : "page-item";
		var isEmpty = page < 1 || page > totalPages;

		return <li className={pageClass}>
			{!isCurrentPage && !isEmpty &&
				<button type="button" className="page-link" onClick={() => changePage(page)}>
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

    useEffect(() => {
		// something external has changed the results 
		if (currentPage && currentPage != pageIndex) {
			changePage(pageIndex);
		}

    }, [pageIndex, totalResults]);

	return <>
		{renderPaginator(description, maxLinksMobile, true)}
		{renderPaginator(description, maxLinks, false)}
	</>;
}

Paginator.propTypes = {
	always: 'bool'
};