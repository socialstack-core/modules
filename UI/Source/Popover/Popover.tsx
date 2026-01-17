import popoverPolyfillJs from './static/popover.min.js';
import { lazyLoad } from 'UI/Functions/WebRequest';
import { getUrl } from 'UI/FileRef';
import {FocusEvent, useEffect, useRef} from 'react';
//import { toggleFocusable } from 'UI/Functions/ToggleFocusable';

export type PopoverAlignment = 'left' | 'right' | 'top' | 'bottom' | 'center' | 'maximize';
const DEFAULT_METHOD = 'auto';
const DEFAULT_ALIGNMENT = 'left';

/**
 * Props for the Popover component.
 */
interface PopoverProps {
	/**
	 * popover alignment (see PopoverAlignment for supported options) 
	 */
	alignment?: PopoverAlignment,

	/**
	 * auto (default)
	 * - allow closing via clicking elsewhere / pressing esc
	 * 
	 * manual
	 * - can only be displayed / closed using declarative buttons or JavaScript
	 * 
	 * hint (DON'T USE)
	 * - doesn't close auto popovers but will close other hints
	 * NB: not to be used until we find a workaround for Firefox/Safari which currently don't support this
	 * 
	 */
	method?: string,

	/**
	 * optional additional classes
	 */
	className?: string,

	/**
	 * unique ID
	 */
	id: string,

	/**
	 * set true if background should blur when popover is open
	 */
	blurBackground?: boolean,

	/**
	 * set true if top should be aligned underneath header
	 */
	underHeader?: boolean,

	/**
	 * set true if background should remain accessible
	 */
	backgroundActive?: boolean,

	/**
	 * optional flags to indicate when popover should appear without trigger
	 * (e.g. filters - could be behind a trigger for mobile, but visible on desktop)
	 */
	tabletPortraitVisible?: boolean,	// 753px +
	tabletLandscapeVisible?: boolean,	// 1024px +
	desktopVisible?: boolean,			// 1360px +

	/**
	 * The HTML wrapping tag to use (defaults to div if not supplied)
	 */
	tag?: string
}

/**
 * The Popover React component.
 * @param props React props.
 */
