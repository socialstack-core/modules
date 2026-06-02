//@ts-ignore
import Summary from './Summary';
//@ts-ignore
import Content from './Content';
import { useEffect, useState, useRef } from 'react';

interface ExpanderProps {
	id?: string;
	name?: string;
	label?: string;
	summaryChildren?: React.ReactNode;
	open?: boolean;
	className?: string;
	children: React.ReactNode;
}

/**
 * The Expander React component.
 * @param props React props.
 */
const Expander: React.FC<React.PropsWithChildren<ExpanderProps>> = ({
	id, name, label, summaryChildren, open, className, children
}) => {
	const [animSupported, setAnimSupported] = useState(false);
	const expanderRef = useRef<HTMLDetailsElement>(null);

	let animation: Animation | null = null;
	let isClosing = false;
	let isExpanding = false;

	const componentClasses = ['ui-expander'];

	if (className?.length) {
		componentClasses.push(className);
	}

	useEffect(() => {
		function clickHandler(e: MouseEvent) {
			// fallback for when we don't support the CSS-only animation method
			// (Firefox / Safari, as of March 2025)
			if (!animSupported) {
				e.preventDefault();
				const ele = expanderRef.current;

				if (!ele) {
					return;
				}
				ele.style.overflow = 'hidden';

				if (isClosing || !ele.open) {
					expand();
				} else if (isExpanding || ele.open) {
					contract();
				}
			}
		}

		/*
		function toggleHandler(e: ToggleEvent) {
			/*
	
			if (e.target.open && isOpening) {
				return;
			}
	
			if (!e.target.open && isClosing) {
				return;
			}
	
			if (e.target.open) {
				console.log("open: ", e.target);
			} else {
				console.log("closed: ", e.target);
			}
	
			if (!animSupported) {
				e.preventDefault();
	
				clickHandler(e);
			}
		}
		*/

		setAnimSupported(CSS.supports("interpolate-size", "allow-keywords"));

		if (expanderRef.current) {
			expanderRef.current.addEventListener("click", clickHandler);
			// expanderRef.current.addEventListener("toggle", toggleHandler);
		}

		return () => {

			if (expanderRef.current) {
				expanderRef.current.removeEventListener("click", clickHandler);
				// expanderRef.current.removeEventListener("toggle", toggleHandler);
			}

		};

	});

	function expand() {
		const ele = expanderRef.current;

		if (!ele) {
			return;
		}
		ele.style.height = `${ele.offsetHeight}px`;
		ele.open = true;

		// wait for the next frame to trigger the expansion
		window.requestAnimationFrame(() => expand2());
	}

	function expand2() {
		const ele = expanderRef.current;

		if (!ele) {
			return;
		}
		isExpanding = true;
		let summary = ele.querySelector("summary") as HTMLElement;
		let content = ele.querySelector('.ui-expander__content') as HTMLElement;

		if (!summary || !content) {
			return;
		}

		// Get the current fixed height of the element
		const startHeight = `${ele.offsetHeight}px`;

		// Calculate the open height of the element (summary height + content height)
		const endHeight = `${summary.offsetHeight + content.offsetHeight}px`;

		// cancel if animation is already running
		if (animation) {
			animation.cancel();
		}

		// Start a WAAPI animation
		animation = ele.animate({
			// Set the keyframes from the startHeight to endHeight
			height: [startHeight, endHeight]
		}, {
			duration: 600,
			easing: 'ease'
		});

		if (animation) {
			animation.onfinish = () => onAnimationFinish(true);
			animation.oncancel = () => isExpanding = false;
		}

	}

	function contract() {
		const ele = expanderRef.current;

		if (!ele) {
			return;
		}
		isClosing = true;
		let summary = ele.querySelector("summary");

		if (!summary) {
			return;
		}

		// Store the current height of the element
		const startHeight = `${ele.offsetHeight}px`;

		// Calculate the height of the summary
		const endHeight = `${summary.offsetHeight}px`;

		// cancel if animation is already running
		if (animation) {
			animation.cancel();
		}

		// Start a WAAPI animation
		animation = ele.animate({
			// Set the keyframes from the startHeight to endHeight
			height: [startHeight, endHeight]
		}, {
			duration: 600,
			easing: 'ease'
		});

		if (animation) {
			animation.onfinish = () => onAnimationFinish(false);
			animation.oncancel = () => isClosing = false;
		}

	}

	function onAnimationFinish(open: boolean) {
		const ele = expanderRef.current;

		if (!ele) {
			return;
		}
		ele.open = open;

		animation = null;
		isClosing = false;
		isExpanding = false;

		ele.style.height = '';
		ele.style.overflow = '';
	}

	return (
		<details className={componentClasses.join(' ')} id={id} name={name} open={open === true ? true : undefined} ref={expanderRef}>
			{/* assume <Summary> and <Content> are already supplied */}
			{!label && !summaryChildren && <>
				{children}
			</>}

			{(label || summaryChildren) && <>
				<Summary label={label}>
					{summaryChildren}
				</Summary>
				<Content>
					{children}
				</Content>
			</>}
		</details>
	);

}

export default Expander;