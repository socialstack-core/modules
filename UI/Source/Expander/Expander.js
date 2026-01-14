import Summary from './Summary';
import Content from './Content';
import { useEffect, useState, useRef } from 'react';

const COMPONENT_PREFIX = 'ui-expander';

export default function Expander(props) {
	const { id, name, label, summaryChildren, open, className, children } = props;
	const [animSupported, setAnimSupported] = useState(false);
	const expanderRef = useRef();
	let animation = null;
	let isClosing = false;
	let isExpanding = false;

	let componentClasses = [COMPONENT_PREFIX];

	if (className) {
		componentClasses.push(className);
	}

	useEffect(() => {
		setAnimSupported(CSS.supports("interpolate-size", "allow-keywords"));

		if (expanderRef.current) {
			expanderRef.current.addEventListener("click", clickHandler);
			expanderRef.current.addEventListener("toggle", toggleHandler);
		}

		return () => {

			if (expanderRef.current) {
				expanderRef.current.removeEventListener("click", clickHandler);
				expanderRef.current.removeEventListener("toggle", toggleHandler);
			}

		};

	});

	function clickHandler(e) {

		// fallback for when we don't support the CSS-only animation method
		// (Firefox / Safari, as of March 2025)
		if (!animSupported) {
			e.preventDefault();

			expanderRef.current.style.overflow = 'hidden';

			if (isClosing || !expanderRef.current.open) {
				expand();
			} else if (isExpanding || expanderRef.current.open) {
				contract();
			}

		}

	}

	function expand() {
		expanderRef.current.style.height = `${expanderRef.current.offsetHeight}px`;
		expanderRef.current.open = true;

		// wait for the next frame to trigger the expansion
		window.requestAnimationFrame(() => expand2());
	}

	function expand2() {
		isExpanding = true;
		let summary = expanderRef.current.querySelector("summary");
		let content = expanderRef.current.querySelector(`.${COMPONENT_PREFIX}__content`);

		if (!summary || !content) {
			return;
		}

		// Get the current fixed height of the element
		const startHeight = `${expanderRef.current.offsetHeight}px`;

		// Calculate the open height of the element (summary height + content height)
		const endHeight = `${summary.offsetHeight + content.offsetHeight}px`;

		// cancel if animation is already running
		if (animation) {
			animation.cancel();
		}

		// Start a WAAPI animation
		animation = expanderRef.current.animate({
			// Set the keyframes from the startHeight to endHeight
			height: [startHeight, endHeight]
		}, {
			duration: 600,
			easing: 'ease'
		});

		animation.onfinish = () => onAnimationFinish(true);
		animation.oncancel = () => isExpanding = false;
	}

	function contract() {
		isClosing = true;
		let summary = expanderRef.current.querySelector("summary");

		if (!summary) {
			return;
		}

		// Store the current height of the element
		const startHeight = `${expanderRef.current.offsetHeight}px`;

		// Calculate the height of the summary
		const endHeight = `${summary.offsetHeight}px`;

		// cancel if animation is already running
		if (animation) {
			animation.cancel();
		}

		// Start a WAAPI animation
		animation = expanderRef.current.animate({
			// Set the keyframes from the startHeight to endHeight
			height: [startHeight, endHeight]
		}, {
			duration: 600,
			easing: 'ease'
		});

		animation.onfinish = () => onAnimationFinish(false);
		animation.oncancel = () => isClosing = false;
	}
	
	function onAnimationFinish(open) {
		expanderRef.current.open = open;

		animation = null;
		isClosing = false;
		isExpanding = false;

		expanderRef.current.style.height = '';
		expanderRef.current.style.overflow = '';
	}

	function toggleHandler(e) {
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

		*/
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

Expander.propTypes = {
	name: 'string',
	label: 'string',
	open: 'boolean',
	className: 'string',
	summaryChildren: 'jsx'
};

Expander.defaultProps = {
}
