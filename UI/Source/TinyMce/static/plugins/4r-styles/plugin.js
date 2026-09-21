/**
 * 4 Roads Block Class Applicator
 *
 * Adds a toolbar dropdown that applies a CSS class to either:
 *   - The selected text (wrapped in a <span>), or
 *   - The parent block element (H1, H2, P, DIV, etc.) when
 *     the caret is simply placed within a block with no selection.
 *
 * BEHAVIOUR
 * ---------
 * - Text selected
 * wraps selection in <span class="chosen-class">
 * (re-selecting a span that already has that class removes it instead)
 * 
 * - Caret only
 * adds/removes the class on the nearest block ancestor
 * 
 * - Remove class
 * strips the class from block or span as appropriate
 * 
 * - Checkmark
 * shown next to any class that is active at the current caret position or selection
 */

(function () {

	tinymce.PluginManager.add('4r-styles', (editor, url) => {

		editor.options.register('fr_styles_classes', {
			processor: 'object[]',
			default: []
		});

		// helpers
		const BLOCK_TAGS = /^(ADDRESS|ARTICLE|ASIDE|BLOCKQUOTE|DD|DIV|DL|DT|
		FIELDSET | FIGCAPTION | FIGURE | FOOTER | FORM | H[1 - 6] | HEADER |
			HR | LI | MAIN | NAV | OL | P | PRE | SECTION | TABLE | UL)$ / i;

		/** Return the nearest block ancestor (or self) of node. */
		function nearestBlock(node) {
			let n = node;
			while (n && n !== editor.getBody()) {
				if (BLOCK_TAGS.test(n.nodeName)) return n;
				n = n.parentNode;
			}
			return editor.getBody();
		}

		/** True when there is no text selection (caret only). */
		function isCollapsed() {
			return editor.selection.isCollapsed();
		}

		/**
		 * Returns the single <span> that completely contains the selection,
		 * or null when the selection covers mixed / partial content.
		 *
		 * Covers three cases:
		 *   1. getNode() IS the span (click inside a span, or span fully selected).
		 *   2. A text node fully selected whose sole parent is a span.
		 *   3. The formatter already applied the class — check via editor.formatter.
		 */
		function getSelectedSpan() {
			const node = editor.selection.getNode();
			if (node && node.nodeName === 'SPAN') return node;

			const rng = editor.selection.getRng();
			const sc = rng.startContainer;
			const ec = rng.endContainer;
			if (
				sc === ec &&
				sc.nodeType === Node.TEXT_NODE &&
				sc.parentNode &&
				sc.parentNode.nodeName === 'SPAN' &&
				rng.startOffset === 0 &&
				rng.endOffset === sc.length
			) {
				return sc.parentNode;
			}

			return null;
		}

		/**
		 * True if className is active at the current selection.
		 *
		 * Priority:
		 *   - Caret: check nearest block element's classList
		 *   - Select: check wrapping span, then fall back to editor.formatter
		 *     so partial / formatter-applied spans are also detected.
		 */
		function isClassActive(className) {
			if (isCollapsed()) {
				const block = nearestBlock(editor.selection.getStart());
				return !!(block && block.classList.contains(className));
			}

			// Direct span check
			const span = getSelectedSpan();
			if (span) return span.classList.contains(className);

			// Formatter-based check (handles partial / multi-node selections)
			const fmtName = '_bca_check_' + className;
			if (!editor.formatter.get(fmtName)) {
				editor.formatter.register(fmtName, {
					inline: 'span',
					classes: [className],
					remove: 'all',
				});
			}
			return editor.formatter.match(fmtName);
		}

		// core apply / remove logic
		function applyClass(className) {
			if (!className) return;

			editor.undoManager.transact(function () {

				if (isCollapsed()) {
					// CARET MODE: toggle class on parent block
					const block = nearestBlock(editor.selection.getStart());
					if (!block || block === editor.getBody()) return;

					if (block.classList.contains(className)) {
						block.classList.remove(className);
						if (!block.getAttribute('class')) block.removeAttribute('class');
					} else {
						block.classList.add(className);
					}

				} else {
					// SELECTION MODE
					const span = getSelectedSpan();

					if (span && span.classList.contains(className)) {
						// Already wrapped in this class — unwrap
						editor.dom.remove(span, true);
					} else if (span) {
						// Wrapped in a different class — swap
						span.classList.remove(...Array.from(span.classList));
						span.classList.add(className);
					} else {
						// Use formatter so TinyMCE handles split / merge properly
						const fmtName = '_bca_' + className;
						if (!editor.formatter.get(fmtName)) {
							editor.formatter.register(fmtName, {
								inline: 'span',
								classes: [className],
								remove: 'all',
								split: true,
								expand: false,
								deep: false,
							});
						}

						// Toggle: remove if already applied, apply if not
						if (editor.formatter.match(fmtName)) {
							editor.formatter.remove(fmtName);
						} else {
							editor.formatter.apply(fmtName);
						}
					}
				}

				editor.nodeChanged();
			});
		}

		function removeClass() {
			editor.undoManager.transact(function () {

				if (isCollapsed()) {
					const block = nearestBlock(editor.selection.getStart());
					if (!block || block === editor.getBody()) return;
					getClassItems().forEach(function (item) {
						block.classList.remove(item.value);
					});
					if (!block.getAttribute('class')) block.removeAttribute('class');

				} else {
					const span = getSelectedSpan();
					if (span) {
						getClassItems().forEach(function (item) {
							span.classList.remove(item.value);
						});
						if (!span.getAttribute('class')) editor.dom.remove(span, true);
					} else {
						getClassItems().forEach(function (item) {
							const fmtName = '_bca_' + item.value;
							if (!editor.formatter.get(fmtName)) {
								editor.formatter.register(fmtName, {
									inline: 'span',
									classes: [item.value],
									remove: 'all',
								});
							}
							editor.formatter.remove(fmtName);
						});
					}
				}

				editor.nodeChanged();
			});
		}


		// dropdown items
		function getClassItems() {
			return editor.options.get('fr_styles_classes') || [
				{ title: 'Uppercase', value: 'fr-styles-uppercase' },
				{ title: 'Expanded', value: 'fr-styles-expanded' }
			];
		}

		function buildMenuItems() {
			const items = getClassItems().map(function (item) {
				return {
					// togglemenuitem renders a checkmark glyph when setActive(true) is called.
					// Regular menuitem only highlights the row — no checkmark.
					type: 'togglemenuitem',
					text: item.title,

					onSetup: function (api) {
						function updateState() {
							api.setActive(isClassActive(item.value));
						}

						// Reflect state immediately when the menu opens
						updateState();

						// Keep state in sync as the caret moves
						editor.on('NodeChange SelectionChange', updateState);
						return function () {
							editor.off('NodeChange SelectionChange', updateState);
						};
					},

					onAction: function () {
						applyClass(item.value);
					},
				};
			});

			items.push({ type: 'separator' });
			items.push({
				type: 'menuitem',
				text: 'Remove styles',
				onAction: removeClass,
			});

			return items;
		}

		// toolbar button
		editor.ui.registry.addMenuButton('4r-styles', {
			text: 'Apply styles',
			tooltip: 'Apply block/inline style',
			fetch: function (callback) {
				callback(buildMenuItems());
			},
		});

		// plugin metadata
		return {
			getMetadata: () => ({
				name: '4 Roads Block Class Applicator',
				url: 'http://www.4-roads.com'
			})
		};
	});

})();
