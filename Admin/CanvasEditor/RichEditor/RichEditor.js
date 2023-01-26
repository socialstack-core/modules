import Draft from '../DraftJs/Draft.min.js';
const { Editor, EditorState, RichUtils, convertFromHTML, getDefaultKeyBinding, ContentState } = Draft;

const BUTTONBAR_MARGIN = 16;
const SCROLLBAR_WIDTH = 17;

export default class RichEditor extends React.Component {
	buttonBarRO = null;
	buttonBarElement = React.createRef();
	buttonBarRect = null;

	constructor(props) {
		super(props);

		this.focus = () => {
			this.edRef.focus();
		}

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
				var range = sel.getRangeAt(0);
				var clonedRange = range.cloneRange();
				var rangeRect = clonedRange.getBoundingClientRect();

				if (sel && sel.rangeCount && richEditor.contains(sel.anchorNode)) {

					// NB: cursor positioned at the very start of a line
					// is treated as though it begins at the end of the previous line
					// (causing the floating buttonbar to jump to the other end of the paragraph)
					var atSOL = false;

					// check: if the rangeRect for the following cursor position
					// has a Y value more than the current rangeRect, we're at the start of the line
					if (clonedRange.startOffset < clonedRange.commonAncestorContainer.length) {
						clonedRange.setStart(clonedRange.startContainer, clonedRange.startOffset + 1);
						var nextRangeRect = clonedRange.getBoundingClientRect();

						if (nextRangeRect.y > rangeRect.y) {
							atSOL = true;
                        }
					}

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

					this.updateButtonBarPosition(left, top);
				}

				// probably in a page editor context
				if (!this.props.textonly) {
					// page editor has overflow set, so we can't position the buttonbar above the top of the editor, as this will get clipped
					// if there's not room, show it below the insertion point instead
					if (top < 0) {
						top = rangeRect.y + rangeRect.height - richEditorRect.top + BUTTONBAR_MARGIN;
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

		this.extendedBlockRenderMap = props.blockRenderMap;
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
		this.onChange(
			RichUtils.toggleInlineStyle(
				this.props.editorState,
				inlineStyle
			)
		);
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

	render() {
		const { editorState, textonly } = this.props;

		// If the user changes block type before entering any text, we can
		// either style the placeholder or hide it. Let's just hide it now.
		let className = 'RichEditor-editor';
		var contentState = editorState.getCurrentContent();
		if (!contentState.hasText()) {
			if (contentState.getBlockMap().first().getType() !== 'unstyled') {
				className += ' RichEditor-hidePlaceholder';
			}
		}

		return <div className={className} onClick={this.focus}>
			<svg xmlns="http://www.w3.org/2000/svg" className="RichEditor__icons">
				<defs>
					<path id="RichEditor__icon-p" d="M10.5 15a.5.5 0 0 1-.5-.5V2H9v12.5a.5.5 0 0 1-1 0V9H7a4 4 0 1 1 0-8h5.5a.5.5 0 0 1 0 1H11v12.5a.5.5 0 0 1-.5.5z" />
					<path id="RichEditor__icon-blockquote" d="M12 12a1 1 0 0 0 1-1V8.558a1 1 0 0 0-1-1h-1.388c0-.351.021-.703.062-1.054.062-.372.166-.703.31-.992.145-.29.331-.517.559-.683.227-.186.516-.279.868-.279V3c-.579 0-1.085.124-1.52.372a3.322 3.322 0 0 0-1.085.992 4.92 4.92 0 0 0-.62 1.458A7.712 7.712 0 0 0 9 7.558V11a1 1 0 0 0 1 1h2Zm-6 0a1 1 0 0 0 1-1V8.558a1 1 0 0 0-1-1H4.612c0-.351.021-.703.062-1.054.062-.372.166-.703.31-.992.145-.29.331-.517.559-.683.227-.186.516-.279.868-.279V3c-.579 0-1.085.124-1.52.372a3.322 3.322 0 0 0-1.085.992 4.92 4.92 0 0 0-.62 1.458A7.712 7.712 0 0 0 3 7.558V11a1 1 0 0 0 1 1h2Z" />
					<path id="RichEditor__icon-code" d="M10.478 1.647a.5.5 0 1 0-.956-.294l-4 13a.5.5 0 0 0 .956.294l4-13zM4.854 4.146a.5.5 0 0 1 0 .708L1.707 8l3.147 3.146a.5.5 0 0 1-.708.708l-3.5-3.5a.5.5 0 0 1 0-.708l3.5-3.5a.5.5 0 0 1 .708 0zm6.292 0a.5.5 0 0 0 0 .708L14.293 8l-3.147 3.146a.5.5 0 0 0 .708.708l3.5-3.5a.5.5 0 0 0 0-.708l-3.5-3.5a.5.5 0 0 0-.708 0z" />
					<path id="RichEditor__icon-ul" fill-rule="evenodd" d="M5 11.5a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm-3 1a1 1 0 1 0 0-2 1 1 0 0 0 0 2zm0 4a1 1 0 1 0 0-2 1 1 0 0 0 0 2zm0 4a1 1 0 1 0 0-2 1 1 0 0 0 0 2z" />
					<path id="RichEditor__icon-ol1" fill-rule="evenodd" d="M5 11.5a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h9a.5.5 0 0 1 0 1h-9a.5.5 0 0 1-.5-.5z" />
					<path id="RichEditor__icon-ol2" d="M1.713 11.865v-.474H2c.217 0 .363-.137.363-.317 0-.185-.158-.31-.361-.31-.223 0-.367.152-.373.31h-.59c.016-.467.373-.787.986-.787.588-.002.954.291.957.703a.595.595 0 0 1-.492.594v.033a.615.615 0 0 1 .569.631c.003.533-.502.8-1.051.8-.656 0-1-.37-1.008-.794h.582c.008.178.186.306.422.309.254 0 .424-.145.422-.35-.002-.195-.155-.348-.414-.348h-.3zm-.004-4.699h-.604v-.035c0-.408.295-.844.958-.844.583 0 .96.326.96.756 0 .389-.257.617-.476.848l-.537.572v.03h1.054V9H1.143v-.395l.957-.99c.138-.142.293-.304.293-.508 0-.18-.147-.32-.342-.32a.33.33 0 0 0-.342.338v.041zM2.564 5h-.635V2.924h-.031l-.598.42v-.567l.629-.443h.635V5z" />
					<path id="RichEditor__icon-bold" d="M8.21 13c2.106 0 3.412-1.087 3.412-2.823 0-1.306-.984-2.283-2.324-2.386v-.055a2.176 2.176 0 0 0 1.852-2.14c0-1.51-1.162-2.46-3.014-2.46H3.843V13H8.21zM5.908 4.674h1.696c.963 0 1.517.451 1.517 1.244 0 .834-.629 1.32-1.73 1.32H5.908V4.673zm0 6.788V8.598h1.73c1.217 0 1.88.492 1.88 1.415 0 .943-.643 1.449-1.832 1.449H5.907z" />
					<path id="RichEditor__icon-italic" d="M7.991 11.674 9.53 4.455c.123-.595.246-.71 1.347-.807l.11-.52H7.211l-.11.52c1.06.096 1.128.212 1.005.807L6.57 11.674c-.123.595-.246.71-1.346.806l-.11.52h3.774l.11-.52c-1.06-.095-1.129-.211-1.006-.806z" />
					<path id="RichEditor__icon-underline" d="M5.313 3.136h-1.23V9.54c0 2.105 1.47 3.623 3.917 3.623s3.917-1.518 3.917-3.623V3.136h-1.23v6.323c0 1.49-.978 2.57-2.687 2.57-1.709 0-2.687-1.08-2.687-2.57V3.136zM12.5 15h-9v-1h9v1z" />
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
			</div>
		</div>;
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
		case 'blockquote': return 'RichEditor-blockquote';
		default: return null;
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
		let className = 'RichEditor-styleButton';
		if (this.props.active) {
			className += ' RichEditor-activeButton';
		}

		return (
			<span className={className} onMouseDown={this.onToggle} title={this.props.description || this.props.label}>
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
	{ label: 'H1', style: 'header-one', xicon: 'h1' },
	{ label: 'H2', style: 'header-two', xicon: 'h2' },
	{ label: 'H3', style: 'header-three', xicon: 'h3' },
	{ label: 'H4', style: 'header-four', xicon: 'h4' },
	{ label: 'H5', style: 'header-five', xicon: 'h5' },
	{ label: 'H6', style: 'header-six', xicon: 'h6' },
	{ label: 'P', style: 'paragraph', icon: 'p', description: 'Paragraph' },
	{ label: 'Blockquote', style: 'blockquote', icon: 'blockquote' },
	{ label: 'UL', style: 'unordered-list-item', icon: 'ul', description: 'Unordered list' },
	{ label: 'OL', style: 'ordered-list-item', icon: 'ol', iconPathCount: 2, description: 'Ordered list' },
	//{ label: 'Code Block', style: 'code-block', icon: 'code' },
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
					active={type.style === blockType}
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
	{ label: 'Bold', style: 'BOLD', icon: 'bold' },
	{ label: 'Italic', style: 'ITALIC', icon: 'italic' },
	{ label: 'Underline', style: 'UNDERLINE', icon: 'underline' },
	{ label: 'Monospace', style: 'CODE', icon: 'code' },
];

const InlineStyleControls = (props) => {
	const currentStyle = props.editorState.getCurrentInlineStyle();

	return (
		<div className="RichEditor-controls">
			{INLINE_STYLES.map((type) =>
				<StyleButton
					key={type.label}
					active={currentStyle.has(type.style)}
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
