import { useEffect, useState } from "react";
import PlacementEditor from './PlacementEditor';
import { Placement, PlacementType, PlacementTypeLabels, PlacementTypeDescriptions } from './types';
import Dialog from 'UI/Dialog';
import Button from 'UI/Button';

function extractId(value: number | object): number {
    if (typeof value === 'number') {
        return value;
    }
    return (value as any).id;
}

function serializePlacements(placements: Placement[]): string {
    const serialized = placements.map(p => ({
        type: p.type,
        conditions: {
            ...p.conditions,
            pages: p.conditions.pages?.map(extractId),
            searchCategories: p.conditions.searchCategories?.map(extractId),
            categories: p.conditions.categories?.map(extractId),
            products: p.conditions.products?.map(extractId),
            minPrice: p.conditions.minPrice ?? null,
            maxPrice: p.conditions.maxPrice ?? null
        }
    }));
    return JSON.stringify(serialized);
}

const Editor: React.FC = (props: any) => {
    const [placements, setPlacements] = useState<Placement[]>([]);
    const [showAddMenu, setShowAddMenu] = useState(false);

    const initValue = props.value || props.defaultValue;

    useEffect(() => {
        if (initValue !== undefined) {
            try {
                const parsed = JSON.parse(initValue);
                const loadedPlacements = (parsed || []).map((p: any): Placement => ({
                    type: p.type || 'header',
                    conditions: p.conditions || {}
                }));
                setPlacements(loadedPlacements);
            } catch (error) {
                console.error("Failed to parse placements", error);
            }
        }
    }, [initValue]);

    const addPlacement = (type: PlacementType) => {
        const newPlacement: Placement = {
            type,
            conditions: type === 'category' ? { includeChildren: true } : {}
        };
        setPlacements(prev => [...prev, newPlacement]);
        setShowAddMenu(false);
    };

    const updatePlacement = (index: number, updates: Partial<Placement>) => {
        setPlacements(prev => prev.map((p, i) => 
            i === index ? { ...p, ...updates } : p
        ));
    };

    const removePlacement = (index: number) => {
        setPlacements(prev => prev.filter((_, i) => i !== index));
    };

    const getPlacementSummary = (placement: Placement): string => {
        const conditions = placement.conditions;
        const plural = (count: number, singular: string, plural: string) => 
            count === 1 ? `${count} ${singular}` : `${count} ${plural}`;

        switch (placement.type) {
            case 'header':
                if (conditions.pages && conditions.pages.length > 0) {
                    return plural(conditions.pages.length, 'page', 'pages');
                }
                return 'All pages';
            case 'search':
                if (conditions.searchCategories && conditions.searchCategories.length > 0) {
                    return plural(conditions.searchCategories.length, 'category filter', 'category filters');
                }
                return 'All searches';
            case 'category':
                if (conditions.categories && conditions.categories.length > 0) {
                    const childText = conditions.includeChildren !== false ? ' (incl. children)' : '';
                    return plural(conditions.categories.length, 'category', 'categories') + childText;
                }
                return 'All categories';
            case 'product':
                const parts: string[] = [];
                if (conditions.products && conditions.products.length > 0) {
                    parts.push(plural(conditions.products.length, 'product', 'products'));
                }
                if (conditions.categories && conditions.categories.length > 0) {
                    parts.push(plural(conditions.categories.length, 'category filter', 'category filters'));
                }
                if (conditions.minPrice != null || conditions.maxPrice != null) {
                    const min = conditions.minPrice != null ? (conditions.minPrice / 100).toFixed(2) : '0';
                    const max = conditions.maxPrice != null ? (conditions.maxPrice / 100).toFixed(2) : '∞';
                    parts.push(`£${min} - £${max}`);
                }
                return parts.length > 0 ? parts.join(', ') : 'All products';
            case 'dashboard':
                return 'Dashboard';
            default:
                return '';
        }
    };

    const placementTypes: PlacementType[] = ['header', 'search', 'category', 'product', 'dashboard'];

    return (
        <div className="promotion-placement-editor">
            <div className="mb-3">
                <label className="form-label fw-bold">{`Where should this promotion appear?`}</label>
                <input 
                    type="hidden" 
                    name={props.name} 
                    value={serializePlacements(placements)}
                />
            </div>

            <div className="placement-list">
                {placements.length === 0 ? (
                    <p className="text-muted">{`No placements configured. Add one below to define where this promotion should appear.`}</p>
                ) : (
                    placements.map((placement, index) => (
                        <div key={index} className="placement-wrapper">
                            <div className="placement-summary">
                                <i className="fa fa-arrows me-2" />
                                <span>{getPlacementSummary(placement)}</span>
                            </div>
                            <PlacementEditor
                                placement={placement}
                                onUpdate={(updates) => updatePlacement(index, updates)}
                                onRemove={() => removePlacement(index)}
                            />
                        </div>
                    ))
                )}
            </div>

            <div className="add-placement-section mt-3">
                <Button onClick={() => setShowAddMenu(true)}>
                    {`Add placement...`}
                </Button>

                <Dialog
                    title={`Add Placement`}
                    isOpen={showAddMenu}
                    onClose={() => setShowAddMenu(false)}
                >
                    <div className="placement-type-options">
                        {placementTypes.map(type => (
                            <Button
                                key={type}
                                className="placement-type-option"
                                onClick={() => addPlacement(type)}
                            >
                                <div className="fw-bold">{PlacementTypeLabels[type]}</div>
                                <div className="small text-muted">{PlacementTypeDescriptions[type]}</div>
                            </Button>
                        ))}
                    </div>
                </Dialog>
            </div>
        </div>
    );
};

export default Editor;
