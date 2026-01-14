import { CodeModuleMeta, isJsx } from 'Admin/Functions/GetPropTypes';
import { createLinkDecorator } from 'Admin/CanvasEditor/Link';
import { CanvasNode } from "UI/Functions/CanvasExpand";
import Draft from 'Admin/CanvasEditor/DraftJs/Draft.min.js';
const { EditorState } = Draft; // draftjs

type RootProp = {
	name: string
};

/**
 * Creates an empty root node.
 * @returns
 */
export function createEmptyRoot() : CanvasNode {
	var decorator = createLinkDecorator();
	var rootObj = { content: [] } as CanvasNode;
	var emptyPara = {
		type: 'richtext',
		editorState: EditorState.createEmpty(decorator),
		parent: rootObj
	} as CanvasNode;
	rootObj.content.push(emptyPara);
	return rootObj;
}

/**
 * Gets the list of props which are roots from the given prop type info.
 * @param type
 * @returns
 */
export function getRootInfo(type: CodeModuleMeta): RootProp[] {

	if (!type || !type.propTypes)
	{
		return [];
	}
	var {propTypes} = type;
	
	if(!propTypes){
		return [];
	}
	
	var rootInfo = [];
	
	for(var name in propTypes){
		var info = propTypes[name];

		if (isJsx(info)) {
			rootInfo.push({ name } as RootProp);
		}
	}
	
	return rootInfo;
}