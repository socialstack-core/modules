// ========================
// UI Imports
// ========================
import Button from "UI/Button";
import Container from "UI/Container";

// ========================
// React Imports
// ========================
import {useEffect, useState} from "react";

// ========================
// Types
// ========================
export type TabItem = {
    label: string;
    content: React.ReactNode;
};

export type TabsProps = {
    tabs: TabItem[];
    currentTab?: string | null;
    onTabChange?: (tab: string) => void;
};

/**
 * Tabs Component
 *
 * Renders a set of tab buttons with associated content panels.
 *
 * Features:
 * - Switches active tab on button click.
 * - Highlights the active tab.
 * - Displays the corresponding tab content.
 *
 * Usage:
 * ```
 * <Tabs
 *     tabs={[
 *         { label: "Tab 1", content: <div>Content 1</div> },
 *         { label: "Tab 2", content: <div>Content 2</div> }
 *     ]}
 * />
 * ```
 */
const Tabs: React.FC<TabsProps> = ({ tabs, currentTab, onTabChange }) => {

    if (!currentTab && tabs?.length) {
        currentTab = tabs[0].label;
    }

    // ========================
    // Render
    // ========================
    return (
        <div className="admin-tabs">
            {/* Tab controls */}
            <div className="control">
                <Container>
                    {tabs.map((item) => (
                        <Button
                            key={item.label}
                            className={item.label === currentTab ? "active" : ""}
                            onClick={() => onTabChange && onTabChange(item.label)}
                        >
                            {item.label}
                        </Button>
                    ))}
                </Container>
            </div>

            {/* Tab content */}
            <div className="panels">
                <Container>
                    {tabs.map((tab) => {
						
						return (
							<div className={'tab-pane' + (tab.label === currentTab ? " active" : "")}>
								{tab.content}
							</div>
						)
					})}
                </Container>
            </div>
        </div>
    );
};

export default Tabs;
