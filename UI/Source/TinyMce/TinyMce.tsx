import { lazyLoad } from 'UI/Functions/WebRequest';
// @ts-ignore
import tinyMceRef from './static/tinymce.min.js'; // Imports as a URL-like string
import {getUrl} from 'UI/FileRef';
import { PublicError } from "UI/Failed";
import { DefaultInputType } from "UI/Input/Default";
import { useEffect, useRef, useState } from 'react';
// @ts-ignore
import themeRef from './static/themes/socialstack.css'; // Imports as a URL-like string
import { EditorConfig, TinyMCE, Editor } from './editor.js';

type TinyMceInputType = DefaultInputType & TinyMceProps;

declare global {
	interface InputPropsRegistry {
		'html': TinyMceInputType,
		'htmlstring': TinyMceInputType
	}
}

inputTypes.html = function (props) {
	const { field } = props;
	return <TinyMce
		{...field}
	/>;
}

// Used by typescript props via HtmlString
inputTypes.htmlstring = function (props) {
	const { field } = props;
	return <TinyMce
		{...field}
	/>;
}

/**
 * Props for the spacer component.
 */
interface TinyMceProps {
	/**
	 * true if menu bar should be hidden
	 */
	showMenubar?: boolean,

	/**
	 * Initial value to populate the editor with (HTML)
	 */
	defaultValue?: string,

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
	 * min height in pixels
	 */
	minHeight?: number,

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
	resizable?: "both" | boolean,

	/**
	 * plugins
	 */
	plugins?: string,

	/**
	 * mentions lookup URL
	 */
	mentionsLookupUrl?: string,

/**
	 * mentions query
	 */
	mentionsQuery?: string,

	/**
	 * true if editor should allow react-component elements
	 */
	allowsReact?: boolean,

	onSetup?: (editor: Editor) => void,
	onChange?: (evt: any) => void,

	required?: boolean | string
}

