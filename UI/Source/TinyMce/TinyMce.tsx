import {lazyLoad} from 'UI/Functions/WebRequest';
import tinyMceRef from './static/tinymce.min.js';
import {getUrl} from 'UI/FileRef';
import {PublicError} from "UI/Failed";
import { useEffect, useRef, useState } from 'react';
import themeRef from './static/themes/socialstack.css';
import {useEditor} from './EditorContext';
import getConfig from 'UI/Config';

export { useEditor };

// Root placeholder component - renders as root-content in TinyMCE

inputTypes.html = function (props) {
	const { field, validate, validationFailure, required, onInputRef } = props;
	return <TinyMce id={props.id}
		{...field}
		validate={validate}
		validationFailure={validationFailure}
		required={required}
		onInputRef={onInputRef}
	/>;
}

// Used by typescript props via HtmlString
inputTypes.htmlstring = function (props) {
	const { field, validate, validationFailure, required, onInputRef } = props;
	return <TinyMce id={props.id}
		{...field}
		validate={validate}
		validationFailure={validationFailure}
		required={required}
		onInputRef={onInputRef}
	/>;
}

/**
 * Props for the spacer component.
 */
interface TinyMceProps {
	/**
	 * unique field ID
	 */
	id?: string,

	/**
	 * true if menu bar should be hidden
	 */
	hideMenubar?: boolean,

	/**
	 * true if status bar should be hidden
	 */
	hideStatusbar?: boolean,

	/**
	 * menu item options
	 * ref: https://www.tiny.cloud/docs/tinymce/latest/menus-configuration-options/#menu
	 */
	menu?: object,

	/**
	 * menu bar options
	 * ref: https://www.tiny.cloud/docs/tinymce/latest/menus-configuration-options/#menubar
	 */
	menubar?: string | boolean,

	/**
	 * toolbar options
	 * ref: https://www.tiny.cloud/docs/tinymce/latest/toolbar-configuration-options/
	 */
	toolbar?: string,
	
	/**
	 * Right click menu options
	 */
	contextMenu?: string,

	/**
	 * true if code view allowed
	 */
	allowCode?: boolean,

	/**
	 * true if images allowed
	 */
	allowImages?: boolean,

	/**
	 * true if embedding of images (automatic uploads) allowed
	 */
	embedImages?: boolean,

	/**
	 * true if media (e.g. video) allowed
	 */
	allowMedia?: boolean,

	/**
	 * true if emojis allowed
	 */
	allowEmojis?: boolean,

	/**
	 * true if links allowed
	 */
	allowLinks?: boolean,

	/**
	 * true if tables allowed
	 */
	allowTables?: boolean,

	/**
	 * true if fullscreen allowed
	 */
	allowFullscreen?: boolean,

	/**
	 * true if menu / toolbars should remain in place while scrolling
	 */
	dockHeader?: boolean,

	/**
	 * true if this is a basic editor, allowing for only specific tags
	 */
	basicEditor?: boolean,

	/**
	 * preferred root wrapping element to use (defaults to <p>)
	 */
	rootBlock?: string,

	/**
	 * true if content should be stripped of wrapping tag
	 */
	unwrapHtml?: boolean,

	/**
	 * true if carriage returns should be automatically converted to line breaks (<br/>)
	 */
	disableCarriageReturn?: boolean,

	/**
	 * comma-separated list of supported elements (all others will be stripped)
	 */
	validElements?: string,

	/**
	 * min height in pixels
	 */
	minHeight?: number,

	/**
	 * max height in pixels
	 */
	maxHeight?: number,

	uploadEndpoint?: string,

	/**
	 * true if element path should be displayed within status bar
	 */
	showElementPath?: boolean,

	/**
	 * true if word count should be displayed within status bar
	 */
	showWordCount?: boolean,

	/**
	 * true if editor should be resizable (true, false or "both")
	 */
	resizable?: string | boolean,

	/**
	 * plugins
	 */
	plugins: string,

	/**
	 * mentions lookup URL
	 */
	mentionsLookupUrl: string,

/**
	 * mentions query
	 */
	mentionsQuery: string,

	/**
	 * true if editor should allow react-component elements
	 */
	allowsReact?: boolean,

	/**
	 * The template type when this editor is being used to edit a template.
	 * 1 = web, 2 = email, 3 = pdf
	 */
	templateType?: number,

	/**
	 * Validation rules for this field (e.g. ["Required"])
	 */
	validate?: string[],

