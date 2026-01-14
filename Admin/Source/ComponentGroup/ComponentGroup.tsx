import { CodeModuleMeta, getAll, TypeMeta } from "Admin/Functions/GetPropTypes";
import { useEffect, useState } from "react";

const Editor: React.FC = (props: any) => {
    // Holds all available components that pass filter criteria
    const [components, setComponents] = useState<string[] | null>(null);

    // Tracks currently enabled components
    const [enabled, setEnabled] = useState<string[]>([]);

    // Tracks the filter query
    const [filter, setFilter] = useState<string>("");

    // Initialize `enabled` from `props.value` on mount
    useEffect(() => {
        if (props.currentContent?.allowedComponents) {
            try {
                const parsed = JSON.parse(props.currentContent?.allowedComponents);
                if (Array.isArray(parsed)) {
                    setEnabled(parsed);
                }
            } catch (error) {
                console.error("Failed to parse `props.value` as JSON", error);
            }
        }
    }, [props.currentContent?.allowedComponents]);

    // Load available components only once
    useEffect(() => {
        if (!components) {
            getAll().then((results: TypeMeta) => {
                const { codeModules } = results;

                const filtered = Object.keys(codeModules).filter((mod) => {
                    return !(
                        mod.startsWith("Admin/") ||
                        mod.startsWith("Api/") ||
                        mod.startsWith("UI/Templates") ||
                        mod.startsWith("UI/Functions") ||
                        mod.startsWith("Email/Templates")
                    );
                });

                setComponents(filtered);
            });
        }
    }, [components]);

    /**
     * Toggles the presence of a component in the enabled list
     */
    const onComponentSelected = (componentName: string) => {
        setEnabled((prev) =>
            prev.includes(componentName)
                ? prev.filter((c) => c !== componentName)
                : [...prev, componentName]
        );
    };

    /**
     * Determines whether a component should be visible based on filter
     */
    const isComponentVisible = (componentName: string): boolean => {
        return !filter || componentName.toLowerCase().includes(filter.toLowerCase());
    };

    return (
        <div className="component-group-editor component-group-page">
            <div className="mb-3">
                <label htmlFor="form-field-4" className="form-label">{props.label}</label>
                <input type="hidden" value={JSON.stringify(enabled)} name={props.name} />

                <input
                    type="text"
                    onChange={(ev) => setFilter(ev.target.value)}
                    placeholder="Filter components"
                    value={filter}
                    id="filter-components"
                    className="form-control"
                />

                <div className="component-list">
                    {components?.filter(isComponentVisible).map((componentName) => (
                        <div
                            key={componentName}
                            title={componentName}
                            className={`group-item ${enabled.includes(componentName) ? "selected" : ""}`}
                            onClick={() => onComponentSelected(componentName)}
                        >
                            <i className="fa fa-puzzle-piece" />
                            <label>{componentName}</label>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
};

export default Editor;
