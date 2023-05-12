import Draft from '../DraftJs/Draft.min.js';
import Modal from 'UI/Modal';
const { Editor, EditorState, RichUtils, convertFromHTML, getDefaultKeyBinding, ContentState, convertToRaw, Modifier } = Draft;

const BUTTONBAR_MARGIN = 16;
const SCROLLBAR_WIDTH = 17;
const DEFAULT_BLOCK_STYLE = 'paragraph';

export default class RichEditor extends React.Component {
	buttonBarRO = null;
	buttonBarElement = React.createRef();
	buttonBarRect = null;

	constructor(props) {
		super(props);

		this.focus = () => {
			this.edRef.focus();
		}

		this.state = {
 			showURLInput: false,
			urlValue: '',
			textAlignment: 'left'
		};

		this.urlRef = React.createRef();
		this.tokenRef = React.createRef();

		this.onChange = (editorState) => {
			var element = props.selectedNode && props.selectedNode.dom ? props.selectedNode.dom.current : null;

			if (!this.buttonBarRect) {
				this.buttonBarRect = this.buttonBarElement.current.getBoundingClientRect();
			}

			if (element && this.buttonBarRect) {
				var elemRect = element.getBoundingClientRect();

				var richEditor = element.closest(".rich-editor");
				var richEditorRect = richEditor ? richEditor.getBoundingClientRect() : { x: 0, y: 0 };

				var scrollParent = this.getScrollParent(richEditor);
				var parent = scrollParent || richEditor;
				var parentRect = parent.getBoundingClientRect();

				var editorOffset = this.props.textonly ? {
					x: richEditorRect.x - elemRect.x,
					y: richEditorRect.y - elemRect.y
				} : { x: 0, y: 0 };

				// default to just above field
				var left = 0 + editorOffset.x;
				var top = 0 + editorOffset.y - this.buttonBarRect.height;

				var sel = window.getSelection();

				if (sel && sel.rangeCount && richEditor.contains(sel.anchorNode)) {
					var range = sel.getRangeAt(0);
					var clonedRange = range.cloneRange();
					var rangeRect = clonedRange.getBoundingClientRect();

					// NB: cursor positioned at the very start of a line
					// is treated as though it begins at the end of the previous line
					// (causing the floating buttonbar to jump to the other end of the paragraph)
					var atSOL = false;

					/*
					// check: if the rangeRect for the following cursor position
					// has a Y value more than the current rangeRect, we're at the start of the line
					if (clonedRange.startOffset < clonedRange.commonAncestorContainer.length) {
						clonedRange.setStart(clonedRange.startContainer, clonedRange.startOffset + 1);
						var nextRangeRect = clonedRange.getBoundingClientRect();

						if (nextRangeRect.y > rangeRect.y) {
							atSOL = true;
                        }
					}
					 */

					left = (atSOL ? 0 : rangeRect.x) - elemRect.left - (this.buttonBarRect.width / 2);
					top = (atSOL ? nextRangeRect.y : rangeRect.y) - (this.props.textonly ? elemRect.top : richEditorRect.top) - this.buttonBarRect.height - BUTTONBAR_MARGIN;

					// limit to left edge
					left = Math.max(left, editorOffset.x);

					// limit to right edge
					var right = (this.props.textonly ? parentRect.x : 0) + Math.max(0, left) + this.buttonBarRect.width + BUTTONBAR_MARGIN + SCROLLBAR_WIDTH;
					var availableWidth = this.props.textonly ? window.innerWidth : richEditorRect.width;
					var diff = right - (availableWidth - SCROLLBAR_WIDTH);

					if (diff > 0) {
						left -= diff;
					}

					// probably in a page editor context
					if (!this.props.textonly) {
						// page editor has overflow set, so we can't position the buttonbar above the top of the editor, as this will get clipped
						// if there's not room, show it below the insertion point instead
						if (top < 0) {
							top = rangeRect.y + rangeRect.height - richEditorRect.top + BUTTONBAR_MARGIN;
						}

					}

					this.updateButtonBarPosition(left, top);
				}

            }
			
			this.props.onStateChange(editorState);

			// Export into the provided node as well (probably).
			props.onChange && props.onChange(props.content);
		};

		this.handleKeyCommand = this._handleKeyCommand.bind(this);
		this.mapKeyToEditorCommand = this._mapKeyToEditorCommand.bind(this);
		this.toggleBlockType = this._toggleBlockType.bind(this);
		this.toggleInlineStyle = this._toggleInlineStyle.bind(this);
		this.toggleAlignment = this._toggleAlignment.bind(this);

		this.extendedBlockRenderMap = props.blockRenderMap;

 		this.promptForLink = this._promptForLink.bind(this);
        this.onURLChange = this._onURLChange.bind(this);
        this.confirmLink = this._confirmLink.bind(this);
        this.onLinkInputKeyDown = this._onLinkInputKeyDown.bind(this);
		this.removeLink = this._removeLink.bind(this);
		this.promptForToken = this._promptForToken.bind(this);
		this.insertText = this._insertText.bind(this);
	}

