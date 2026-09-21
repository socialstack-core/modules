/**
 * 4 Roads Horizontal Rule Plugin
 *
 * Allows various styles of <hr/> to be inserted into the editor
 */

(function () {

	tinymce.PluginManager.add('4r-hr', (editor, url) => {

		editor.options.register('fr_hr_classes', {
			processor: 'object[]',
			default: []
		});

		// dropdown items
		function getClassItems() {
			const defaultItems = [
				{ type: 'choiceitem', title: 'Default', value: '' },
			];

			const separator = [{ type: 'separator' }];

			const additionalOptions = editor.options.get('fr_hr_classes') || [
				{ type: 'choiceitem', title: 'Primary', value: 'fr-hr--primary' },
				{ type: 'choiceitem', title: 'Secondary', value: 'fr-hr--secondary' },
				{ type: 'choiceitem', title: 'Information', value: 'fr-hr--info' },
				{ type: 'choiceitem', title: 'Danger', value: 'fr-hr--danger' },
				{ type: 'choiceitem', title: 'Warning', value: 'fr-hr--warning' },
				{ type: 'choiceitem', title: 'Success', value: 'fr-hr--success' }
			];

			return additionalOptions?.length ?
				defaultItems.concat(defaultItems, separator, additionalOptions) : defaultItems;
		}

		// Helper function to insert the styled HR
		const insertHr = (val) => {
			editor.insertContent(`<hr class="fr-hr ${val}" />`);
		};

		// Register the Custom Split Button
		editor.ui.registry.addSplitButton('4r-hr', {
			icon: 'hr',
			tooltip: 'Insert Horizontal Line',
			// What happens when you click the main button icon directly:
			onAction: () => insertHr('default'),
			// What happens when you click the small dropdown arrow:
			onItemAction: (api, value) => insertHr(value),
			fetch: (callback) => {
				callback(getClassItems());
			}
		});

		// plugin metadata
		return {
			getMetadata: () => ({
				name: '4 Roads Horizontal Rule Plugin',
				url: 'http://www.4-roads.com'
			})
		};
	});

})();
