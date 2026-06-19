import { getUrl } from 'UI/FileRef';
// @ts-ignore
import aceJsRef from './static/ace.js';
import { useEffect, useRef } from 'react';
import { DefaultInputType } from 'UI/Input/Default';

type AceEditorInputType = Omit<DefaultInputType, 'type'> & AceEditorProps;

declare global {
	interface InputPropsRegistry {
		'json': AceEditorInputType;
		'sql': AceEditorInputType;
	}
}

inputTypes.json = function (props) {
	const { field } = props;
	return <AceEditor
		{...field}
		type="json"
	/>;
};

inputTypes.sql = function (props) {
	const { field } = props;
	return <AceEditor
		{...field}
		type="sql"
	/>;
};

	interface AceEditorProps {
		type?: 'json' | 'sql';
		defaultValue?: string;
		value?: string;
		readonly?: boolean;
		name?: string;
		onChange?: (e: { target: { value: string } }) => void;
	}

var aceLoading: Promise<any> | undefined;

function loadAce(): Promise<any> {
	if (aceLoading) {
		return aceLoading;
	}

	return aceLoading = new Promise((resolve, reject) => {
		var script = document.createElement("script");
		script.src = getUrl(aceJsRef) || '';
		script.onload = () => {
			resolve((window as any).ace);
		};
		script.onerror = reject;
		document.head.appendChild(script);
	});
}

const AceEditor: React.FC<AceEditorProps> = (props) => {
	const editorRef = useRef<HTMLDivElement>(null);
	const editorInstanceRef = useRef<any>(null);
	const hiddenInputRef = useRef<HTMLInputElement>(null);

	useEffect(() => {
		if (!editorRef.current) {
			return;
		}

		var cancelled = false;

		loadAce().then((ace) => {
			if (cancelled || !editorRef.current) {
				return;
			}

			var ed = ace.edit(editorRef.current, {
				behavioursEnabled: true,
				wrapBehavioursEnabled: true,
				wrap: true,
				cursorStyle: "smooth",
			});

			editorInstanceRef.current = ed;

			var html = document.querySelector("html");

			if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ||
				(html && html.getAttribute("data-theme-variant") == "dark")) {
				ed.setTheme("ace/theme/monokai");
			}

			var type = props.type || 'json';
			ed.session.setMode("ace/mode/" + type);
			ed.session.setValue(props.defaultValue || props.value || '');

			if (props.readonly) {
				ed.session.setUseWorker(false);
				ed.setShowPrintMargin(false);
				ed.setReadOnly(true);
			}

			ed.session.on('change', () => {
				if (props.onChange) {
					props.onChange({ target: { value: ed.getValue() } });
				}
			});

			if (hiddenInputRef.current) {
				(hiddenInputRef.current as any).onGetValue = (val: string, field: HTMLInputElement) => {
					if (field != hiddenInputRef.current) {
						return;
					}
					return editorInstanceRef.current?.getValue();
				};
			}
		});

		return () => {
			cancelled = true;

			if (editorInstanceRef.current) {
				editorInstanceRef.current.destroy();
				editorInstanceRef.current = null;
			}
		};
	}, []);

	useEffect(() => {
		var ed = editorInstanceRef.current;
		if (!ed) {
			return;
		}

		var type = props.type || 'json';
		ed.session.setMode("ace/mode/" + type);
	}, [props.type]);

	useEffect(() => {
		var ed = editorInstanceRef.current;
		if (!ed) {
			return;
		}

		if (props.readonly && props.value != undefined) {
			ed.session.setValue(props.defaultValue || props.value || '');
		}
	}, [props.readonly, props.value, props.defaultValue]);

	return (
		<div className="aceeditor">
			<div ref={editorRef} />
			{!props.readonly && (
				<input type="hidden" name={props.name} ref={hiddenInputRef} />
			)}
		</div>
	);
};

export default AceEditor;