	_handleKeyCommand(command, editorState) {
		const newState = RichUtils.handleKeyCommand(editorState, command);
		if (newState) {
			this.onChange(newState);
			return true;
		}
		return false;
	}

	_mapKeyToEditorCommand(e) {
		if (e.key == '/') {
			// Add component mode if the placeholder is visible. Replaces the editor with the specified block.
			var contentState = this.props.editorState.getCurrentContent();
			if (!contentState.hasText()) {
				if (contentState.getBlockMap().first().getType() == 'unstyled') {
					// Placeholder is visible. Add component mode.
					this.props.onAddComponent && this.props.onAddComponent();
					e.preventDefault();
					return;
				}
			}
		}

		if (e.keyCode === 9 /* TAB */) {
			const newEditorState = RichUtils.onTab(
				e,
				this.props.editorState,
				4, /* maxDepth */
			);
			if (newEditorState !== this.props.editorState) {
				this.onChange(newEditorState);
			}
			return;
		}
		return getDefaultKeyBinding(e);
	}

	_toggleBlockType(blockType) {
		this.onChange(
			RichUtils.toggleBlockType(
				this.props.editorState,
				blockType
			)
		);
	}

	_toggleInlineStyle(inlineStyle) {

		if (inlineStyle == "TOKEN") {
			this.promptForToken();
			return;
		}

		if (inlineStyle == "LINK") {
			this.promptForLink();
			return;
		}

		this.onChange(
			RichUtils.toggleInlineStyle(
				this.props.editorState,
				inlineStyle
			)
		);
	}

	_toggleAlignment(alignment) {
		var textAlignment = 'left';

		switch (alignment) {
			case 'CENTRE':
				textAlignment = 'center';
				break;
			case 'RIGHT':
				textAlignment = 'right';
				break;
        }

		this.setState({
			textAlignment: textAlignment
		});
	}

	getScrollParent(node) {

		if (node == null) {
			return null;
		}

		return (node.scrollHeight > node.clientHeight) ? node : this.getScrollParent(node.parentNode);
	}

	updateButtonBarPosition(x, y) {
		var rootStyle = document.documentElement.style;
		rootStyle.setProperty('--richeditor-buttonbar-left', x + "px");
		rootStyle.setProperty('--richeditor-buttonbar-top', y + "px");
    }

	componentDidMount() {
		this.buttonBarRO = new ResizeObserver(entries => {
			for (const entry of entries) {

				if (entry.contentRect.width != 0) {
					var rootStyle = document.documentElement.style;
					rootStyle.setProperty('--richeditor-buttonbar-left', "-1000px");

					// buttonbar shown
					this.buttonBarRect = this.buttonBarElement.current.getBoundingClientRect();
					break;
				}
			}
		});

		this.buttonBarRO.observe(this.buttonBarElement.current);
	}

	componentWillUnmount() {
		if (this.buttonBarRO) {
			this.buttonBarRO.disconnect();
		}
	}

	_promptForLink(e) {
		if (e) {
			e.preventDefault();
        }
        const {editorState} = this.props;
        const selection = editorState.getSelection();
        if (!selection.isCollapsed()) {
            const contentState = editorState.getCurrentContent();
            const startKey = editorState.getSelection().getStartKey();
            const startOffset = editorState.getSelection().getStartOffset();
            const blockWithLinkAtBeginning = contentState.getBlockForKey(startKey);
            const linkKey = blockWithLinkAtBeginning.getEntityAt(startOffset);

            let url = '';
            if (linkKey) {
              	const linkInstance = contentState.getEntity(linkKey);
              	url = linkInstance.getData().url;
            }

            this.setState({
              	showURLInput: true,
              	urlValue: url,
            }, () => {
              	setTimeout(() => this.urlRef.current.focus(), 0);
            });
        }
	}
	
