import Draft from '../DraftJs/Draft.min.js';
const { Editor, EditorState, RichUtils, convertFromHTML, getDefaultKeyBinding, ContentState } = Draft;

export default class RichEditor extends React.Component {

	constructor(props) {
		super(props);

		this.focus = () => this.edRef.focus();
		this.onChange = (editorState) => {
			var element = props.content && props.content.dom ? props.content.dom.current : null;

			if (element) {
				var bodyRect = document.body.getBoundingClientRect();
				var elemRect = element.getBoundingClientRect();
				var preview = element.closest(".page-form__preview");
				// admin-page__header + page-form__header + height of buttonbar + margin
				var yOffset = 50 + 44 + 68 + 16;
				document.documentElement.style.setProperty('--richeditor-buttonbar-top', ((elemRect.top - bodyRect.top) + preview.scrollTop - yOffset) + "px")
            }
			
			this.props.onStateChange(editorState);

			// Export into the provided node as well (probably).
			props.onChange && props.onChange(props.content);
		};

		this.handleKeyCommand = this._handleKeyCommand.bind(this);
		this.mapKeyToEditorCommand = this._mapKeyToEditorCommand.bind(this);
		this.toggleBlockType = this._toggleBlockType.bind(this);
		this.toggleInlineStyle = this._toggleInlineStyle.bind(this);
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
					<path id="fa_cog" d="M1152 896q0-106-75-181t-181-75-181 75-75 181 75 181 181 75 181-75 75-181zm512-109v222q0 12-8 23t-20 13l-185 28q-19 54-39 91 35 50 107 138 10 12 10 25t-9 23q-27 37-99 108t-94 71q-12 0-26-9l-138-108q-44 23-91 38-16 136-29 186-7 28-36 28h-222q-14 0-24.5-8.5t-11.5-21.5l-28-184q-49-16-90-37l-141 107q-10 9-25 9-14 0-25-11-126-114-165-168-7-10-7-23 0-12 8-23 15-21 51-66.5t54-70.5q-27-50-41-99l-183-27q-13-2-21-12.5t-8-23.5v-222q0-12 8-23t19-13l186-28q14-46 39-92-40-57-107-138-10-12-10-24 0-10 9-23 26-36 98.5-107.5t94.5-71.5q13 0 26 10l138 107q44-23 91-38 16-136 29-186 7-28 36-28h222q14 0 24.5 8.5t11.5 21.5l28 184q49 16 90 37l142-107q9-9 24-9 13 0 25 10 129 119 165 170 7 8 7 22 0 12-8 23-15 21-51 66.5t-54 70.5q26 50 41 98l183 28q13 2 21 12.5t8 23.5z" fill="currentColor" />
				</defs>
			</svg>

			<Editor
				blockStyleFn={getBlockStyle}
				customStyleMap={styleMap}
				editorState={editorState}
				handleKeyCommand={this.handleKeyCommand}
				keyBindingFn={this.mapKeyToEditorCommand}
				onChange={this.onChange}
				placeholder={textonly ? "Type here" : "Type here or press / to add a component"}
				ref={(edRef) => {
					this.edRef = edRef;
				}}
				spellCheck={true}
			/>
			<div className="RichEditor-buttonbar">
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

	render() {
		let className = 'RichEditor-styleButton';
		if (this.props.active) {
			className += ' RichEditor-activeButton';
		}

		return (
			<span className={className} onMouseDown={this.onToggle}>
				{this.props.label}
			</span>
		);
	}
}

const BLOCK_TYPES = [
	{ label: 'H1', style: 'header-one', icon: 'h1' },
	{ label: 'H2', style: 'header-two', icon: 'h2' },
	{ label: 'H3', style: 'header-three', icon: 'h3' },
	{ label: 'H4', style: 'header-four', icon: 'h4' },
	{ label: 'H5', style: 'header-five', icon: 'h5' },
	{ label: 'H6', style: 'header-six', icon: 'h6' },
	{ label: 'P', style: 'paragraph', icon: 'p' },
	{ label: 'Blockquote', style: 'blockquote', icon: 'blockquote' },
	{ label: 'UL', style: 'unordered-list-item', icon: 'ul' },
	{ label: 'OL', style: 'ordered-list-item', icon: 'ol' },
	{ label: 'Code Block', style: 'code-block', icon: 'code' },
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
					icon={type.icon}
					onToggle={props.onToggle}
					style={type.style}
				/>
			)}
		</div>
	);
};

var INLINE_STYLES = [
	{ label: 'Bold', style: 'BOLD' },
	{ label: 'Italic', style: 'ITALIC' },
	{ label: 'Underline', style: 'UNDERLINE' },
	{ label: 'Monospace', style: 'CODE' },
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
					onToggle={props.onToggle}
					style={type.style}
				/>
			)}
		</div>
	);
};
