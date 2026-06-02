import { useEffect, useState, RefObject } from "react";

interface ScrollbarVisibility {
	verticalScroll: boolean;
	horizontalScroll: boolean;
}

export default function useScrollbarVisibility(ref: RefObject<HTMLElement>): ScrollbarVisibility {
	const [scrollbars, setScrollbars] = useState<ScrollbarVisibility>({
		verticalScroll: false,
		horizontalScroll: false,
	});

	useEffect(() => {
		const el = ref.current;

		if (!el) {
			return;
		}

		const checkScrollbars = () => {

			if (!ref.current) {
				return;
			}

			setScrollbars({
				verticalScroll: ref.current.scrollHeight > ref.current.clientHeight,
				horizontalScroll: ref.current.scrollWidth > ref.current.clientWidth,
			});
		};

		const resizeObserver = new ResizeObserver(checkScrollbars);
		const mutationObserver = new MutationObserver(checkScrollbars);

		resizeObserver.observe(el);
		mutationObserver.observe(el, {
			childList: true,
			subtree: true,
			characterData: true,
		});

		// Initial check
		checkScrollbars();

		// Cleanup function
		return () => {
			resizeObserver.disconnect();
			mutationObserver.disconnect();
		};
	}, [ref.current]); // Re-run effect if the element ref changes

	return scrollbars;
}