	_promptForToken(e) {
		if (e) {
			e.preventDefault();
		}
		
        this.setState({
			showTokenInput: true
		}, () => {
			setTimeout(() => this.tokenRef.current.focus(), 0);
	  	});
    }

	_onURLChange(e) {
		this.setState({ urlValue: e.target.value });
	}

	_insertText(textToInsert) {
		const {editorState} = this.props;
	
		const currentContent = editorState.getCurrentContent();
		const currentSelection = editorState.getSelection();
	
		let newContent = Modifier.replaceText(
			currentContent,
			currentSelection,
			textToInsert
		);
	
		const textToInsertSelection = currentSelection.set('focusOffset', currentSelection.getFocusOffset() + textToInsert.length);
	
		let inlineStyles = editorState.getCurrentInlineStyle();
	
		inlineStyles.forEach(inLineStyle => newContent = Modifier.applyInlineStyle(newContent, textToInsertSelection, inLineStyle));
	
		let newState = EditorState.push(editorState, newContent, 'insert-characters');
	
		if (newState) {
			newState = EditorState.forceSelection(newState, textToInsertSelection.set('anchorOffset', textToInsertSelection.getAnchorOffset() + textToInsert.length));
		}

		if (newState) {
			this.onChange(newState);
			this.setState({
				showTokenInput: false,
				tokenValue: null
			  }, () => {
				setTimeout(() => this.tokenRef.focus(), 0);
			});
			return true;
		}

		return false;
	}

	_confirmLink(e) {
		if (e) {
			e.preventDefault();
        }
		const {editorState} = this.props;
        const urlValue = this.urlRef.current.value;
        const contentState = editorState.getCurrentContent();
        const contentStateWithEntity = contentState.createEntity(
            'LINK',
            'MUTABLE',
            {url: urlValue}
		);
        const entityKey = contentStateWithEntity.getLastCreatedEntityKey();
        const newEditorState = EditorState.set(editorState, { currentContent: contentStateWithEntity });
		this.onChange(RichUtils.toggleLink(
				newEditorState,
				newEditorState.getSelection(),
				entityKey
        ));
        this.setState({
            showURLInput: false,
            urlValue: '',
          }, () => {
            setTimeout(() => this.edRef.focus(), 0);
        });
    }

    _onLinkInputKeyDown(e) {
        if (e.which === 13) {
        	this._confirmLink(e);
        }
    }

    _removeLink(e) {
		if (e) {
			e.preventDefault();
		}
        const {editorState} = this.props;
        const selection = editorState.getSelection();
        if (!selection.isCollapsed()) {
			this.onChange(RichUtils.toggleLink(editorState, selection, null));
		}

		this.setState({
			showURLInput: false,
			urlValue: '',
		}, () => {
			setTimeout(() => this.edRef.focus(), 0);
		});

    }