	/**
	 * The current validation failure, if any
	 */
	validationFailure?: PublicError | null,

	/**
	 * Callback to provide the textarea element reference to the parent (e.g. Input.tsx)
	 */
	onInputRef?: (el: HTMLElement | null) => void,

	/**
	 * maximum permissable length in characters
	 */
	maxlength?: number,
}

const TinyMce: React.FC<TinyMceProps> = props => {
	const tinyMceConfig = getConfig('tinymce') || [];
	const filteredConfig = tinyMceConfig.filter(cfg =>
		// strip entry if all fields within are equal to null
		!Object.values(cfg).every(val => val === null)
	);
	const editorConfig = filteredConfig.length ? filteredConfig[0] : undefined;

	const TINYMCE_DEFAULTS_ADMIN = {
		toolbar: 'bold italic underline strikethrough superscript subscript | alignleft aligncenter alignright alignjustify | outdent indent | numlist bullist | grid_insert',
		
		allowCode: true,
		allowImages: true,
		embedImages: true,
		allowMedia: true,
		allowEmojis: true,
		allowLinks: true,
		allowTables: true,

		showElementPath: false,
		showWordCount: true,
		resizable: false,

		//plugins: 'lists anchor autolink link quickbars searchreplace table help',
		plugins: 'lists anchor searchreplace media grid',

		mentionsLookupUrl: '/v1/mentions',
		mentionsQuery: '_purl=/'
	};

	const TINYMCE_DEFAULTS_UI = {
		toolbar: 'bold italic underline strikethrough superscript subscript | alignleft aligncenter alignright alignjustify | outdent indent | numlist bullist',
		
		allowCode: true,
		allowImages: false,
		embedImages: false,
		allowMedia: false,
		allowEmojis: true,
		allowLinks: true,
		allowTables: false,

		showElementPath: false,
		showWordCount: false,
		resizable: false,

		plugins: 'lists searchreplace',

		mentionsLookupUrl: '/v1/mentions',
		mentionsQuery: '_purl=/'
	};

	// if document.body is undefined, we're on server-side rendering for the main UI front-end;
	// if document.body is defined, check for the "admin" class on the HTML tag to differentiate between admin / UI
	const TINYMCE_DEFAULTS = document?.body?.parentElement.classList.contains("admin") ? TINYMCE_DEFAULTS_ADMIN : TINYMCE_DEFAULTS_UI;

	const {
		toolbar = props.toolbar ?? TINYMCE_DEFAULTS['toolbar'],
		allowCode = props.allowCode ?? TINYMCE_DEFAULTS['allowCode'],
		allowImages = props.allowImages ?? TINYMCE_DEFAULTS['allowImages'],
		embedImages = props.embedImages ?? TINYMCE_DEFAULTS['embedImages'],
		allowMedia = props.allowMedia ?? TINYMCE_DEFAULTS['allowMedia'],
		allowEmojis = props.allowEmojis ?? TINYMCE_DEFAULTS['allowEmojis'],
		allowLinks = props.allowLinks ?? TINYMCE_DEFAULTS['allowLinks'],
		allowTables = props.allowTables ?? TINYMCE_DEFAULTS['allowTables'],
		minHeight = props.minHeight ?? 275,
		maxHeight = props.maxHeight ?? undefined,

		showElementPath = props.showElementPath ?? TINYMCE_DEFAULTS['showElementPath'],
		showWordCount = props.showWordCount ?? TINYMCE_DEFAULTS['showWordCount'],
		resizable = props.resizable ?? TINYMCE_DEFAULTS['resizable'],

		plugins = !!props.plugins?.length ? props.plugins : TINYMCE_DEFAULTS['plugins'],

		mentionsLookupUrl = !!props.mentionsLookupUrl?.length ? props.mentionsLookupUrl : TINYMCE_DEFAULTS['mentionsLookupUrl'],
		mentionsQuery = !!props.mentionsQuery?.length ? props.mentionsQuery : TINYMCE_DEFAULTS['mentionsQuery'],

		onChange,
		validationFailure,
		onInputRef,
		maxlength,
		...otherProps
	} = props;

	const textareaRef = useRef(null);
	const editorRef = useRef(null);
	const [editor, setEditor] = useState(null);
	const timeoutRef = useRef(null);
	const currentCharCount = useRef(0);
	const initEditorRef = useRef<any>(null);

	useEffect(() => {

		if (!textareaRef.current) {
			return;
		}

		const textarea = textareaRef.current;
		const doc = textarea.ownerDocument;
		const win = doc.defaultView || doc.parentWindow;
		let tinyMceUrl = getUrl(tinyMceRef);

		// Cordova fix
		if (tinyMceUrl[0] !== '/') {
			tinyMceUrl = './' + tinyMceUrl;
		}

		lazyLoad(tinyMceUrl, win).then(imported => {
			const tinymce = win.tinymce;
			tinymce.baseURL = tinyMceUrl.replace(/\/tinymce\.min\.js/gi, '');
			tinymce.suffix = '.min';
			return initEditorRef.current(textarea, tinymce);
		}).then(editors => {
			editorRef.current = editors[0];
			setEditor(editors[0]);
		});

		// Cleanup on unmount
		return () => {
			if (editorRef.current) {
				editorRef.current.destroy();
				editorRef.current = null;
			}
		};
	}, []);

	const omit = (obj, fields) => {
		const out = {}

		Object.keys(obj).forEach(key => {
			if (fields.includes(key)) {
				return;
			}
			out[key] = obj[key];
		})
		return out;
	};

	const isValidUrl = (string) => {
		try {
			const url = new URL(string);
			return true;
		} catch (err) {
			return false;
		}
	};

	const initEditor = (target, tinymce) => {
		const contentCss = [getUrl(themeRef)];
		const parentLinks = document?.querySelectorAll('link[rel="stylesheet"]');
		parentLinks.forEach((link) => {
			if (link.href.includes('main.css')) {
				contentCss.push(link.href);
			}
		});

		const isEmailTemplate = props.templateType === 2;

		// include references to @font-face rules for email fonts
		if (isEmailTemplate && !!editorConfig?.emailFontFamilies?.length) {
			contentCss.push(...editorConfig.emailFontFamilies.map(font => font.fontFaceUrl));
		}

		// build desktop / email font stacks
		const desktopFontFamilies: string[] = !!editorConfig?.desktopFontFamilies?.length ? editorConfig.desktopFontFamilies.map(font => {
			const fallbacks = font.fallbackFonts.endsWith(';') ? font.fallbackFonts : `${font.fallbackFonts};`;
			return `'${font.familyName}'='${fallbacks}`;
		}).join('') : "";

		const emailFontFamilies: string[] = !!editorConfig?.emailFontFamilies?.length ? editorConfig.emailFontFamilies.map(font => {
			const fallbacks = font.fallbackFonts.endsWith(';') ? font.fallbackFonts : `${font.fallbackFonts};`;
			return `'${font.familyName}'='${fallbacks}`;
		}).join('') : "";

		const fontFamiliesCount = isEmailTemplate ? emailFontFamilies.length : desktopFontFamilies.length;

		const config = {
			target,
			toolbar,
			promotion: false,
			branding: false,
			toolbar_sticky: true,
			toolbar_mode: 'wrap', // prevents toolbar collapsing to '...'
			ui_mode: 'split',
			autoresize_overflow_padding: 0,
			selection_toolbar_sticky: true,
			scroll_into_view_on_focus: false,
			statusbar: props.hideStatusBar === false ? false : showElementPath || showWordCount || resizable,
			elementpath: showElementPath,
			resize: resizable,

			plugins,

			grid_preset: 'Bootstrap5',

			mentionsLookupUrl,
			mentionsQuery,

			content_css: contentCss,
			content_style: isEmailTemplate && editorConfig?.emailStyles ? editorConfig.emailStyles : undefined,

			font_family_formats: isEmailTemplate ? emailFontFamilies : desktopFontFamilies,
			/* original list:
				'Andale Mono=andale mono,monospace;' +
				'Arial=arial,helvetica,sans-serif;' +
				'Arial Black=arial black,sans-serif;' +
				'Book Antiqua=book antiqua,palatino,serif;' +
				'Comic Sans MS=comic sans ms,sans-serif;' +
				'Courier New=courier new,courier,monospace;' +
				'Georgia=georgia,palatino,serif;' +
				'Helvetica=helvetica,arial,sans-serif;' +
				'Impact=impact,sans-serif;' +
				'Symbol=symbol;' +
				'Tahoma=tahoma,arial,helvetica,sans-serif;' +
				'Terminal=terminal,monaco,monospace;' +
				'Times New Roman=times new roman,times,serif;' +
				'Trebuchet MS=trebuchet ms,geneva,sans-serif;' +
				'Verdana=verdana,geneva,sans-serif;' +
				'Webdings=webdings;' +
				'Wingdings=wingdings,zapf dingbats',
			*/

			contextmenu: props.contextMenu,

			schema: props.allowsReact ? 'html5' : undefined,
			automatic_uploads: true,
			images_upload_handler: (blobInfo, progress) => new Promise((resolve, failure) => {
				const xhr = new XMLHttpRequest();
				var ep = props.uploadEndpoint || "upload/create";
				var apiUrl = (window as any).ingestUrl || (window as any).apiHost || '';
				if (!apiUrl.endsWith('/')) {
					apiUrl += '/';
				}
				apiUrl += 'v1/';

				ep = (ep.indexOf('http') === 0 || ep[0] == '/') ? ep : apiUrl + ep;

				xhr.open('PUT', ep, true);
				xhr.upload.onprogress = (e) => {
					progress(e.loaded / e.total * 100);
				};

				xhr.onload = () => {
					if (xhr.status < 200 || xhr.status >= 300) {
						failure('HTTP Error: ' + xhr.status);
						return;
					}

					const json = JSON.parse(xhr.responseText);

					if (!json) {
						failure('Invalid JSON: ' + xhr.responseText);
						return;
					}

					var location = getUrl(json.result?.ref);

					// This 'location' string is what gets inserted into the src/data attribute
					resolve(location);
				};

				xhr.onerror = () => {
					failure('Image upload failed due to a Network Error.');
				};

				xhr.setRequestHeader("Content-Name", blobInfo.filename());
				xhr.send(blobInfo.blob());
			}),

			extended_valid_elements: props.allowsReact ?
				'react-component[data-name|data-props|data-links|contenteditable|data-mounted|class],root-content[data-name|data-props|data-links|contenteditable|data-placeholder|class]' : undefined,
			custom_elements: props.allowsReact ? 'react-component,root-content' : undefined,

			// wihtout this, editable <summary> text is not saved
			valid_children: props.allowsReact ? '+summary[root-content]' : undefined,

			// This class tells the plugin to treat the component as a single unit
			noneditable_noneditable_class: 'react-component',
			// This allows your root-content to remain editable inside it
			noneditable_editable_class: 'root-content',

			// NB: by default, TinyMCE renders underlined text with <span style="text-decoration: underline;" />;
			// while strictly speaking this is correct due to the semantic meaning of <u> changing in HTML5
			// (ref: https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/u), we're also using
			// this editor to produce email templates, so to avoid further confusing some email clients, use <u />
			// (this also simplifies allowing underlined text within basic editors - see valid_elements)
			formats: props.allowsReact ? {
				reactComponent: {
					tag_names: ['react-component'],
					attributes: ['data-name', 'data-props', 'data-links', 'contenteditable', 'data-mounted', 'class']
				},
				rootContent: {
					tag_names: ['root-content'],
					attributes: ['data-name', 'data-props', 'data-links', 'contenteditable', 'data-placeholder', 'class']
				},
				underline: { inline: 'u', exact: true }
			} : {
				underline: { inline: 'u', exact: true }
			},

			setup: (editor) => {
				props.onSetup && props.onSetup(editor);
				
				const fireChange = () => {
					if (onChange) {
						clearTimeout(timeoutRef.current);
						timeoutRef.current = setTimeout(() => {
							onChange({ target: { value: editor.getContent() } });
						}, 100);
								}
							};

				editor.on('init', () => {
					// ensure clicks on the outer label focus the editor
					const label = document.querySelector(`label[for="${target.id}"]`);

					if (label) {
						label.addEventListener('click', (event) => {
							event.preventDefault();
							editor.focus();
						});
					}
				});

				editor.on('focus', () => {
					editor.getContainer().closest('[role="application"]').classList.add('is-editor-focused');
				});

				editor.on('blur', () => {
					editor.getContainer().closest('[role="application"]').classList.remove('is-editor-focused');
				});

				editor.on('change input undo redo', fireChange);

				// maxlength support - utilises char count from wordcount plugin
				if (maxlength) {

					editor.on('wordCountUpdate', (e) => {
						currentCharCount.current = e.wordCount.characters;
					});

					/*
					editor.on('paste', (e) => {
						var clipboardData = e.clipboardData || (window as any).clipboardData;

						if (clipboardData) {
							var pastedText = clipboardData.getData('text/plain');

							if (pastedText && currentCharCount.current + pastedText.length > maxlength) {
								e.preventDefault();
								return;
							}

						}

						fireChange();
					});
					*/

					editor.on('keydown', (e) => {

						if (currentCharCount.current >= maxlength) {
							var keyCode = e.keyCode;

							// Allow navigation, deletion, and control keys
							if (
								(keyCode >= 33 && keyCode <= 40) || // Page Up/Down, Home, End, arrows
								keyCode === 8 || // Backspace
								keyCode === 46 || // Delete
								keyCode === 13 || // Enter
								(e.ctrlKey || e.metaKey) || // Ctrl/Cmd shortcuts
								keyCode === 27 // Escape
							) {
								return;
							}

							e.preventDefault();
						}
					});

				} else {
					editor.on('paste', fireChange);
				}

			},

			//quickbars_insert_toolbar: false,
			// NB: may be necessary to omit quicklink as this bypasses class injection
			//quickbars_selection_toolbar: 'bold italic | quicklink h2 h3 blockquote',
			//quickbars_selection_toolbar: 'bold italic | quicklink blockquote',

			//quickbars_image_toolbar: 'alignleft aligncenter alignright',
		};

		if (editorConfig) {
			config.fr_styles_classes = editorConfig.styles;
			config.fr_hr_classes = editorConfig.horizontalRules;
		}

		/* default menu:
			menu: {
				file: { title: 'File', items: 'newdocument restoredraft | preview | importword exportpdf exportword | print | deleteallconversations' },
				edit: { title: 'Edit', items: 'undo redo | cut copy paste pastetext | selectall | searchreplace' },
				insert: { title: 'Insert', items: 'image link media addcomment pageembed codesample inserttable | math | charmap emoticons horizontalrule | pagebreak nonbreaking anchor tableofcontents | insertdatetime' },
				view: { title: 'View', items: 'code suggestededits revisionhistory | visualaid visualchars visualblocks | spellchecker | preview fullscreen | showcomments' },
				format: { title: 'Format', items: 'bold italic underline strikethrough superscript subscript codeformat | styles blocks fontfamily fontsize align lineheight | forecolor backcolor | language | removeformat' },
				table: { title: 'Table', items: 'inserttable | cell row column | advtablesort | tableprops deletetable' },
				tools: { title: 'Tools', items: 'spellchecker spellcheckerlanguage | a11ycheck code wordcount' },
				help: { title: 'Help', items: 'help' }
			},
		 */
		config.menu = {
			//file: {}
			edit: {
				title: `Edit`,
				items: 'undo redo | cut copy paste pastetext | selectall | searchreplace'
			},
			insert: {
				title: `Insert`,
				items: `${allowImages ? 'image' : ''} ${allowLinks ? 'link' : ''} ${allowMedia ? 'media' : ''} ${allowTables ? 'inserttable' : ''} | charmap ${allowEmojis ? 'emoticons' : ''} horizontalrule | anchor tableofcontents | insertdatetime`
			},
			view: {
				title: `View`,
				items: `${allowCode ? '4r-code | ' : ''}preview${props.allowFullscreen ? ' | 4r-fullscreen' : ''}`
			},
			format: {
				title: `Format`,
				//items: `bold italic underline strikethrough superscript subscript codeformat | styles blocks fontfamily fontsize align lineheight | forecolor backcolor | language | removeformat`
				items: `bold italic underline strikethrough superscript subscript | styles blocks ${fontFamiliesCount > 0 ? 'fontfamily' : ''} fontsize align lineheight | forecolor backcolor | language | removeformat`
			}
			// NB: table / tools menus handled below
		};
		config.menubar = 'edit insert view format';

		if (allowTables) {
			config.menu.table = {
				title: `Table`,
				items: 'inserttable | cell row column | advtablesort | tableprops deletetable'
			};
			config.menubar += " table";
			config.plugins += " table";
		}

		if (allowCode || showWordCount) {
			config.menu.tools = {
				title: `Tools`,
				items: `${allowCode ? '4r-code' : ''} ${showWordCount ? 'wordcount' : ''}`
			};
			config.menubar += " tools";
		}

		if (props.hideMenubar === true) {
			config.menubar = false;
		}

		// insert styled horizontal rules
		if (editorConfig?.horizontalRules?.length) {
			config.plugins += " 4r-hr";
			config.toolbar += " | 4r-hr ";
		} 

		// ref: https://www.tiny.cloud/docs/tinymce/latest/link/
		if (allowLinks) {
			config.plugins += " link ";
			config.toolbar += " | link ";
			config.link_class_list = [
				{ title: 'None', value: 'ui-link' },
				{ title: 'Button (Primary)', value: 'btn ui-btn btn-primary' },
				{ title: 'Button (Secondary)', value: 'btn ui-btn btn-secondary' }
			];
			//config.link_default_target = '_blank';
			//config.link_assume_external_targets = false; // true/false/'http'/'https'
			//config.link_default_protocol = 'http';
			/*
			config.link_list = [
				{ title: '{companyname} Home Page', value: '{companyurl}' },
				{ title: '{companyname} Blog', value: '{companyurl}/blog' },
				{ title: '{productname} Documentation', value: '{companyurl}/docs/' },
				{ title: '{productname} on Stack Overflow', value: '{communitysupporturl}' },
				{ title: '{productname} GitHub', value: 'https://github.com/tinymce/' }
			];
			*/
			//config.link_quicklink = true;
			/*
			config.link_rel_list = [
				{ title: 'No Referrer', value: 'noreferrer' },
				{ title: 'External Link', value: 'external' }
			];
			*/
			config.link_context_toolbar = true;
			config.link_title = false;
			//config.link_target_list = false;

			// ref: https://www.tiny.cloud/docs/tinymce/latest/url-handling
			// set true to allow javascript: URLs in links and images (not recommended)
			config.allow_script_urls = false;

			// This option enables you to control whether TinyMCE is to be smart and restore URLs to their original values.
			// URLs are automatically converted (messed up) by default because the browser’s built-in logic works this way.
			// There is no way to get the real URL unless you store it away.
			// If you set this option to false it tries to keep these URLs intact.
			// This option is set to true by default, which means URLs are forced to be either absolute or relative depending on the state of relative_urls.
			config.convert_urls = true;

			// This option specifies the base URL for all relative URLs in the document.
			// The default value is the directory of the current document.
			// If a value is provided, it must specify a directory (not a document) and must end with a /.
			var baseUrl = window?.location.origin;
			config.document_base_url = baseUrl?.endsWith("/") ? baseUrl : `${baseUrl}/`;

			// For URLs with the same domain as the page containing the TinyMCE editor. If set to:
			// 	true — all URLs created in TinyMCE will be converted to a link relative to the document_base_url.
			//  false - all URLs will be converted to absolute URLs.
			config.relative_urls = true;

			// This option is used if the relative_urls option is set to false and only applies to links with the same domain as the document_base_url.
			// If this option is set to true, the protocol and host of the document_base_url is excluded for relative links.
			// If this option is set to false, the protocol and host of the document_base_url is added for relative links.
			config.remove_script_host = true;

			config.urlconverter_callback = (url, node, on_save, name) => {
				// Guard against non-string values (null, undefined, etc.)
				if (typeof url !== 'string' || !url) {
					return url;
				}

				// TinyMCE strips the leading slash for relative URLs, which breaks them
				// (e.g. viewing "product/1" from "site.com/other-page" attempts to view "site.com/other-page/product/1")
				if (!isValidUrl(url) && !url.startsWith("/") && url.indexOf(':') == -1) {
					return `/${url}`;
				}

				return url;
			};

			// Set whether TinyMCE should prepend a http:// prefix if the supplied URL does not contain a protocol prefix.
			//  false: Users are prompted to prepend http:// when the URL entered starts with www and does not have a protocol. Other URLs are added without prompt.
			//  true: URLs are assumed to be external.Users are prompted to prepend a http:// prefix when the protocol is not specified.
			//  'http': URLs are assumed to be external.URLs without a protocol prefix are prepended a http:// prefix.
			//  'https': URLs are assumed to be external.URLs without a protocol prefix are prepended a https:// prefix.
			config.link_assume_external_targets = false;

			// This option allows you to set a default protocol for links when inserting/editing a link via the link dialog.
			// The protocol will apply to any links where the protocol has not been specified and the prefix prompt has been accepted.
			config.link_default_protocol = "https";
		}

		if (allowImages) {
			config.plugins += " image ";
			config.toolbar += " | image ";
			config.image_advtab = false;
			//config.images_file_types = 'jpeg,jpg,jpe,jfi,jif,jfif,png,gif,webp';
			//config.image_dimensions = false;
			//config.image_title = true;

			if (embedImages) {
				// enable automatic uploads of images represented by blob or data URIs
				config.automatic_uploads = true;

				/*
				URL of our upload handler (for more details check: https://www.tiny.cloud/docs/configure/file-image-upload/#images_upload_url)
				images_upload_url: 'postAcceptor.php',
				here we add custom filepicker only to Image dialog
				*/
				config.file_picker_types = 'image';

				// and here's our custom image picker
				config.file_picker_callback = (cb, value, meta) => {
					const input = document.createElement('input');
					input.setAttribute('type', 'file');
					input.setAttribute('accept', 'image/*');

					/*
						Note: In modern browsers input[type="file"] is functional without
						even adding it to the DOM, but that might not be the case in some older
						or quirky browsers like IE, so you might want to add it to the DOM
						just in case, and visually hide it. And do not forget do remove it
						once you do not need it anymore.
					*/

					input.onchange = function () {
						const file = this.files[0];
						const reader = new FileReader();

						reader.onload = function () {
							/*
							  Note: Now we need to register the blob in TinyMCEs image blob
							  registry. In the next release this part hopefully won't be
							  necessary, as we are looking to handle it internally.
							*/
							const id = 'blobid' + (new Date()).getTime();
							const blobCache = tinymce.activeEditor.editorUpload.blobCache;
							const base64 = reader.result.split(',')[1];
							const blobInfo = blobCache.create(id, file, base64);
							blobCache.add(blobInfo);

							// call the callback and populate the Title field with the file name
							cb(blobInfo.blobUri(), { title: file.name });
						};

						reader.readAsDataURL(file);
					};

					input.click();
				};
			}
		}

		// ref: https://www.tiny.cloud/docs/tinymce/latest/media/
		if (allowMedia) {
			config.plugins += " media ";
			config.toolbar += " | media ";

			/*
			// specify HTML template for audio
			config.audio_template_callback = (data) =>
				'<audio controls>\n' +
				`<source src="${data.source}"${data.sourcemime ? ` type="${data.sourcemime}"` : ''} />\n` +
				(data.altsource ? `<source src="${data.altsource}"${data.altsourcemime ? ` type="${data.altsourcemime}"` : ''} />\n` : '') +
				'</audio>';

			// specify HTML template for video
			config.video_template_callback = (data) =>
				`<video width="${data.width}" height="${data.height}"${data.poster ? ` poster="${data.poster}"` : ''} controls="controls">\n` +
				`<source src="${data.source}"${data.sourcemime ? ` type="${data.sourcemime}"` : ''} />\n` +
				(data.altsource ? `<source src="${data.altsource}"${data.altsourcemime ? ` type="${data.altsourcemime}"` : ''} />\n` : '') +
				'</video>';

			// specify HTML for iframe
			config.iframe_template_callback = (data) =>
				`<iframe title="${data.title}" width="${data.width}" height="${data.height}" src="${data.source}"></iframe>`;

			// custom media resolver
			config.media_url_resolver = (data) => {
				return new Promise((resolve) => {

					// NB: demo only - tinymce handles this OOTB
					if (data.url.indexOf('youtube.com/watch') !== -1) {
						const url = new URL(data.url);
						const videoId = url.searchParams.get("v");

						const width = 560;
						const height = 315;
						const title = `YouTube video player`;

						const embedHtml = `<iframe width="${width}" height="${height}" src="https://www.youtube.com/embed/${videoId}" title="${title}" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>`;

						resolve({ html: embedHtml });
					} else {
						resolve({ html: '' });
					}
				});
			};
			*/

			config.media_alt_source = false;
			//config.media_dimensions = false;
			//config.media_live_embeds = false;
			//config.media_poster = false;
		}

		// ref: https://www.tiny.cloud/docs/tinymce/latest/emoticons/
		if (allowEmojis) {
			config.plugins += " emoticons ";
			config.toolbar += " | emoticons ";
		}

		if (allowCode) {
			config.plugins += " 4r-code ";
			config.toolbar += " | 4r-code ";
		}

		// include wordcount plugin if we need to track text length
		if (showWordCount || maxlength) {
			config.plugins += " wordcount ";
		}
		
		// enforce autoresizing (prevents potential double scrollbar / repaint issues in Chrome when rendered within tabs)
		config.plugins += " autoresize";
		config.min_height = minHeight;
		config.max_height = maxHeight;
		// config.autoresize_bottom_margin = 20;

		/*
		// sticky header
		config.toolbar_sticky = true;

		// Use this if the scrollable area is NOT the window, 
		// but a specific parent element:
		
		*/

		// config.toolbar_sticky_offset = 150;
		// config.fixed_toolbar_container = '.admin-page__content';
		config.ui_container = '.admin-page__content';

		//console.log("tinymce config: ", config);

		if (props.allowFullscreen) {
			config.toolbar += " | 4r-fullscreen";
			config.plugins += " 4r-fullscreen";
		}

		if (editorConfig) {
			config.fr_styles_classes = editorConfig.styles;
			config.fr_hr_classes = editorConfig.horizontalRules;
		}

		if (editorConfig?.styles?.length) {
			config.toolbar += " | 4r-styles";
			config.plugins += " 4r-styles";
		} 

		if (!!props.rootBlock?.length) {
			config.forced_root_block = props.rootBlock;
		}

		if (props.disableCarriageReturn) {
			config.newline_behavior = 'linebreak';
		}

		if (!!props.validElements?.length) {
			config.valid_elements = props.validElements;
		}

		if (props.basicEditor) {
			config.menubar = false;
			config.toolbar_sticky = false;
			config.valid_elements = !!props.validElements?.length ? props.validElements : 'b,strong,em,i,u,br,s,strike,ins,del,mark,abbr,small,sub,sup,a';
			config.min_height = props.minHeight ?? 40;
			config.max_height = props.maxHeight ?? 200;

			// basic editors always allow bold/italic/underline,
			// but extended options such as links, images, media, emojis and code need to be explicitly allowed
			if (!props.toolbar) {
				var basicToolbar = "bold italic underline strikethrough | ";

				if (props.allowLinks) {
					basicToolbar += " link ";
				}

				if (props.allowImages) {
					basicToolbar += " image ";
				}

				if (props.allowMedia) {
					basicToolbar += " media ";
				}

				if (props.allowEmojis) {
					basicToolbar += " emoticons ";
				}

				if (props.allowCode) {
					basicToolbar += " 4r-code ";
				}

				config.toolbar = basicToolbar;
			}

			if (!isEmailTemplate) {
				config.content_style = "body { padding: 5px 20px 0 20px !important; }";
			}

			// match field sizing to basic input fields
			config.init_instance_callback = function (editor) {
				const container = editor.getContainer();
				container.style.border = '1px solid #ced4da';
				container.style.borderRadius = '5px';
				container.style.maxWidth = '50vw';
				container.style.minWidth = '25rem';
				container.style.width = '100%'; // Ensures it scales fluently between min and max
			};

			// Pasted content often carries its own paragraph breaks. Convert any
			// literal newlines to <br> before TinyMCE processes them, otherwise
			// valid_elements will just strip the wrapping <p> and the line break
			// is lost entirely (text gets mashed together).
			config.paste_preprocess = function (plugin, args) {
				args.content = args.content.replace(/\r?\n/g, '<br>');
			};

		}

		return tinymce.init(config);
	};

	initEditorRef.current = initEditor;

	useEffect(() => {
		if (textareaRef.current) {
			if (onInputRef) {
				onInputRef(textareaRef.current);
			}

			textareaRef.current.onGetValue = (val, ele) => {
				if (ele === textareaRef.current && editor) {
					var htmlContent = editor.getContent();

					// remove wrapping HTML tag?
					if (props.unwrapHtml) {
						const doc = new DOMParser().parseFromString(htmlContent, 'text/html');
						const root = doc.body.firstElementChild;

						if (!root) {
							return htmlContent;
					}

						const tag = props.rootBlock || 'p';
						htmlContent = (root?.tagName.toLowerCase() === tag.toLowerCase()) ? root.innerHTML : htmlContent;
					}

					if (maxlength) {
						var charCount = editor.plugins.wordcount
							? editor.plugins.wordcount.body.getCharacterCount()
							: currentCharCount.current;

						if (charCount > maxlength) {
							throw new PublicError("tiny-mce/validation", `Content must not exceed ${maxlength} characters (currently ${charCount}).`);
						}
					}

					return htmlContent;
				}
			};

		}
	}, [editor, maxlength, onInputRef, props.rootBlock, props.unwrapHtml]);
	
	const textAreaClasses = ['form-control', 'ui-form-control', 'textarea-tinymce'];

	if (validationFailure) {
		textAreaClasses.push('is-invalid');
	}

	return <>
		<textarea className={textAreaClasses.join(' ')}
			ref={textareaRef} id={props.id}

			// reason for this:
			// the required attribute requires the textarea to be focusable, when "display: none" is in effect
			// it isn't focusable, "display:none" happens due to tinymce, so instead I've omitted
			// the required attribute, and handled it the same way the common Input component handles it
			// @see https://stackoverflow.com/questions/22148080/an-invalid-form-control-with-name-is-not-focusable	
			{...omit(otherProps, ['required'])}
		/>
	</>;
}

export default TinyMce;