const TinyMce: React.FC<TinyMceProps> = props => {
	const TINYMCE_DEFAULTS_ADMIN = {
		toolbar: 'bold italic underline | alignleft aligncenter alignright alignjustify | outdent indent | numlist bullist | grid_insert',
		
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
		toolbar: 'bold italic underline | alignleft aligncenter alignright alignjustify | outdent indent | numlist bullist',
		
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
	const TINYMCE_DEFAULTS = document?.body?.parentElement?.classList.contains("admin") ? TINYMCE_DEFAULTS_ADMIN : TINYMCE_DEFAULTS_UI;

	const {
		toolbar = TINYMCE_DEFAULTS['toolbar'],
		allowCode = TINYMCE_DEFAULTS['allowCode'],
		allowImages = TINYMCE_DEFAULTS['allowImages'],
		embedImages = TINYMCE_DEFAULTS['embedImages'],
		allowMedia = TINYMCE_DEFAULTS['allowMedia'],
		allowEmojis = TINYMCE_DEFAULTS['allowEmojis'],
		allowLinks = TINYMCE_DEFAULTS['allowLinks'],
		allowTables = TINYMCE_DEFAULTS['allowTables'],
		minHeight = props.minHeight || 275,

		showElementPath = TINYMCE_DEFAULTS['showElementPath'],
		showWordCount = TINYMCE_DEFAULTS['showWordCount'],
		resizable = TINYMCE_DEFAULTS['resizable'],

		plugins = TINYMCE_DEFAULTS['plugins'],

		mentionsLookupUrl = TINYMCE_DEFAULTS['mentionsLookupUrl'],
		mentionsQuery = TINYMCE_DEFAULTS['mentionsQuery'],

		onChange,
		required,
		...otherProps
	} = props;

	const textareaRef = useRef<HTMLTextAreaElement | null>(null);
	const editorRef = useRef<any | null>(null);
	const [editor, setEditor] = useState<any | null>(null);
	const timeoutRef = useRef<number | undefined>(undefined);

	useEffect(() => {

		if (!textareaRef.current) {
			return;
		}

		const textarea = textareaRef.current;
		const doc = textarea.ownerDocument;
		const win = doc.defaultView || ((doc as any).parentWindow as Window);
		let tinyMceUrl = getUrl(tinyMceRef as string) || '';

		// Cordova fix
		if (tinyMceUrl[0] !== '/') {
			tinyMceUrl = './' + tinyMceUrl;
		}

		lazyLoad(tinyMceUrl).then((imported:any) => {
			const tinymce: TinyMCE = (win as any).tinymce as TinyMCE;
			tinymce.baseURL = tinyMceUrl.replace(/\/tinymce\.min\.js/gi, '');
			tinymce.suffix = '.min';
			return initEditor(textarea, tinymce);
		}).then((editors:any) => {
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

	const isValidUrl = (str: string) => {
		try {
			const url = new URL(str);
			return true;
		} catch (err) {
			return false;
		}
	};

	const initEditor = (target: HTMLTextAreaElement, tinymce: TinyMCE) => {
		const contentCss: string[] = [];
		const themeUrl = getUrl(themeRef as string);
		themeUrl && contentCss.push(themeUrl);

		const parentLinks = document?.querySelectorAll('link[rel="stylesheet"]');
		parentLinks.forEach((link) => {
			const anchor = (link as HTMLAnchorElement);
			if (anchor.href.includes('main.css')) {
				contentCss.push(anchor.href);
			}
		});

	const getScrollParent = (node: HTMLElement | null) => {
		if (!node || node === document.body) {
			return window;
		}

		const isScrollable = (el: HTMLElement) => {
			const style = window.getComputedStyle(el);
			const overflowY = style.getPropertyValue('overflow-y') || style.getPropertyValue('overflow');
			const hasScrollStyle = /(auto|scroll)/.test(overflowY);
			const isTallEnough = el.scrollHeight > el.clientHeight;

			return hasScrollStyle && isTallEnough;
		};

		if (isScrollable(node)) {
			return node;
		} else {
			return getScrollParent(node.parentElement);
		}
	};

	const dockEditorHeader = (editor: any) => {
		const container = editor.getContainer() as HTMLElement;
		const header = container?.querySelector('.tox-editor-header') as HTMLElement;

		if (!container || !header) {
			return;
		}

		const headerRect = header.getBoundingClientRect();
		container.style.position = 'relative';
		container.style.paddingTop = `${headerRect.height}px`;

		const updateHeaderStyles = () => {
			const rect = container.getBoundingClientRect();
			const scrollParent = getScrollParent(container);
			const scrollParentDistance = scrollParent == window ? window.scrollY : (scrollParent as HTMLElement).scrollTop;

			if (rect.width > 0) {
				header.style.position = 'fixed';
				header.style.top = `${rect.top + scrollParentDistance}px`;
				header.style.width = `${rect.width}px`;
			}

		};

		updateHeaderStyles();

		// catches visibility changes (e.g. parent tab hidden / displayed)
		const containerObserver = new IntersectionObserver((entries) => {
			entries.forEach(entry => {
				if (entry.isIntersecting) {
					updateHeaderStyles();
				}
			});
		}, {
			threshold: 0.1
		});

		containerObserver.observe(container);

		// watch for width/size changes
		const ro = new ResizeObserver(() => updateHeaderStyles());
		ro.observe(container);

		// Clean up if the editor is destroyed
		editor.on('remove', () => {
			containerObserver.disconnect();
			ro.disconnect();
		});
	};

	const hasLoadingDiv = (parent : HTMLElement) => {

		if (!parent) {
			return false;
		}

		const firstElement = parent.firstElementChild;
		return firstElement?.nodeName == "DIV" && firstElement.classList.contains("loading");
	};

	const config: EditorConfig = {
			target,
			toolbar,
			promotion: false,
			branding: false,
			toolbar_sticky: false,
			autoresize_overflow_padding: 0,
			selection_toolbar_sticky: true,
			scroll_into_view_on_focus: false,
			statusbar: showElementPath || showWordCount || !!resizable,
			elementpath: showElementPath,
			resize: resizable,

			plugins,

			grid_preset: 'Bootstrap5',

			mentionsLookupUrl,
			mentionsQuery,

			content_css: contentCss,

			contextmenu: props.contextMenu,

			schema: props.allowsReact ? 'html5' : undefined,
			automatic_uploads: true,
			images_upload_handler: (blobInfo:any, progress: (val:number) => void) => new Promise((resolve, failure) => {
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
					resolve(location || '');
				};

				xhr.onerror = () => {
					failure('Image upload failed due to a Network Error.');
				};

				xhr.setRequestHeader("Content-Name", blobInfo.filename());
				xhr.send(blobInfo.blob());
			}),

			extended_valid_elements: props.allowsReact ? 'react-component[data-name|data-props|contenteditable|data-mounted|class],root-content[data-name|data-props|contenteditable|data-placeholder|class]' : undefined,
			custom_elements: props.allowsReact ? 'react-component,root-content' : undefined,

			// This class tells the plugin to treat the component as a single unit
			noneditable_noneditable_class: 'react-component',
			// This allows your root-content to remain editable inside it
			noneditable_editable_class: 'root-content',

			formats: props.allowsReact ? {
				reactComponent: {
					block: 'react-component',
					attributes: ['data-name', 'data-props', 'contenteditable', 'data-mounted', 'class']
				},
				rootContent: {
					block: 'root-content',
					attributes: ['data-name', 'data-props', 'contenteditable', 'data-placeholder', 'class']
				}
			} : undefined,

			setup: (editor: Editor) => {
				props.onSetup && props.onSetup(editor);

				editor.on('init', () => {

					if (props.dockHeader) {
						const iframe = editor.getContainer().querySelector("iframe");

						if (!iframe) {
							dockEditorHeader(editor);
							return;
						}

						const iframeDoc = iframe.contentDocument || iframe.contentWindow?.document;
						const reactComponent = iframeDoc?.body.querySelector("react-component") as HTMLElement;

						if (!hasLoadingDiv(reactComponent)) {
							dockEditorHeader(editor);
							return;
						}

						const rcObserver = new MutationObserver((mutationsList, observer) => {
							if (!hasLoadingDiv(reactComponent)) {
								observer.disconnect();
								dockEditorHeader(editor);
							}
						});

						// Start observing the parent for changes to its child elements
						rcObserver.observe(reactComponent, {
							childList: true,
							subtree: true
						});

					}

				});

				editor.on('Paste Change input Undo Redo', (e: any) => {
					if (onChange) {
						clearTimeout(timeoutRef.current);
						timeoutRef.current = setTimeout(() => {
							const evt = {
								target: {
									value: editor.getContent()
								}
							};
							onChange(evt);
						}, 100);
					}
				});
			},

			//quickbars_insert_toolbar: false,
			// NB: may be necessary to omit quicklink as this bypasses class injection
			//quickbars_selection_toolbar: 'bold italic | quicklink h2 h3 blockquote',
			//quickbars_selection_toolbar: 'bold italic | quicklink blockquote',

			//quickbars_image_toolbar: 'alignleft aligncenter alignright',
		};
		
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
				items: `${allowCode ? 'code | ' : ''}preview${props.allowFullscreen ? ' | 4r-fullscreen' : ''}`
			},
			format: {
				title: `Format`,
				//items: `bold italic underline strikethrough superscript subscript codeformat | styles blocks fontfamily fontsize align lineheight | forecolor backcolor | language | removeformat`
				items: `bold italic underline strikethrough superscript subscript | styles blocks fontfamily fontsize align lineheight | forecolor backcolor | language | removeformat`
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
				items: `${allowCode ? 'code' : ''} ${showWordCount ? 'wordcount' : ''}`
			};
			config.menubar += " tools";
		}

		if (props.showMenubar === false) {
			config.menubar = false;
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

			config.urlconverter_callback = (url: string, node: any, on_save: any, name: string) => {
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
						const file = (this as HTMLInputElement).files![0];
						const reader = new FileReader();

						reader.onload = function () {
							/*
							  Note: Now we need to register the blob in TinyMCEs image blob
							  registry. In the next release this part hopefully won't be
							  necessary, as we are looking to handle it internally.
							*/
							const id = 'blobid' + (new Date()).getTime();
							const blobCache = tinymce.activeEditor?.editorUpload.blobCache;
							const base64 = (reader.result as string)?.split(',')[1];
							const blobInfo = blobCache?.create(id, file, base64);

							if (blobInfo) {
								blobCache?.add(blobInfo);

								// call the callback and populate the Title field with the file name
								cb(blobInfo.blobUri(), { title: file.name });
							}
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
			config.plugins += " code ";
			config.toolbar += " | code ";
		}

		if (showWordCount) {
			config.plugins += " wordcount ";
		}
		
		// enforce autoresizing (prevents potential double scrollbar / repaint issues in Chrome when rendered within tabs)
		config.plugins += " autoresize";
		config.min_height = minHeight;
		// config.autoresize_bottom_margin = 20;

		/*
		// sticky header
		config.toolbar_sticky = true;

		// Use this if the scrollable area is NOT the window, 
		// but a specific parent element:
		
		*/

		// config.toolbar_sticky_offset = 150;
		//config.event_root = '.admin-page__content';
		//config.fixed_toolbar_container = '.admin-page__content';
		//config.ui_container = '.admin-page__content';

		//console.log("tinymce config: ", config);

		if (props.allowFullscreen) {
			config.toolbar += " | 4r-fullscreen";
			config.plugins += " 4r-fullscreen";
		}

		return tinymce.init(config);
	};

	useEffect(() => {
		if (textareaRef.current) {

			// @ts-ignore
			textareaRef.current.onGetValue = (val: string, ele: HTMLElement) => {
				if (ele === textareaRef.current && editor) {
					const htmlContent = editor.getContent();

					if (typeof required === 'boolean' && required && !htmlContent) {
						throw new PublicError("tiny-mce/validation", `This field is required.`);
					}

					if (typeof required === 'string') {
						var mtd = require("UI/Functions/Validation/" + required).default;
						var error = mtd(htmlContent);

						if (error) {
							throw error;
						}
					}

					return htmlContent;
				}
			};
		}
	}, [editor, required]);
	
	return (
		<textarea className="form-control ui-form-control textarea--tinymce"
			ref={textareaRef}
			{...otherProps}
		/>
	);
}

export default TinyMce;