	render() {
		const { editorState, textonly, context } = this.props;

		// If the user changes block type before entering any text, we can
		// either style the placeholder or hide it. Let's just hide it now.
		let className = 'RichEditor-editor';
		var contentState = editorState.getCurrentContent();
		if (!contentState.hasText()) {
			if (contentState.getBlockMap().first().getType() !== 'unstyled') {
				className += ' RichEditor-hidePlaceholder';
			}
		}

		var tokenOptions = context 
			? context.map(contextItem => { 
					return {
						value: contextItem.isPrice
							? "${currencysymbol}${context." + contextItem.name + "}"
							: "${context." + contextItem.name + "}",
						label: contextItem.value 
							? contextItem.label + " - " + contextItem.value 
							: contextItem.label
					} 
				}) 
			: [];

		if (tokenOptions) {
			tokenOptions.unshift({
				value: "${currencysymbol}",
				label: "Currency Symbol"
			});
		}

		return <>
			<Modal visible={this.state.showURLInput} title={`Insert Link`} className="richeditor-modal link-modal"
				onClose={() => this.setState({ showURLInput: false })}>
				<div className="mb-3">
					<label htmlFor="link_url" className="form-label">
						{`Link URL`}
					</label>
					<input className="form-control" type="text" id="link_url" ref={this.urlRef}
						placeholder={`/local or https://www.remote.com address`}
						value={this.state.urlValue} onInput={this.onURLChange} />
				</div>

				<footer className="richeditor-modal-footer">
					<button type="button" className="btn btn-outline-danger" onClick={() => this.removeLink()}
						style={{ 'visibility': this.state.urlValue && this.state.urlValue.length > 0 ? 'visible' : 'hidden'}}>
						<i className="far fa-fw fa-trash" />
						{`Remove link`}
					</button>

					<div>
						<button type="button" className="btn btn-primary" disabled={this.state.urlValue && this.state.urlValue.length > 0 ? undefined : true}
							onClick={() => this.confirmLink()}>
							{`Save`}
						</button>
						<button type="button" className="btn btn-outline-primary" onClick={() => this.setState({ showURLInput: false })}>
							{`Cancel`}
						</button>
					</div>
				</footer>
			</Modal>

			<Modal visible={this.state.showTokenInput} title={`Insert Token`} className="richeditor-modal prompy-modal"
				onClose={() => this.setState({ showTokenInput: false, tokenValue: null })}>
				<div className="mb-3">
					<label htmlFor="token" className="form-label">
						{`Choose the token`}
					</label>
					<select className="form-select" id="token" ref={this.tokenRef} onChange={e => this.setState({ tokenValue: e.target.value })}>
						{
							tokenOptions.map(option => <option value={option.value}>
								{option.label}
							</option>)
						}
					</select >
				</div>

				<footer className="richeditor-modal-footer">
					<div>
						<button type="button" className="btn btn-primary"
							onClick={() => {
								this.insertText(this.tokenRef.current.value);
							}}>
							{`Save`}
						</button>
						<button type="button" className="btn btn-outline-primary" onClick={() => this.setState({ showTokenInput: false, tokenValue: null })}>
							{`Cancel`}
						</button>
					</div>
				</footer>
			</Modal>

			<div className="RichEditor-wrapper">
				<div className={className} onClick={this.focus}>
					<svg xmlns="http://www.w3.org/2000/svg" className="RichEditor__icons">
						<defs>
							<path id="RichEditor__icon-h1" d="M12.775 3.77c-.19.386-.516.784-.976 1.195-.46.41-.997.762-1.611 1.05v1.063a7.287 7.287 0 001.154-.568 5.633 5.633 0 001.043-.756v7h1.1V3.77zm-11.98.037v8.947h1.184V8.537H6.63v4.217h1.183V3.807H6.631V7.48H1.979V3.807z" />
							<path id="RichEditor__icon-h2" d="M12.318 3.77c-.85 0-1.528.222-2.033.666-.504.439-.794 1.08-.87 1.921l1.128.116c.004-.562.165-1.001.482-1.319.318-.317.741-.474 1.27-.474.5 0 .904.15 1.213.45.313.298.47.663.47 1.099 0 .415-.17.856-.511 1.324-.342.464-1 1.095-1.973 1.892-.626.513-1.106.965-1.44 1.356-.329.39-.57.786-.72 1.19-.094.243-.137.498-.129.763h5.914v-1.057h-4.387c.122-.2.276-.396.463-.592.187-.199.61-.575 1.27-1.128.79-.668 1.354-1.192 1.691-1.575.342-.382.586-.748.733-1.093.146-.346.218-.697.218-1.055 0-.704-.25-1.294-.75-1.77s-1.18-.714-2.039-.714zM.795 3.807v8.947h1.184V8.537H6.63v4.217h1.183V3.807H6.631V7.48H1.979V3.807z" />
							<path id="RichEditor__icon-h3" d="M12.154 3.77c-.708 0-1.301.202-1.777.609-.476.403-.78.972-.914 1.709l1.098.195c.08-.537.264-.938.548-1.207.285-.268.641-.404 1.069-.404.431 0 .784.134 1.056.398.273.265.409.597.409 1 0 .509-.187.885-.561 1.13a2.264 2.264 0 01-1.264.366 1.76 1.76 0 01-.177-.013l-.121.965c.309-.082.567-.122.775-.122.509 0 .926.165 1.256.495.33.325.494.74.494 1.244 0 .529-.18.974-.537 1.332a1.762 1.762 0 01-1.305.537 1.61 1.61 0 01-1.111-.41c-.301-.277-.515-.725-.64-1.348l-1.1.147c.073.744.368 1.35.884 1.818.521.468 1.176.701 1.961.701.871 0 1.59-.27 2.16-.81a2.65 2.65 0 00.854-1.99c0-.578-.147-1.06-.44-1.442-.292-.387-.703-.64-1.232-.762.407-.187.714-.44.922-.758.207-.317.31-.668.31-1.054 0-.407-.108-.792-.328-1.155a2.24 2.24 0 00-.941-.855 2.897 2.897 0 00-1.348-.316zm-11.36.037v8.947H1.98V8.537H6.63v4.217h1.183V3.807H6.631V7.48H1.979V3.807z" />
							<path id="RichEditor__icon-h4" d="M.795 3.807v8.947h1.184V8.537H6.63v4.217h1.183V3.807H6.631V7.48H1.979V3.807zm12.273 0L8.986 9.604v1.007h3.881v2.143h1.1V10.61h1.209V9.604h-1.21V3.807zm-.2 1.763v4.034h-2.802z" />
							<path id="RichEditor__icon-h5" d="M.795 3.807v8.947h1.184V8.537H6.63v4.217h1.183V3.807H6.631V7.48H1.979V3.807zm9.613.12l-.867 4.596 1.031.135c.163-.256.387-.464.672-.623a1.9 1.9 0 01.96-.244c.569 0 1.028.18 1.378.543.354.362.531.858.531 1.484 0 .66-.182 1.188-.549 1.586-.366.4-.814.6-1.343.6-.44 0-.816-.141-1.13-.422-.308-.285-.506-.708-.591-1.27l-1.154.098c.073.753.366 1.357.879 1.813.516.455 1.182.683 1.996.683.993 0 1.776-.361 2.35-1.086.471-.59.708-1.29.708-2.1 0-.85-.27-1.546-.806-2.087-.537-.541-1.196-.813-1.977-.813-.59 0-1.154.188-1.691.563l.482-2.404h3.57V3.928z" />
							<path id="RichEditor__icon-h6" d="M12.508 3.77c-.944 0-1.695.341-2.252 1.025-.639.785-.96 2.042-.96 3.771 0 1.547.29 2.657.868 3.332.578.672 1.325 1.008 2.24 1.008.537 0 1.017-.128 1.44-.385a2.63 2.63 0 00.996-1.103 3.441 3.441 0 00.365-1.57c0-.855-.259-1.55-.775-2.086-.513-.541-1.135-.813-1.868-.813a2.54 2.54 0 00-2.172 1.19c.009-.896.112-1.584.311-2.069.2-.484.473-.853.819-1.105a1.51 1.51 0 01.921-.293c.436 0 .801.157 1.098.47.18.196.318.509.416.94l1.092-.084c-.09-.696-.361-1.242-.813-1.637-.447-.394-1.022-.591-1.726-.591zM.795 3.807v8.947h1.184V8.537H6.63v4.217h1.183V3.807H6.631V7.48H1.979V3.807zM12.344 7.92c.5 0 .914.179 1.244.537.33.354.494.84.494 1.459 0 .643-.166 1.151-.5 1.525-.334.375-.733.563-1.197.563-.318 0-.62-.09-.908-.27a1.835 1.835 0 01-.678-.793 2.574 2.574 0 01-.238-1.086c0-.577.173-1.044.52-1.398a1.69 1.69 0 011.263-.537z" />
							<path id="RichEditor__icon-p" d="M10.5 15a.5.5 0 0 1-.5-.5V2H9v12.5a.5.5 0 0 1-1 0V9H7a4 4 0 1 1 0-8h5.5a.5.5 0 0 1 0 1H11v12.5a.5.5 0 0 1-.5.5z" />
							<path id="RichEditor__icon-blockquote" d="M12 12a1 1 0 0 0 1-1V8.558a1 1 0 0 0-1-1h-1.388c0-.351.021-.703.062-1.054.062-.372.166-.703.31-.992.145-.29.331-.517.559-.683.227-.186.516-.279.868-.279V3c-.579 0-1.085.124-1.52.372a3.322 3.322 0 0 0-1.085.992 4.92 4.92 0 0 0-.62 1.458A7.712 7.712 0 0 0 9 7.558V11a1 1 0 0 0 1 1h2Zm-6 0a1 1 0 0 0 1-1V8.558a1 1 0 0 0-1-1H4.612c0-.351.021-.703.062-1.054.062-.372.166-.703.31-.992.145-.29.331-.517.559-.683.227-.186.516-.279.868-.279V3c-.579 0-1.085.124-1.52.372a3.322 3.322 0 0 0-1.085.992 4.92 4.92 0 0 0-.62 1.458A7.712 7.712 0 0 0 3 7.558V11a1 1 0 0 0 1 1h2Z" />
							<path id="RichEditor__icon-code" d="M10.478 1.647a.5.5 0 1 0-.956-.294l-4 13a.5.5 0 0 0 .956.294l4-13zM4.854 4.146a.5.5 0 0 1 0 .708L1.707 8l3.147 3.146a.5.5 0 0 1-.708.708l-3.5-3.5a.5.5 0 0 1 0-.708l3.5-3.5a.5.5 0 0 1 .708 0zm6.292 0a.5.5 0 0 0 0 .708L14.293 8l-3.147 3.146a.5.5 0 0 0 .708.708l3.5-3.5a.5.5 0 0 0 0-.708l-3.5-3.5a.5.5 0 0 0-.708 0z" />
							<path id="RichEditor__icon-link1" d="M4.715 6.542 3.343 7.914a3 3 0 1 0 4.243 4.243l1.828-1.829A3 3 0 0 0 8.586 5.5L8 6.086a1.002 1.002 0 0 0-.154.199 2 2 0 0 1 .861 3.337L6.88 11.45a2 2 0 1 1-2.83-2.83l.793-.792a4.018 4.018 0 0 1-.128-1.287z" />
							<path id="RichEditor__icon-link2" d="M6.586 4.672A3 3 0 0 0 7.414 9.5l.775-.776a2 2 0 0 1-.896-3.346L9.12 3.55a2 2 0 1 1 2.83 2.83l-.793.792c.112.42.155.855.128 1.287l1.372-1.372a3 3 0 1 0-4.243-4.243L6.586 4.672z" />
							<path id="RichEditor__icon-ul" fill-rule="evenodd" d="M5 11.5a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm-3 1a1 1 0 1 0 0-2 1 1 0 0 0 0 2zm0 4a1 1 0 1 0 0-2 1 1 0 0 0 0 2zm0 4a1 1 0 1 0 0-2 1 1 0 0 0 0 2z" />
							<path id="RichEditor__icon-ol1" fill-rule="evenodd" d="M5 11.5a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5z" />
							<path id="RichEditor__icon-ol2" d="M1.713 11.865v-.474H2c.217 0 .363-.137.363-.317 0-.185-.158-.31-.361-.31-.223 0-.367.152-.373.31h-.59c.016-.467.373-.787.986-.787.588-.002.954.291.957.703a.595.595 0 0 1-.492.594v.033a.615.615 0 0 1 .569.631c.003.533-.502.8-1.051.8-.656 0-1-.37-1.008-.794h.582c.008.178.186.306.422.309.254 0 .424-.145.422-.35-.002-.195-.155-.348-.414-.348h-.3zm-.004-4.699h-.604v-.035c0-.408.295-.844.958-.844.583 0 .96.326.96.756 0 .389-.257.617-.476.848l-.537.572v.03h1.054V9H1.143v-.395l.957-.99c.138-.142.293-.304.293-.508 0-.18-.147-.32-.342-.32a.33.33 0 0 0-.342.338v.041zM2.564 5h-.635V2.924h-.031l-.598.42v-.567l.629-.443h.635V5z" />
							<path id="RichEditor__icon-bold" d="M8.21 13c2.106 0 3.412-1.087 3.412-2.823 0-1.306-.984-2.283-2.324-2.386v-.055a2.176 2.176 0 0 0 1.852-2.14c0-1.51-1.162-2.46-3.014-2.46H3.843V13H8.21zM5.908 4.674h1.696c.963 0 1.517.451 1.517 1.244 0 .834-.629 1.32-1.73 1.32H5.908V4.673zm0 6.788V8.598h1.73c1.217 0 1.88.492 1.88 1.415 0 .943-.643 1.449-1.832 1.449H5.907z" />
							<path id="RichEditor__icon-italic" d="M7.991 11.674 9.53 4.455c.123-.595.246-.71 1.347-.807l.11-.52H7.211l-.11.52c1.06.096 1.128.212 1.005.807L6.57 11.674c-.123.595-.246.71-1.346.806l-.11.52h3.774l.11-.52c-1.06-.095-1.129-.211-1.006-.806z" />
							<path id="RichEditor__icon-underline" d="M5.313 3.136h-1.23V9.54c0 2.105 1.47 3.623 3.917 3.623s3.917-1.518 3.917-3.623V3.136h-1.23v6.323c0 1.49-.978 2.57-2.687 2.57-1.709 0-2.687-1.08-2.687-2.57V3.136zM12.5 15h-9v-1h9v1z" />
							<path id="RichEditor__icon-left" fill-rule="evenodd" d="M2 12.5a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5zm0-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5zm0-3a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5zm0-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5z" />
							<path id="RichEditor__icon-right" fill-rule="evenodd" d="M6 12.5a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5zm-4-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5zm4-3a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5zm-4-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5z" />
							<path id="RichEditor__icon-center" fill-rule="evenodd" d="M4 12.5a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5zm-2-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5zm2-3a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7a.5.5 0 0 1-.5-.5zm-2-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5z" />
							<path id="RichEditor__icon-justified" fill-rule="evenodd" d="M2 12.5a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5zm0-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5zm0-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5zm0-3a.5.5 0 0 1 .5-.5h11a.5.5 0 0 1 0 1h-11a.5.5 0 0 1-.5-.5z" />
							<path id="RichEditor__icon-token" d="M2.114 8.063V7.9c1.005-.102 1.497-.615 1.497-1.6V4.503c0-1.094.39-1.538 1.354-1.538h.273V2h-.376C3.25 2 2.49 2.759 2.49 4.352v1.524c0 1.094-.376 1.456-1.49 1.456v1.299c1.114 0 1.49.362 1.49 1.456v1.524c0 1.593.759 2.352 2.372 2.352h.376v-.964h-.273c-.964 0-1.354-.444-1.354-1.538V9.663c0-.984-.492-1.497-1.497-1.6zM13.886 7.9v.163c-1.005.103-1.497.616-1.497 1.6v1.798c0 1.094-.39 1.538-1.354 1.538h-.273v.964h.376c1.613 0 2.372-.759 2.372-2.352v-1.524c0-1.094.376-1.456 1.49-1.456V7.332c-1.114 0-1.49-.362-1.49-1.456V4.352C13.51 2.759 12.75 2 11.138 2h-.376v.964h.273c.964 0 1.354.444 1.354 1.538V6.3c0 .984.492 1.497 1.497 1.6z" />
						</defs>
					</svg>

					<Editor
						blockStyleFn={getBlockStyle}
						customStyleMap={styleMap}
						editorState={editorState}
						handleKeyCommand={this.handleKeyCommand}
						keyBindingFn={this.mapKeyToEditorCommand}
						onChange={this.onChange}
						placeholder={textonly ? `Type here` : `Type here or press / to add a component`}
						ref={(edRef) => {
							this.edRef = edRef;
						}}
						spellCheck={true}
						blockRenderMap={this.extendedBlockRenderMap}
					/>
					<div className="RichEditor-buttonbar" ref={this.buttonBarElement}>
						<BlockStyleControls
							editorState={editorState}
							onToggle={this.toggleBlockType}
						/>
						<InlineStyleControls
							editorState={editorState}
							onToggle={this.toggleInlineStyle}
						/>
						<AlignmentControls
							editorState={editorState}
							onToggle={this.toggleBlockType}
						/>
					</div>
				</div>
			</div>
		</>;
	}
}

