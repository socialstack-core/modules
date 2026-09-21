import { useRef, useEffect } from 'react';

/**
 * recommended usage:
 * 
  
 import { useRouter } from 'UI/Router';
 const { pageState, updateQuery } = useRouter();
 
 
 * define tabs via an enum:
  
 	enum UserTab {
		Details = `Details`,
		Permissions = `Permissions`,
		Actions = `Actions`
	}


* define a var to hold the current tab (optionally store and retrieve this as a query param to better support sharing)
	const { query } = pageState;
	const currentTab = query?.get("tab") || UserTab.Details.toLowerCase();


* define a method to update the URL without refreshing the page each time a tab is selected:

	const setCurrentTab = (target: string) => {
		updateQuery({
			tab: target
		});
	};


 * define a render function for each tab:

 const renderDetails = () => { ... }
 const renderPermissions = () => { ... }
 const renderActions = () => { ... }

 const renderTab = (tab: string) => {

   switch (tab) {
     case UserTab.Details:
		 return renderDetails();

     case UserTab.Permissions:
		 return renderPermissions();

     case UserTab.Actions:
		 return renderActions();

     default:
		 return;
   }

 }


 * reference via props on the component:

 <Tabs currentTab={currentTab} tabs={Object.values(UserTab)} renderPanel={renderTab} onChange={(tab) => setCurrentTab(tab.toLowerCase())} />

 */

/**
 * Props for the Tabs component.
 */
interface TabsProps {
	tabs: string[] | object[],
	renderLink?: Function,
	renderPanel: Function,
	currentTab?: string | object,
	onChange?: (tab: string | object) => void,
	name?: string
}

/**
 * The Tabs React component.
 * @param props React props.
 */
const Tabs: React.FC<TabsProps> = (props) => {
	const { tabs, renderLink, renderPanel, onChange } = props;
	const name = !!props.name?.length ? props.name : "tabs";

	if (!tabs?.length || !renderPanel) {
		return;
	}

	const isStringArray = (value: unknown): value is string[] => {
		return Array.isArray(value) && value.every(item => typeof item === 'string');
	};

	const getTabKey = (tab: string | object | undefined) => {

		if (!tab) {
			return undefined;
		}

		if (typeof tab === 'string') {
			return tab.toLowerCase();
		}

		const idx = tabs.findIndex(t => t === tab);

		return idx ? `${name}_${idx}` : undefined;
	};

	const currentTab = props.currentTab || getTabKey(!!tabs?.length ? tabs[0] : undefined);

	const renderLinkInternal = (tab: string | object) => {

		if (renderLink instanceof Function) {
			return renderLink(tab);
		}

		return tab.toString();
	};

	return (
		<TabsWrapper>

			{/* tab links - rendered as radio buttons to allow functionality without reliance on JavaScript */}
			<TabsLinksWrapper>
				{tabs.map((tab, i) => {
					const currentKey = getTabKey(currentTab);
					const tabKey = getTabKey(tab);
					const linkId = `${name}-link${i + 1}`;
					const panelId = `${name}-panel${i + 1}`;
					const selected = currentKey === tabKey;

					return (
						<TabsLinkWrapper key={linkId}>
							<input
								data-tab={tabKey} data-current={currentKey}
								type="radio"
								name={name}
								id={linkId}
								aria-controls={panelId}
								defaultChecked={selected}
								onClick={() => {
									if (onChange) {
										onChange(tab);
									}
								}}
							/>
							<label htmlFor={linkId}>
								{renderLinkInternal(tab)}
							</label>
						</TabsLinkWrapper>
					);
				})}
			</TabsLinksWrapper>

			{/* tab panels */}
			<TabsPanelsWrapper>
				{tabs.map((tab, i) => {
					const currentKey = getTabKey(currentTab);
					const tabKey = getTabKey(tab);
					const panelId = `${name}-panel${i + 1}`;
					const selected = currentKey === tabKey;
					
					return (
						<TabsPanelWrapper id={panelId} key={panelId} selected={selected}>
							{renderPanel(tab)}
						</TabsPanelWrapper>
					);
				})}
			</TabsPanelsWrapper>

		</TabsWrapper>
	);
}

interface TabsWrapperProps {
	children?: React.ReactNode,
	fullWidth?: boolean,
	sticky?: boolean,
}

export function TabsWrapper({ children, fullWidth, sticky }: TabsWrapperProps): JSX.Element {
	var tabClasses = ['ui-page__tabs'];

	if (fullWidth) {
		tabClasses.push('ui-page__tabs--full-width');
	}

	if (sticky) {
		tabClasses.push('ui-page__tabs--sticky');
	}

	return (
		<section className={tabClasses.join(' ')}>
			{children}
		</section>
	);
}

interface TabsLinksWrapperProps {
	children?: React.ReactNode;
}

export function TabsLinksWrapper({ children }: TabsLinksWrapperProps): JSX.Element {
	return (
		<div className="ui-page__tab-links">
			{children}
		</div>
	);
}

interface TabsLinkWrapperProps {
	children?: React.ReactNode;
}

export function TabsLinkWrapper({ children }: TabsLinkWrapperProps): JSX.Element {
	return (
		<div className="ui-page__tab-link">
			{children}
		</div>
	);
}

interface TabsPanelsWrapperProps {
	children?: React.ReactNode;
}

export function TabsPanelsWrapper({ children }: TabsPanelsWrapperProps): JSX.Element {
	return (
		<div className="ui-page__tab-panels">
			{children}
		</div>
	);
}

interface TabsPanelWrapperProps {
	id?: string,
	children?: React.ReactNode;
	selected?: boolean;
}

export function TabsPanelWrapper({ id, children, selected }: TabsPanelWrapperProps): JSX.Element {
	const tabRef = useRef(null);

	useEffect(() => {
		const tab = tabRef.current;

		if (!tab) {
			return;
		}

		// switching to a tab hosting a TinyMCE instance which was previously inactive
		// requires a nudge to ensure sticky headers (menu / toolbar) are sized appropriately
		const tabActiveHandler = (e) => {
			requestAnimationFrame(() => {
				window.dispatchEvent(new Event('resize'));
				window.tinymce?.activeEditor?.fire("ResizeEditor");
			});
		};

		tab.addEventListener('tab-active', tabActiveHandler);

		return () => {
			tab.removeEventListener('tab-active', tabActiveHandler);
		};
	}, []);

	return (
		<div className={"ui-page__tab-panel" + (selected ? ' selected' : '')} id={id} ref={tabRef}>
			{children}
		</div>
	);
}

export default Tabs;