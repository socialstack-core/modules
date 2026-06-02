import PopoverWrapper from 'UI/Popover/Wrapper';
// @ts-ignore
import popoverPolyfillJs from './static/popover.min.js';
import { lazyLoad } from 'UI/Functions/WebRequest';
import { getUrl } from 'UI/FileRef';
import {FocusEvent, useEffect, useRef} from 'react';
//import { toggleFocusable } from 'UI/Functions/ToggleFocusable';

export type PopoverAlignment = 'left' | 'right' | 'top' | 'bottom' | 'center' | 'maximize';
export type PopoverAutoClose = 'never' | 'always' | 'when-bg-disabled';
const DEFAULT_METHOD = 'auto';
const DEFAULT_ALIGNMENT = 'left';

export { PopoverWrapper };

// Define the shape of the Ref value (Preact component instance style)
export interface PreactComponentRef {
	base: HTMLElement;
}

/**
 * Props for the Popover component.
 */
interface PopoverProps {
	/**
	 * popover alignment (see PopoverAlignment for supported options) 
	 */
	alignment?: PopoverAlignment,

	ref?: React.RefObject<PreactComponentRef | null>,

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
	method?: "" | "auto" | "manual",

	/**
	 * optional additional classes
	 */
	className?: string,

	/**
	 * unique ID
	 */
	id?: string,

	/**
	 * set true if background should blur when popover is open
	 */
	blurBackground?: boolean,

	/**
	 * set true if top should be aligned underneath header
	 */
	underHeader?: boolean,

	/**
	 * set true if top should be aligned underneath subheader
	 */
	underSubHeader?: boolean,

	/**
	 * set true if bottom should be aligned above footer
	 */
	aboveFooter?: boolean,

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
	 * set to true to have the popover open by default (only works with method="manual")
	 */
	open?: boolean,

	/**
	 * The HTML wrapping tag to use (defaults to div if not supplied)
	 */
	tag?: 'div' | 'span',

	/**
	 * set true to disable scroll position locking on popover display
	 */
	disableScrollLock?: boolean,

	/**
	 * sets when popover automatically closes if any internal link or button clicked (enforces popover="manual")
	 * [never, always, or when-bg-disabled] (defaults to never)
	 * NB: when-bg-disabled relies upon bgDisabledWidth
	 */
	closeOnInteractiveClick?: PopoverAutoClose

	/**
	 * used in conjunction with closeOnInteractiveClick; determines the width under which the background is considered disabled
	 */
	bgDisabledWidth?: number,

	/**
	 * optional handler for toggle event
	 * @param e
	 * @returns
	 */
	onToggle?: (e: ToggleEvent) => void
}

/**
 * The Popover React component.
 * @param props React props.
 */