// Custom overrides for "code" style.
const styleMap = {
	CODE: {
		backgroundColor: 'rgba(0, 0, 0, 0.05)',
		fontFamily: '"Inconsolata", "Menlo", "Consolas", monospace',
		fontSize: 16,
		padding: 2,
	},
};

function getBlockStyle(block) {
	switch (block.getType()) {
		case 'blockquote': 
			return 'RichEditor-blockquote';
		case "left":
			return "align-left";
		case "center":
			return "align-center";
		case "right":
			return "align-right";
		default:
			return null;
	}
}

class StyleButton extends React.Component {
	constructor() {
		super();
		this.onToggle = (e) => {
			e.preventDefault();
			this.props.onToggle(this.props.style);
		};
	}

	renderIconPaths(icon, pathCount) {

		if (!pathCount || pathCount == 1) {
			return <use xlinkHref={'#RichEditor__icon-' + icon} />;
		}

		return Array.from({ length: pathCount }).map((_, i) => <use xlinkHref={'#RichEditor__icon-' + icon + (i+1)} />);
    }

	render() {
		let title = this.props.description || this.props.label;
		let className = 'RichEditor-styleButton';
		if (this.props.active) {
			className += ' RichEditor-activeButton';
		}

		if (this.props.disabled) {
			className += ' RichEditor-disabledButton';
			title = undefined;
        }

		return (
			<span className={className} onMouseDown={this.props.disabled ? undefined : this.onToggle} title={title}>
				{this.props.icon && <>
					<svg className="RichEditor-buttonbar-icon" fill="currentColor" viewBox="0 0 16 16">
						{this.renderIconPaths(this.props.icon, this.props.iconPathCount)}
					</svg>
				</>}
				{!this.props.icon && this.props.label}
			</span>
		);
	}
}

