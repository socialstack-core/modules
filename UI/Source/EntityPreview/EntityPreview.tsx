import Alert from "UI/Alert";
import {Content} from "Api/Content";

/**
 * Available component packs for entities.
 * - 'UI': Default user interface pack.
 * - 'Admin': Admin-specific interface (falls back to UI if not registered).
 * - 'Email': Email-specific rendering (must be explicitly registered).
 */
export type ComponentPack = 'Admin' | 'UI' | 'Email';

/**
 * Internal shape of props each entity renderer must accept.
 * Every renderer must accept a `content` prop containing the entity's data.
 */
export type EntityRendererInnerProps = {
	/** Entity content to render. */
	content: Content<uint>
}

/**
 * Props passed to the `EntityRenderer` wrapper component.
 * Combines the entity name, optional pack selection, optional fallback, and required content.
 */
export type EntityRendererProps = EntityRendererInnerProps & {
	/** Unique name of the entity to render. */
	entityName: string;

	/** Optional component pack to render. Defaults to 'UI'. */
	pack?: ComponentPack;

	/** Optional fallback node to render if no renderer is found. Defaults to an info alert. */
	fallback?: React.ReactNode;
}

/**
 * Internal registry storing entity renderers per pack.
 * Uses Partial<Record<...>> to allow missing packs safely.
 */
const _entityRendererRegistry: Record<string, Partial<Record<ComponentPack, React.FC<EntityRendererProps>>>> = {};

/**
 * Registers a React component as the renderer for a given entity.
 *
 * @param entityName - Unique name of the entity.
 * @param component - React component that renders the entity.
 * @param pack - Component pack to register under. Defaults to 'UI'.
 *
 * Behavior:
 * - If `pack` is 'UI' (default), the component is automatically registered for both 'UI' and 'Admin'.
 * - If `pack` is 'Admin', it is only registered for 'Admin'.
 * - If `pack` is 'Email', it must be explicitly registered and does not affect other packs.
 *
 * Null safety:
 * - The registry is lazily initialized for each entity.
 * - Throws errors for invalid inputs to prevent silent bugs.
 */
export const registerEntityRenderer = (
	entityName: string,
	component: React.FC<EntityRendererInnerProps>,
	pack: ComponentPack = 'UI'
): void => {
	if (!entityName) {
		throw new Error('Entity name must be a non-empty string.');
	}
	if (!component) {
		throw new Error('Component must be a valid React component.');
	}

	if (!_entityRendererRegistry[entityName]) {
		_entityRendererRegistry[entityName] = {};
	}

	switch (pack) {
		case 'UI':
			_entityRendererRegistry[entityName]['UI'] = component;
			_entityRendererRegistry[entityName]['Admin'] = component;
			break;
		case 'Admin':
		case 'Email':
			_entityRendererRegistry[entityName][pack] = component;
			break;
		default:
			throw new Error(`Unknown component pack: ${pack}`);
	}
};

/**
 * Retrieves a registered entity renderer for a given entity and pack.
 *
 * @param entityName - Unique name of the entity.
 * @param pack - Desired component pack. Defaults to 'UI'.
 * @returns The registered React component, or undefined if not found.
 *
 * Fallback behavior:
 * - If the requested pack is 'Admin' or 'UI' and not registered, 'UI' is used as a fallback.
 * - Email pack never falls back to UI.
 *
 * Null safety:
 * - Returns undefined if the entity or pack does not exist.
 */
export const getEntityRenderer = (
	entityName: string,
	pack: ComponentPack = 'UI'
): React.FC<EntityRendererProps> | undefined => {
	if (!entityName) return undefined;

	const entityEntry = _entityRendererRegistry[entityName];
	if (!entityEntry) return undefined;

	// Fallback: Admin falls back to UI if not present
	if (pack === 'Admin' || pack === 'UI') {
		return entityEntry[pack] ?? entityEntry['UI'] ?? undefined;
	}

	// Email pack has no fallback
	return entityEntry[pack] ?? undefined;
};

/**
 * A wrapper component that dynamically renders an entity using its registered renderer.
 *
 * Props:
 * - `entityName`: Required. Name of the entity to render.
 * - `pack`: Optional. Component pack to render. Defaults to 'UI'.
 * - `content`: Required. The content of the entity to render.
 * - `fallback`: Optional. Custom React node to display if no renderer is found.
 *
 * Behavior:
 * - Looks up the entity renderer via `getEntityRenderer`.
 * - If found, renders the component with the provided content.
 * - If not found:
 *   - Renders the `fallback` prop if provided.
 *   - Otherwise, renders an `<Alert>` with an informational message.
 */
const EntityRenderer: React.FC<EntityRendererProps> = (props: EntityRendererProps) => {
	const Renderer = getEntityRenderer(props.entityName, props.pack);

	if (!Renderer) {
		if (!props.fallback) {
			return <Alert variant={'info'}>{`No fallback renderer for the entity`}</Alert>;
		}
		return <>{props.fallback}</>;
	}

	// Render the entity component
	return <Renderer content={props.content} entityName={props.entityName} pack={props.pack} fallback={props.fallback} />;
};

export default EntityRenderer;