const PopoverRoot: React.FC<React.PropsWithChildren<PopoverProps>> = (props) => {
	const { className, id, children, blurBackground, underHeader, underSubHeader, aboveFooter, backgroundActive, onToggle,
		tabletPortraitVisible, tabletLandscapeVisible, desktopVisible, tag, disableScrollLock, open } = props;
	var method = props.method || DEFAULT_METHOD;
	var closeOnInteractiveClick = props.closeOnInteractiveClick || "never";

	if (closeOnInteractiveClick == "always" || closeOnInteractiveClick == "when-bg-disabled") {
		method = "manual";
	}

	const alignment = props.alignment || DEFAULT_ALIGNMENT;
	const bgDisabledWidth = props.bgDisabledWidth || 1024;
	const popoverRef = useRef<HTMLElement>(null);

	// TODO: investigate use of scrollbar-gutter: stable to prevent page content horizontally shifting

	// Handle open prop for method="manual"
	useEffect(() => {
		if (method === "manual" && open && popoverRef.current) {
			try {
				popoverRef.current.showPopover();
			} catch (e) {
				// Already open
			}
		}
	}, [open, method]);

	// TODO: investigate use of scrollbar-gutter: stable to prevent page content horizontally shifting

	function useScrollLockPopover() {
		const scrollYRef = useRef(0);

		useEffect(() => {
			const popover = popoverRef?.current;
			
			if (!popover) {
				return;
			}

			function handleToggle(e: ToggleEvent) {

				if (typeof onToggle === "function") {
					onToggle(e);
				}

				const isOpen = e.newState === "open";

				// update data-* attribute flags on <body>
				// (available as an alternative to body:has() rules)
				if (isOpen) {
					document.body.dataset.popoverOpen = "true";
					document.body.dataset.popoverFullHeight = (!underHeader && !underSubHeader && !aboveFooter).toString();
					document.body.dataset.popoverBlurred = blurBackground?.toString();
				} else {
					delete document.body.dataset.popoverOpen;
					delete document.body.dataset.popoverFullHeight;
					delete document.body.dataset.popoverBlurred;
				}

				if (disableScrollLock) {
					return;
				}

				if (isOpen) {
					// Save scroll position
					scrollYRef.current = window.scrollY;

					// Lock the body
					document.body.dataset.scrollFixed = 'true';
				} else {

					setTimeout(() => {
						// Unlock the body
						delete document.body.dataset.scrollFixed;

						// Restore scroll
						window.scrollTo({
							left: 0,
							top: scrollYRef.current,
							behavior: 'instant'
						});
					}, 100);

				}
			}

			popover.addEventListener("toggle", handleToggle as EventListener);
			return () => {
				popover.removeEventListener("toggle", handleToggle as EventListener);

				delete document.body.dataset.popoverOpen;
				delete document.body.dataset.popoverFullHeight;
				delete document.body.dataset.popoverBlurred;
			}

		}, [id]);
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
			lazyLoad(getUrl(popoverPolyfillJs as string)!);
		}

		document.addEventListener("click", docClickHandler);

		if (popoverRef.current) {
			(popoverRef.current as HTMLElement).addEventListener("focusout", focusHandler);
			(popoverRef.current as HTMLElement).addEventListener("click", clickHandler);
		}

		return () => {
			delete document.body.dataset.scrollFixed;

			if (popoverRef.current) {
				(popoverRef.current as HTMLElement).removeEventListener("click", clickHandler);
				(popoverRef.current as HTMLElement).removeEventListener("focusout", focusHandler);
			}

			document.removeEventListener("click", docClickHandler);
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
				// Check if another popover is open and has focus - if so, don't interfere
				// This prevents focus jumping when multiple popovers are open simultaneously
				const otherPopovers = document.querySelectorAll('[popover]:popover-open');
				let otherPopoverHasFocus = false;
				otherPopovers.forEach((popover) => {
					if (popover !== popoverRef.current && popover.contains(document.activeElement)) {
						otherPopoverHasFocus = true;
					}
				});

				if (otherPopoverHasFocus) {
					return;
				}

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
							(els[els.length - 1] as HTMLElement).focus();
						} else if (e.target == els[els.length - 1]) {
							// if we tabbed away from the last focusable item, wrap to the start
							(els[0] as HTMLElement).focus();
						}

					} else {
						(popoverRef.current as HTMLElement).hidePopover();
					}

				}	
			}
		}, 0);
	};

	// checks for clicks within the popover - handy for auto-closing on selections made
	const clickHandler = (e: Event) => {
		// was this click event via a child link or button?
		const ele = e.target as HTMLElement;
		const isInteractiveTarget = ele?.closest('.ui-link, .ui-btn');

		if (!isInteractiveTarget) {
			return;
		}

		switch (closeOnInteractiveClick) {
			case 'always':
				popoverRef.current?.hidePopover();
				break;

			case 'when-bg-disabled':

				if (window.innerWidth < bgDisabledWidth) {
					popoverRef.current?.hidePopover();
				}

				break;

			default:
				return;
		}

	};

	// used to check for clicks occuring outside the popover
	// (for when we're using popover="manual" but need to mimic popover="auto" at a specific size, e.g. mobile)
	const docClickHandler = (e: Event) => {

		if (closeOnInteractiveClick == "when-bg-disabled") {

			if (window.innerWidth < bgDisabledWidth) {
				const ele = e.target as HTMLInputElement;
				const isOutsidePopover = !popoverRef.current?.contains(ele);
				const isTrigger = ele?.popoverTargetElement == popoverRef.current;

				if (isOutsidePopover && !isTrigger) {
					popoverRef.current?.hidePopover();
				}

			}

		}
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
	}

	if (underSubHeader) {
		popoverClasses.push("ui-popover--under-subheader");
	}

	if (aboveFooter) {
		popoverClasses.push("ui-popover--above-footer");
	}

	if (!underHeader && !underSubHeader && !aboveFooter) {
		popoverClasses.push("ui-popover--full");
	}

	if (tabletPortraitVisible) {
		popoverClasses.push("ui-popover--tablet-portrait-visible");
	}
	if (tabletLandscapeVisible) {
		popoverClasses.push("ui-popover--tablet-landscape-visible");
	}
	if (desktopVisible) {
		popoverClasses.push("ui-popover--desktop-visible");
	}

	if (className) {
		popoverClasses.push(className);
	}

	useScrollLockPopover();

	const Tag = tag || "div";

	return (
		<Tag className={popoverClasses.join(' ')} popover={method} id={id} ref={popoverRef as React.RefObject<any>}>
			{children}
		</Tag>
	);
}

const Popover = PopoverRoot;
export default Popover;