const BLOCK_TYPES = [
	{ label: 'H1', style: 'header-one', icon: 'h1', description: `Heading level 1` },
	{ label: 'H2', style: 'header-two', icon: 'h2', description: `Heading level 2` },
	{ label: 'H3', style: 'header-three', icon: 'h3', description: `Heading level 3` },
	{ label: 'H4', style: 'header-four', icon: 'h4', description: `Heading level 4` },
	{ label: 'H5', style: 'header-five', icon: 'h5', description: `Heading level 5` },
	{ label: 'H6', style: 'header-six', icon: 'h6', description: `Heading level 6` },
	{ label: 'P', style: 'paragraph', icon: 'p', description: `Paragraph` },
	{ label: `Blockquote`, style: 'blockquote', icon: 'blockquote' },
	{ label: 'UL', style: 'unordered-list-item', icon: 'ul', description: `Unordered list` },
	{ label: 'OL', style: 'ordered-list-item', icon: 'ol', iconPathCount: 2, description: `Ordered list` },
];

const BlockStyleControls = (props) => {
	const { editorState } = props;
	const selection = editorState.getSelection();
	const blockType = editorState
		.getCurrentContent()
		.getBlockForKey(selection.getStartKey())
		.getType();

	return (
		<div className="RichEditor-controls">
			{BLOCK_TYPES.map((type) => 
				<StyleButton
					key={type.label}
					active={type.style === blockType || blockType == "unstyled" && type.style == DEFAULT_BLOCK_STYLE}
					label={type.label}
					description={type.description}
					icon={type.icon}
					iconPathCount={type.iconPathCount}
					onToggle={props.onToggle}
					style={type.style}
				/>
			)}
		</div>
	);
};

