import React, { useEffect, useRef } from "react";

export type LazyLoaderProps = {
	/** Called when the sentinel becomes visible AND we still have more to load. */
	onLoadMore: () => void | Promise<void>;
	/** If false, the loader won’t trigger load-more (and detaches once done). */
	hasMore: boolean;
	/** Prevents re-triggering while a page is in-flight. */
	isLoading?: boolean;

	/** IntersectionObserver options */
	root?: Element | null; // what counts as the viewport for intersection checks.
	rootMargin?: string; // This option expands the observer’s “viewport box”.
	threshold?: number | number[]; //how much of the element must be visible before the callback fires.

	/** Extra controls */
	className?: string; // sentinel styling
};

const LazyLoader = (props: LazyLoaderProps) => {

	const ref = useRef<HTMLDivElement | null>(null);

	const {
		onLoadMore,
		hasMore,
		isLoading = false,
		root = null, // use the window
		rootMargin = "600px 0px 0px 0px", // the viewport extends 600px upwards so prefetching
		threshold = 0, // trigger as soon as sentinel appears
		className
	}  = props;


	useEffect(() => {
		const el = ref.current;
			if (!el) {
				return;
			}

		const io = new IntersectionObserver(
			(entries) => {
				const entry = entries[0];

				const isVisible = entry.isIntersecting;

				if (isVisible && hasMore && !isLoading) {
					// request more content 
					onLoadMore();
				}
			},
			{ root, rootMargin, threshold }
		);

		io.observe(el);

		return () => {
			io.disconnect();
		};
	}, [root, rootMargin, threshold, hasMore, isLoading, onLoadMore]);

	// render the sentinel
	return (
		<>
		<div aria-hidden ref={ref} className={className ?? ""}/>
		</>
	);
}

export default LazyLoader;