const Popover: React.FC<React.PropsWithChildren<PopoverProps>> = (props) => {
	const { className, id, children, blurBackground, underHeader, backgroundActive,
		tabletPortraitVisible, tabletLandscapeVisible, desktopVisible, tag } = props;
	const method = props.method || DEFAULT_METHOD;
	const alignment = props.alignment || DEFAULT_ALIGNMENT;
	const popoverRef = useRef(undefined);

	// TODO: investigate use of scrollbar-gutter: stable to prevent page content horizontally shifting

	function useScrollLockPopover(popoverId: string) {
		const scrollYRef = useRef(0);

		useEffect(() => {

			if (!document) {
				return;
			}

			const popover = document.getElementById(popoverId);
			if (!popover) {
				return;
			}

			function handleToggle(e: ToggleEvent) {
				const isOpen = e.newState === "open";

				if (isOpen) {
					// Save scroll position
					scrollYRef.current = window.scrollY;

					// Lock the body
					document.body.dataset.scrollFixed = 'true';
				} else {
					// Unlock the body
					delete document.body.dataset.scrollFixed;

					// Restore scroll
					window.scrollTo({
						left: 0,
						top: scrollYRef.current,
						behavior: 'instant'
					});
				}
			}

			popover.addEventListener("toggle", handleToggle as EventListener);
			return () => popover.removeEventListener("toggle", handleToggle as EventListener);
		}, [popoverId]);
	}

	function isPopoverApiSupported() {
		const test = document.createElement('div');
		return 'popover' in test &&
			typeof HTMLElement.prototype.showPopover === 'function' &&
			typeof HTMLElement.prototype.hidePopover === 'function';
	}

	useEffect(() => {

		// check: iOS versions prior to v17 don't support popover API
		// lazy-load polyfill if required
		if (!isPopoverApiSupported()) {
			lazyLoad(getUrl(popoverPolyfillJs)!);
		}

		let minWidth = 0;

		if (tabletPortraitVisible) {
			minWidth = 753;
		}
		if (tabletLandscapeVisible) {
			minWidth = 1024;
		}
		if (desktopVisible) {
			minWidth = 1360;
		}

		// for those wondering "but ... why?!" with respect to CSS embedded in the component;
		// this is so behaviour can be controlled on a per-popover basis
		// (e.g. some popovers may blur the background, some may not)
		const mediaQuery = (minWidth == 0) ? '' : `
			@media only screen and (min-width: ${minWidth}px) {
				#${id} {
					position: static;
					visibility: visible;
					transition-property: box-shadow, overlay, display, visibility;
					transform: none;
					padding: 0;
					display: block;
					box-shadow: none;
					opacity: 1;
					width: 100%;
				}

				#${id}::backdrop {
					background-color: transparent !important;
				}

				[popovertarget="${id}"] {
					display: none;
				}
			}`;

		const backgroundRules = `
			filter: blur(2px) grayscale(25%);
			pointer-events: none;
		`;

		// NB: odd-looking ".\:popover-open" references are required by the popover API polyfill
		const styleEl = document.createElement('style');
		styleEl.textContent = `
			body:has(#${id}.ui-popover--blur-bg.\:popover-open) {
				#content {
					${backgroundRules}

					~ footer {
						${backgroundRules}
					}
				}
			}
			body:has(#${id}.ui-popover--blur-bg:popover-open) {
				#content {
					${backgroundRules}

					~ footer {
						${backgroundRules}
					}
				}
			}

			body:has(#${id}.ui-popover--full.ui-popover--blur-bg.\:popover-open) {
				#wrapper > header {
					${backgroundRules}
				}
			}
			body:has(#${id}.ui-popover--full.ui-popover--blur-bg:popover-open) {
				#wrapper > header {
					${backgroundRules}
				}
			}

			${mediaQuery}
		`;
		document.head.appendChild(styleEl);

		if (popoverRef.current) {
			(popoverRef.current as HTMLElement).addEventListener("focusout", focusHandler);
		}

		return () => {
			delete document.body.dataset.scrollFixed;

			if (popoverRef.current) {
				(popoverRef.current as HTMLElement).removeEventListener("focusout", focusHandler);
			}

			document.head.removeChild(styleEl);
		};
	}, []);

/*
	// disable background while popover is open (unless backgroundActive set),
	// - while we could use <dialog> to gain modal support, this can only be controlled via JavaScript
	// - it's also an all or nothing approach (e.g. we can't disable everything *except* the header)
	// - we use the Popover API to allow panels to be toggled without relying on JavaScript support
	// - can't use HTML inert attribute as this is liable to disable the contents of the popover itself
	// - note that CSS pointer-events rules are used to disable mouse interaction
	const toggleHandler = (event) => {

		if (backgroundActive) {
			return;
		}

		var reactRoot = window.SERVER ? undefined : document.querySelector("#react-root");

		if (!reactRoot) {
			return;
		}

		const header = reactRoot.querySelector("#wrapper > header");
		const content = reactRoot.querySelector("#wrapper > #content");
		const footer = reactRoot.querySelector("#wrapper > #content ~ footer");

		if (event.newState === "open") {

			if (header && !underHeader) {
				toggleFocusable(header, false);
			}

			if (content) {
				toggleFocusable(content, false);
			}

			if (footer) {
				toggleFocusable(footer, false);
			}

		} else {

			if (header && !underHeader) {
				toggleFocusable(header, true);
			}

			if (content) {
				toggleFocusable(content, true);
			}

			if (footer) {
				toggleFocusable(footer, true);
			}

		}

	};
*/
	// checks for focus leaving the popover - close if this happens, otherwise we run the risk of focusing a blurred background element
	const focusHandler = (e: Event) => {

		if (backgroundActive) {
			return;
		}

		// likely the user tapped the background of the popover
		if (method != "manual" && (e as unknown as FocusEvent).relatedTarget === document.body || (e as unknown as FocusEvent).relatedTarget === null) {
			return;
		}

		setTimeout(() => {
			if (popoverRef.current) {
				if (!(popoverRef.current as HTMLElement).contains(document.activeElement)) {
					const focusableSelectors = [
						"a[href]",
						"button:not([disabled])",
						"input:not([disabled]):not([type=hidden])",
						"select:not([disabled])",
						"textarea:not([disabled])",
						"summary:not([disabled])",
						"iframe:not([disabled])",
						"area[href]",
						"object:not([disabled])",
						"embed:not([disabled])",
						"audio:not([disabled])",
						"video:not([disabled])",
						"[tabindex]:not([tabindex='-1'])",
						"[contenteditable='true']"
					].join(",");

					const els = popoverRef.current.querySelectorAll(focusableSelectors);

					if (els?.length) {

						// if we tabbed away from the first focusable item, wrap to the end
						if (e.target == els[0]) {
							els[els.length - 1].focus();
						} else if (e.target == els[els.length - 1]) {
							// if we tabbed away from the last focusable item, wrap to the start
							els[0].focus();
						}

					} else {
						(popoverRef.current as HTMLElement).hidePopover();
					}

				}	
			}
		}, 0);
	};

	let popoverClasses = ['ui-popover'];
	popoverClasses.push(`ui-popover--${alignment}`);

	if (blurBackground) {
		popoverClasses.push("ui-popover--blur-bg");
	}

	if (backgroundActive) {
		popoverClasses.push("ui-popover--bg-active");
	}

	if (underHeader) {
		popoverClasses.push("ui-popover--under-header");
	} else {
		popoverClasses.push("ui-popover--full");
	}

	if (className) {
		popoverClasses.push(className);
	}

	useScrollLockPopover(id);

	const Tag = !tag?.length ? "div" : tag;

	return (
		<Tag className={popoverClasses.join(' ')} popover={method} id={id} ref={popoverRef}>
			{children}
		</Tag>
	);
}

export default Popover;