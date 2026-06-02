/**
 * The toggleFocusable React component.
 * @param container parent element
 * @param focusable false if all focusable child elements should receive 'tabindex="-1"'
 * @returns void
*/
export function toggleFocusable(container: HTMLElement, focusable: boolean): void {

	if (!container) {
		return;
	}

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

	const els = container.querySelectorAll(focusableSelectors);

	els.forEach(el => {

		if (!focusable) {
			el.setAttribute("data-tabindex", el.getAttribute("tabindex") || "");
			el.setAttribute("tabindex", "-1");
		} else {
			const old = el.getAttribute("data-tabindex");
			el.removeAttribute("data-tabindex");

			if (!old) {
				el.removeAttribute("tabindex");
			} else {
				el.setAttribute("tabindex", old);
			}

		}
	});
}
