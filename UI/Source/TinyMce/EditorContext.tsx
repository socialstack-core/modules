import { createContext, useContext } from 'react';

interface EditorContext {
	isAdmin: boolean;
	isEditing: boolean;
}

const editorCtx = createContext<EditorContext>({
	isAdmin: false,
	isEditing: false
});

export function useEditor() {
	return useContext(editorCtx) || {isAdmin: false, isEditing: false};
}

export {
	editorCtx
};

export const EditorProvider: React.FC<React.PropsWithChildren<{ isAdmin?: boolean, isEditing?: boolean }>> = (props) => {
	return (
		<editorCtx.Provider
			value={{
				isAdmin: props.isAdmin ?? false,
				isEditing: props.isEditing ?? false
			}}
		>
			{props.children}
		</editorCtx.Provider>
	);
};