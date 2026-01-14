import Button from 'UI/Button';

export type LoadMoreProps = {
	/** Called when the button is clicked AND we still have more to load. */
	onLoadMore: () => void | Promise<void>;
	/** If false, the loader won’t display the link */
	hasMore: boolean;
	/** Prevents re-triggering while a page is in-flight. */
	isLoading?: boolean;

    //** override the btn styling */
    className?: string; 
};

const LoadMore = (props: LoadMoreProps) => {

	const {
		onLoadMore,
		hasMore,
		isLoading = false,
		className
	}  = props;

    if (isLoading) {
        return ('');
    }

	return (
		<div className={`ui-load-more ${className ?? ""}`}>
		    <Button variant="secondary" onClick={() => onLoadMore()}>
					{`Load more`}
			</Button>
		</div>
	);
}

export default LoadMore;