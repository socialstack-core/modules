/**
 * 4 Roads fullscreen plugin
 */

(function () {

	tinymce.PluginManager.add('4r-fullscreen', (editor, url) => {

		// NB: plugin currently has to absolutely position editor and rely on a max z-index,
		//     rather than moving the editor to a true top-layer fullscreen as this blocks menus / modals
		/*
		const isFullscreen = () => !!document.fullscreenElement;

		const toggleFullscreen = () => {
			const container = editor.getContainer();

			if (!isFullscreen()) {
				container.requestFullscreen?.() ||
					container.webkitRequestFullscreen?.() ||
					container.msRequestFullscreen?.();
			} else {
				document.exitFullscreen?.() ||
					document.webkitExitFullscreen?.() ||
					document.msExitFullscreen?.();
			}
		}
		*/

		const getContainer = () => {
			// find wrapping .canvas-editor
			const container = editor.getContainer();
			return container.parentElement;
		};

		const isFullscreen = () => {
			const container = getContainer();
			return !!container.classList.contains("fullscreen");
		};

		const closePopoverById = (id) => {
			const popover = document.getElementById(id);

			if (popover && popover.matches(':popover-open')) {
				popover.hidePopover();
			}
		};

		const toggleFullscreen = () => {
			const container = getContainer();

			if (!isFullscreen()) {
				closePopoverById("admin_menu");
				lockDeepFocus(container);
			} else {
				unlockDeepFocus();
			}

			container.classList.toggle("fullscreen");
		}

		const syncFullscreenState = (api) => {
			api.setActive(isFullscreen());

			return () => {
				// required by onSetup
			};
		};

		/**
		 * Traverses up the DOM tree from the target and makes all 
		 * sibling branches inert.
		 */
		function lockDeepFocus(target) {

			if (!target) {
				return;
			}

			let current = target;

			// MCE menus are parented by body > .tox.tox-silver-sink.tox-tinymce-aux
			// (skip this or menus will be disabled)
			const mceMenuWrapper = document.querySelector(".tox.tox-tinymce-aux");

			// Walk up from the target to the <body>
			while (current && current !== document.body) {
				const parent = current.parentElement;

				Array.from(parent.children).forEach(sibling => {

					if (sibling !== current && sibling !== mceMenuWrapper && sibling.nodeName !== "SCRIPT") {

						// Save state and make inert
						if (sibling.hasAttribute('inert')) {
							sibling.setAttribute('data-keep-inert', 'true');
						} else {
							sibling.setAttribute('inert', '');
						}

						// hide if we're a popover, otherwise this will remain above the editor
						if (sibling.matches(":popover-open")) {
							sibling.classList.add("fullscreen-hidden-popover");
						}
					}
				});

				current = parent;
			}
		}

		/**
		 * Cleanup: Removes all inert attributes we applied, 
		 * respecting original states.
		 */
		function unlockDeepFocus() {
			document.querySelectorAll('[inert]').forEach(el => {
				if (el.hasAttribute('data-keep-inert')) {
					el.removeAttribute('data-keep-inert');
				} else {
					el.removeAttribute('inert');
				}
			});

			document.querySelectorAll('.fullscreen-hidden-popover').forEach(el => {
				el.classList.remove('fullscreen-hidden-popover');
			});
		}

		// toolbar button
		editor.ui.registry.addToggleButton('4r-fullscreen', {
			text: `Fullscreen`,
			icon: 'fullscreen',
			tooltip: `Toggle fullscreen view`,
			onAction: toggleFullscreen,
			onSetup: syncFullscreenState
		});

		// menu item
		editor.ui.registry.addToggleMenuItem('4r-fullscreen', {
			text: 'Fullscreen',
			icon: 'fullscreen',
			//shortcut: 'Meta+Shift+F',
			onAction: toggleFullscreen,
			onSetup: syncFullscreenState
		});

		return {
			getMetadata: () => ({
				name: '4 Roads Fullscreen plugin',
				url: 'http://www.4-roads.com'
			})
		};
	});

})();