var INLINE_STYLES = [
	{ label: `Bold`, style: 'BOLD', icon: 'bold' },
	{ label: `Italic`, style: 'ITALIC', icon: 'italic' },
	{ label: `Underline`, style: 'UNDERLINE', icon: 'underline' },
	{ label: `Monospace`, style: 'CODE', icon: 'code', description: `Code` },
	{ label: `Hyperlink`, style: 'LINK', icon: 'link', iconPathCount: 2, description: `Insert link` },
	{ label: `Token`, style: 'TOKEN', icon: 'token', description: `Insert token` },
];

const InlineStyleControls = (props) => {
	var editorState = props.editorState;
	const currentStyle = editorState.getCurrentInlineStyle();
	const selection = editorState.getSelection();

	return (
		<div className="RichEditor-controls">
			{INLINE_STYLES.map((type) => {
				var isActive = currentStyle.has(type.style);
				var disabled = false;

				if (type.style == "LINK") {
					isActive = false;

					if (!selection.isCollapsed()) {
						const contentState = editorState.getCurrentContent();
						const startKey = editorState.getSelection().getStartKey();
						const startOffset = editorState.getSelection().getStartOffset();
						const blockWithLinkAtBeginning = contentState.getBlockForKey(startKey);
						const linkKey = blockWithLinkAtBeginning.getEntityAt(startOffset);

						let url = '';
						if (linkKey) {
							const linkInstance = contentState.getEntity(linkKey);
							url = linkInstance.getData().url;
						}

						if (url && url.length > 0) {
							isActive = true;
						}

					} else {
						disabled = true;
                    }

				}

				return <StyleButton
					key={type.label}
					active={isActive}
					label={type.label}
					description={type.description}
					icon={type.icon}
					iconPathCount={type.iconPathCount}
					onToggle={props.onToggle}
					style={type.style}
					disabled={disabled}
				/>
			})}
		</div>
	);
};

var ALIGNMENT_TYPES = [
	{ label: `Align left`, style: 'left', icon: 'left' },
	{ label: `Centred`, style: 'center', icon: 'center' },
	{ label: `Align right`, style: 'right', icon: 'right' },
	//{ label: `Justified`, style: 'JUSTIFY', icon: 'justified' },
];

const AlignmentControls = (props) => {
	const { editorState } = props;
	const selection = editorState.getSelection();
	const blockType = editorState
		.getCurrentContent()
		.getBlockForKey(selection.getStartKey())
		.getType();

	return (
		<div className="RichEditor-controls">
			{ALIGNMENT_TYPES.map((type) =>
				<StyleButton
					key={type.label}
					active={type.style === blockType || blockType == "unstyled" && type.style == DEFAULT_BLOCK_STYLE}
					label={type.label}
					description={type.description}
					icon={type.icon}
					iconPathCount={type.iconPathCount}
					onToggle={props.onToggle}
					style={type.style}
				/>
			)}
		</div>
	);
};
