import { expand, CanvasNode } from 'UI/Functions/CanvasExpand';
import Alert from 'UI/Alert';
import { useRouter, PageState } from 'UI/Router/RouterCtx';
import { useSession } from 'UI/Session';
import { useErrorBoundary, useEffect, useState, useMemo } from 'react'; // useErrorBoundary is a preact function.
import { resolveSingular } from 'UI/Token/TokenResolver';

var uniqueKey = 1;

const loadJson = (bodyJson?: any, jsonString?: string, onContentNode?: (node: CanvasNode) => void) => {
	var content;
	
	if (bodyJson) {
		// If using pre parsed json, __key is recommended.
		content = bodyJson;
	} else if (jsonString) {
		try {
			content = JSON.parse(jsonString);
		} catch (e) {
			console.log("Canvas failed to load JSON: ", jsonString);
			console.error(e);
		}
	}

	if (content) {
		content = expand(content, onContentNode);
	}

	return content;
}

interface CanvasProps {
	/**
	 * Optional pre-parsed JSON.
	 */
	bodyJson?: any,

	/**
	 * Optional canvas content as a JSON string.
	 */
	children?: string,

	/**
	 * Optional callback which runs when a node is expanded.
	 * @param node
	 * @returns
	 */
	onContentNode?: (node : CanvasNode) => void,
	
	/**
	 * Optional callback which runs whenever a node is rendered. 
	 * It can return a modified version of the node if it wants. Undefined uses the original, null renders nothing.
	 * @param node
	 * @returns
	 */
	onRenderNode?: (node : CanvasNode) => CanvasNode | undefined | null
}

interface CanvasDataStore {
	content: any,
	fields: Record<string, any>
}

/**
 * This component renders canvas JSON. It takes canvas JSON as its child.
 */
const Canvas: React.FC<CanvasProps> = (props) => {
	const { onRenderNode } = props;
	// Stores general use data fields.
	const [canvasDataStoreOverride, setCanvasDataStoreOverride] = useState<CanvasDataStore | null>(null);
	const content = useMemo(() => loadJson(props.bodyJson, props.children, props.onContentNode), [props.bodyJson, props.children, props.onContentNode]);

	const canvasDataStore = canvasDataStoreOverride && canvasDataStoreOverride.content == content ? canvasDataStoreOverride : { fields: content?.dataStore || {} };

	var { pageState } = useRouter();
	var { session } = useSession();

	const setDataStoreField = (name: string, value: any) => {
		const newData = { ...canvasDataStore, content };
		newData.fields[name] = value;
		setCanvasDataStoreOverride(newData);
	};

	const getDataStoreField = (name: string): any => {
		if (name?.indexOf('.')) {
			// Token-style link
			return resolveSingular(name, session, canvasDataStore, pageState);
		}

		return canvasDataStore.fields[name];
	};

	const renderNodeSet = (set: CanvasNode[]) => {
		return set.map((n, i) => {
			if (n && !n.__key) {
				if (n.id) {
					n.__key = "_canvas:id_" + n.id;
				} else {
					n.__key = "_canvas_" + uniqueKey;
				}
				uniqueKey++;
			}
			return renderNode(n);
		});
	};

	const renderNode = (node: CanvasNode): React.ReactNode => {
		if (!node) {
			return null;
		}
		
		if(onRenderNode){
			const newNode = onRenderNode(node);
			if(newNode === null){
				return null;
			}else if(newNode){
				node = newNode;
			}
		}
		
		if (node.type == '#text') {
			return node.text;
		} else if (typeof node.type === 'string') {
			var childContent = null;

			if (node.content && node.content.length) {
				childContent = renderNodeSet(node.content);
			} else if (!node.isInline && node.type != 'br') {
				// Fake a <br> such that block elements still have some sort of height.
				//childContent = renderNode({type:'br', props: {'rte-fake': 1}});
				if (!node.props) {
					node.props = {};
				}
				var className = node.props.className ? node.props.className : "";
				if (!(className && className.length && className.includes("empty-canvas-node"))) {
					className += " empty-canvas-node";
				}
				node.props.className = className;
			}

			const NodeType = node.type as React.ElementType;

			return <NodeType key={node.__key} {...node.props}>{childContent}</NodeType>;
		} else if (node.type) {
			// Custom component
			var props = { ...node.props };
			const NodeType = node.type as React.ElementType;

			if (node.links) {
				for (var k in node.links) {
					var link = node.links[k];

					if (link.primary) {
						props[k] = pageState.po;
					} else {
						props[k] = link.write ? (val: any) => setDataStoreField(link.field, val) : getDataStoreField(link.field);
					}
				}
			}

			if (node.roots) {
				var children = null;

				for (var k in node.roots) {
					var root = node.roots[k];

					var isChildren = k == 'children';

					var rendered = root.content && root.content.length ? renderNodeSet(root.content) : null;

					if (isChildren) {
						children = rendered;
					} else {
						props[k] = rendered;
					}
				}

				return <ErrorCatcher node={node}>
					<NodeType key={node.__key} {...props}>{children}</NodeType>
				</ErrorCatcher>;
			} else {
				// It has no content inside it; it's purely config driven.
				// Either wrap it in a span (such that it only has exactly 1 DOM node, always), unless the module tells us it has one node anyway:
				return <ErrorCatcher node={node}>
					<NodeType key={node.__key} {...props} />
				</ErrorCatcher>;
			}
		} else if (node.content && node.content.length) {
			return renderNodeSet(node.content);
		}

		return null;
	};
	
	// Otherwise, render the (preprocessed) child nodes.
	if(!content){
		return null;
	}
	
	return renderNode(content);
}

export default Canvas;

interface ErrorCatcherProps {

	node: CanvasNode

}

const ErrorCatcher: React.FC<React.PropsWithChildren<ErrorCatcherProps>> = (({ node, children }) => {
	const [error] = useErrorBoundary((e : any) => console.warn(e));

	if (error) {
		var name = node ? node.typeName : `Unknown`;

		return <Alert variant='danger'>
			{`The component "${name}" crashed.`}
			<details>
				<summary>{`Error details`}</summary>
				{
					error.message
				}
			</details>
		</Alert>;
	}

	return children;
});
