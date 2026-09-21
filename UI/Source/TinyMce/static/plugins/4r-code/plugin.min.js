/**
 * 4 Roads ACE Source Code plugin for TinyMCE.
 *
 * Replaces the default textarea-based source code view with an ACE editor
 * providing syntax highlighting, code folding, line numbers, and search.
 */

(function () {

	var global = tinymce.util.Tools.resolve('tinymce.PluginManager');

	var aceEditorInstance = null;

	function loadScript(src) {
		return new Promise(function (resolve) {
			var script = document.createElement('script');
			script.src = src;
			script.onload = resolve;
			document.head.appendChild(script);
		});
	}

	function loadAce(callback) {
		if (window.ace) {
			callback(window.ace);
			return;
		}

		if (!window._tinymceAceLoadPromise) {
			var aceBase = tinymce.baseURL + '/ace/';
			window._tinymceAceLoadPromise = loadScript(aceBase + 'ace.js')
				.then(function () { return loadScript(aceBase + 'ext-fr-beautify.js'); })
				.then(function () { return window.ace; });
		}

		window._tinymceAceLoadPromise.then(callback);
	}

	function destroyAceEditor() {
		if (aceEditorInstance) {
			aceEditorInstance.destroy();
			aceEditorInstance = null;
		}
	}

	function detectDarkMode() {
		var html = document.querySelector('html');
		return (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) ||
			(html && html.getAttribute('data-theme-variant') === 'dark');
	}

	function initAceEditor(container, content, onReady) {
		var ace = window.ace;

		var editor = ace.edit(container, {
			behavioursEnabled: true,
			wrapBehavioursEnabled: true,
			wrap: true,
			cursorStyle: 'smooth',
			fontSize: 14,
			showGutter: true,
			showLineNumbers: true,
			showFoldWidgets: true,
			fadeFoldWidgets: false,
			displayIndentGuides: true,
			highlightActiveLine: true,
			highlightSelectedWord: true,
			showPrintMargin: false,
			tabSize: 2,
			useSoftTabs: true
		});

		editor.session.setMode('ace/mode/html');

		if (detectDarkMode()) {
			editor.setTheme('ace/theme/monokai');
		}

		editor.session.setValue(content || '');

		editor.focus();
		editor.gotoLine(1);

		aceEditorInstance = editor;

		if (onReady) {
			onReady(editor);
		}
	}

	const setContent = (editor, html) => {
		editor.focus();
		editor.undoManager.transact(() => {
			editor.setContent(html);
		});
		editor.selection.setCursorLocation();
		editor.nodeChanged();
	};

	const getContent = (editor) => {
		return editor.getContent({ source_view: true });
	};

	const open = (editor) => {
		const editorContent = getContent(editor);
		const containerId = '4r-code-ace-editor';

		editor.windowManager.open({
			title: 'Source Code',
			size: 'large',
			body: {
				type: 'panel',
				items: [{
					type: 'htmlpanel',
					html: '<div id="' + containerId + '" class="ace-source-editor"></div>'
				}]
			},
			buttons: [
				{
					type: 'custom',
					name: 'format',
					text: 'Format HTML',
					align: 'start'
				},
				{
					type: 'cancel',
					name: 'cancel',
					text: 'Cancel'
				},
				{
					type: 'submit',
					name: 'save',
					text: 'Save',
					primary: true
				}
			],
			onAction: (api, details) => {
				if (details.name === 'format') {

					if (aceEditorInstance) {
						var beautify = window.ace.require('ace/ext/fr-beautify');
						beautify.beautify(aceEditorInstance.session);
						aceEditorInstance.focus();
					}

				}
			},
			onSubmit: (api) => {
				if (aceEditorInstance) {
					var annotations = aceEditorInstance.session.getAnnotations();
					var errors = annotations.filter(function (a) { return a.type === 'error'; });

					if (errors.length > 0) {
						var msg = errors.length === 1
							? '1 error detected in the source code. Save anyway?'
							: errors.length + ' errors detected in the source code. Save anyway?';

						editor.windowManager.confirm(msg, function (ok) {
							if (ok) {
								setContent(editor, aceEditorInstance.getValue());
								destroyAceEditor();
								api.close();
							}
						});

						return;
					}

					setContent(editor, aceEditorInstance.getValue());
				}
				destroyAceEditor();
				api.close();
			},
			onClose: () => {
				destroyAceEditor();
			}
		});

		loadAce((ace) => {
			setTimeout(() => {
				var container = document.getElementById(containerId);

				if (container) {
					initAceEditor(container, editorContent);
				}
			}, 100);
		});
	};

	const register$1 = (editor) => {
		editor.addCommand('4rCodeEditor', () => {
			open(editor);
		});
	};

	const register = (editor) => {
		const onAction = () => editor.execCommand('4rCodeEditor');
		editor.ui.registry.addButton('4r-code', {
			icon: 'sourcecode',
			tooltip: 'Source code',
			onAction
		});
		editor.ui.registry.addMenuItem('4r-code', {
			icon: 'sourcecode',
			text: 'Source code',
			onAction
		});
	};

	global.add('4r-code', (editor) => {
		register$1(editor);
		register(editor);
		return {};
	});

})();
