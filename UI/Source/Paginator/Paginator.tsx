import React, {useState, useEffect} from "react";
import Icon from 'UI/Icon';
import Button from 'UI/Button';

const MAX_PAGES = 5;

let lastId = 0;

function newId() {
    lastId++;
    return `paginator_${lastId}`;
}

/**
 * Props for the Paginator component.
 */
interface PaginatorProps {
    id?: string,
    pageIndex?: number,
    totalResults?: number,
    pageSize: number,
    /**
     * By default the paginator hides if it is not needed. This makes it always show.
     */
    always?: boolean,
    /**
     * Optional custom icon for getting to the first page, usually an <Icon/>
     */
    firstIcon?: React.ReactNode,
    /**
     * Optional custom icon for getting to the previous page, usually an <Icon/>
     */
    previousIcon?: React.ReactNode,
    /**
     * Optional custom icon for getting to the next page, usually an <Icon/>
     */
    nextIcon?: React.ReactNode,
    /**
     * Optional custom icon for getting to the last page, usually an <Icon/>
     */
    lastIcon?: React.ReactNode,
    /**
     * Optional result description.
     */
    description?: string,
    showInput?: boolean,
    showSummary?: boolean,
    maxLinksMobile?: number,
    maxLinks?: number,
    /**
     * Event which runs when the page number is changed.
     * @param toPage
     * @param currentPage
     * @returns
     */
    onChange?: (toPage: number, currentPage: number) => void,
	/**
	 * Does the URL change when there is a change to a page index
	 */
	urlUpdating?: boolean,

	/**
	 * set true to render paginator only (no overview)
	 */
	paginatorOnly?: boolean,

	/**
	 * set true to render overview only (no paginator)
	 */
	overviewOnly?: boolean,

	/** 
	 * set true to have paginator dock to bottom of parent
	 */
	dockBottom?: boolean
}

/**
 * Standalone component which displays a paginator.
 */
const Paginator: React.FC<PaginatorProps> = (props: PaginatorProps) => {
	var { pageIndex, totalResults, pageSize, paginatorOnly, overviewOnly, dockBottom } = props;

    const [dropdownId, setDropdownId] = useState<string>();

    useEffect(() => {
        if (!dropdownId) {
            if (props.id) {
                setDropdownId(props.id);
            } else {
                setDropdownId(newId())
            }
        }
    }, [dropdownId, props?.id])

    const [currentPage, setCurrentPage] = useState(pageIndex || 1);

    let totalPages = getTotalPages();

    const changePage = function (nextPage: number) {
        if (!nextPage || nextPage <= 0) {
            nextPage = 1;
        }

        //var totalPages = getTotalPages();

        if (totalPages && nextPage > totalPages) {
            nextPage = totalPages;
        }

        if (props.onChange) {
            props.onChange(nextPage, currentPage);
        }

        if (!props.urlUpdating) {
			setCurrentPage(nextPage);
		}
    }

    useEffect(() => {
        // something external has changed the results 
        if (currentPage && pageIndex && currentPage != pageIndex) {
            changePage(pageIndex);
        }

    }, [pageIndex, totalResults, currentPage]);

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
    var firstIcon = props.firstIcon || <Icon type="fa-fast-backward" solid/>;
    var prevIcon = props.previousIcon || <Icon type="fa-play" solid horizontalFlip/>;
    var nextIcon = props.nextIcon || <Icon type="fa-play" solid/>;
    var lastIcon = props.lastIcon || <Icon type="fa-fast-forward" solid/>;


    var showInput = props.showInput !== undefined ? props.showInput : true;
    var showSummary = props.showSummary !== undefined ? props.showSummary : !showInput;
    var maxLinks = props.maxLinks || MAX_PAGES;

    var showFirstLastNav = true;
    var showPrevNextNav = true;

    function getTotalPages() {

        if (totalResults) {
            return Math.ceil(totalResults / pageSize);
        }

        return 0;
    }

    function changePageStr(newPageId: string) {
        try {
            var nextPage = parseInt(newPageId, 10);
            changePage(nextPage);
        } catch {
            // E.g. user typed in something that isn't a number
            return;
        }
    }

    function renderPaginator(description: string, maxLinks: number) {
        let paginatorClass = ['paginator'];

		if (dockBottom) {
			paginatorClass.push('paginator--bottom');
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

        var pageRange: number[] = [];

        for (var i = fromPage; i <= toPage; i++) {
            pageRange.push(i);
        }

        return <>
			<nav className={paginatorClass.join(' ')} aria-label={description}>
				{!overviewOnly && <>
					<ul className="pagination">
						{/* first page */}
						{showFirstLastNav &&
							<li className="page-item first-page">
								<Button className="page-link" sm onClick={() => changePage(1)}
									disabled={currentPage <= 1} title={`First page`}>
									{firstIcon}
									<span className="sr-only">
										{`First page`}
									</span>
								</Button>
							</li>
						}
						{/* previous page */}
						{showPrevNextNav &&
							<li className="page-item prev-page">
								<Button className="page-link" sm onClick={() => changePage(currentPage - 1)}
									disabled={currentPage <= 1} title={`Previous page`}>
									{prevIcon}
									<span className="sr-only">
										{`Previous page`}
									</span>
								</Button>
							</li>
						}

						{/* individual page links */}
						{renderPageLinks(pageRange)}

						{/* next page */}
						{showPrevNextNav &&
							<li className="page-item next-page">
								<Button className="page-link" sm onClick={() => changePage(currentPage + 1)}
									disabled={currentPage == totalPages} title={`Next page`}>
									{nextIcon}
									<span className="sr-only">
										{`Next page`}
									</span>
								</Button>
							</li>
						}
						{/* last page */}
						{showFirstLastNav &&
							<li className="page-item last-page">
								<Button className="page-link" sm onClick={() => changePage(totalPages)}
									disabled={currentPage == totalPages} title={`Last page`}>
									{lastIcon}
									<span className="sr-only">
										{`Last page`}
									</span>
								</Button>
							</li>
						}
					</ul>
				</>}

				{!paginatorOnly && <>
					<div className="pagination-overview">
						{showInput && <>
							<label className="page-label" htmlFor={dropdownId}>
								{`Viewing page`}
							</label>
							<input className="form-control" type="text" id={dropdownId} value={pageIndex || '1'}
								onKeyUp={(e: React.KeyboardEvent<HTMLInputElement>) => {
									if (e.keyCode == 13) {
										changePageStr((e.target as HTMLInputElement).value);
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
				</>}

            </nav>
        </>;

    }

    function renderPageLinks(pageRange: number[]) {
        return pageRange.map((page: number) => renderPage(page));
    }

	function renderPage(page: number) {
        var isCurrentPage = page == currentPage;
        var pageClass = isCurrentPage ? "page-item active" : "page-item";
        var isEmpty = page < 1 || page > totalPages;

        return <li className={pageClass}>
            {!isCurrentPage && !isEmpty &&
                <Button className="page-link" sm onClick={() => changePage(page)}>
                    {page}
                </Button>
            }
            {isCurrentPage && !isEmpty &&
                <span className="btn ui-btn btn--sm page-link">
					{page}
				</span>
            }
            {isEmpty &&
				<span className="btn ui-btn btn--sm page-link empty">
					&nbsp;
				</span>
            }
        </li>;
    }

    return <>
        {renderPaginator(description, maxLinks)}
    </>;
}

export default Paginator;