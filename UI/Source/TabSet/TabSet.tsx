import { cloneElement, useEffect, useId, ReactElement, isValidElement, ReactNode, Children } from 'react';

const COMPONENT_PREFIX = 'ui-tabset';

interface TabSetProps {
	name: string;
	id?: string;
	links?: boolean;
	className?: string;
	selectedIndex?: number;
	onChange?: (index: number) => void;
	children?: ReactNode;
}

type MixedElement = HTMLElement & {
	index?: number,
	panel?: boolean,
	name?: string,
	selectedIndex?: number,
	onChange?: (index: number) => void;
}

const TabSet: React.FC<React.PropsWithChildren<TabSetProps>> = (props: TabSetProps) => {
	
	const { name, id, links, className, selectedIndex, onChange, children } = props;
	
	const generatedId = useId();
	const _id = id || generatedId;

	const componentClasses = [COMPONENT_PREFIX];
	
	if (links) componentClasses.push(`${COMPONENT_PREFIX}--links`);
	if (className) componentClasses.push(className);

	return (
		<section id={_id} className={componentClasses.join(' ')}>
			{/* Render tab headers */}
			{Children.map(children, (child, index) => {
				if (isValidElement(child)) {
					return cloneElement(child as ReactElement<MixedElement>, {
						name,
						id: _id,
						index,
						selectedIndex,
						onChange
					});
				}
				return null;
			})}

			{/* Render tab panels */}
			<div className={`${COMPONENT_PREFIX}__panels`}>
				{Children.map(children, (child, index) => {
					if (isValidElement(child)) {
						return cloneElement(child as ReactElement<MixedElement>, {
							id: _id as string,
							index,
							panel: true
						});
					}
					return null;
				})}
			</div>
		</section>
	);
};

export default TabSet;
