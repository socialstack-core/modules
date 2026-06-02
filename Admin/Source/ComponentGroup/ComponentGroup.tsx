import { useEffect, useState } from "react";
import Button from 'UI/Button';
// @ts-ignore
import ModuleSelector from 'Admin/CanvasEditor/ModuleSelector';
// @ts-ignore
import ComponentGroupSelector from 'Admin/CanvasEditor/ModuleSelector/ComponentGroupSelector';

type RuleType = 'wildcard' | 'group' | 'module' | 'prefixWildcard';

type Rule = {
    type: RuleType;
    value: string;
    excluded: boolean;
};

function parseRules(json: string): Rule[] {
    try {
        const rules = JSON.parse(json || "[]");
        if (!Array.isArray(rules)) {
            return [];
        }
        return rules.map((r: string): Rule => {
            const excluded = r.startsWith('!');
            const value = excluded ? r.slice(1) : r;
            let type: RuleType = 'wildcard';
            if (value === '*') {
                type = 'wildcard';
            } else if (value.endsWith('/*')) {
                type = 'prefixWildcard';
            } else if (value.includes('/')) {
                type = 'module';
            } else {
                type = 'group';
            }
            return { type, value, excluded };
        });
    } catch (error) {
        console.error("Failed to parse rules as JSON", error);
        return [];
    }
}

function serializeRules(rules: Rule[]): string {
    return JSON.stringify(rules.map(r => (r.excluded ? '!' : '') + r.value));
}

const Editor: React.FC = (props: any) => {
    const [rules, setRules] = useState<Rule[]>([]);
    const [showSelector, setShowSelector] = useState(false);
    const [showGroupSelector, setShowGroupSelector] = useState(false);
    const [customRuleInput, setCustomRuleInput] = useState('');
	
	const initValue = props.value || props.defaultValue;
	
    useEffect(() => {
        if (initValue !== undefined) {
            setRules(parseRules(initValue));
        }
    }, [initValue]);

    const onComponentAdded = (module: any) => {
        const rule: Rule = { type: 'module', value: module.publicName, excluded: false };
        if (!rules.some(r => r.type === 'module' && r.value === rule.value)) {
            setRules(prev => [...prev, rule]);
        }
        setShowSelector(false);
    };

    const onGroupAdded = (group: any) => {
        const rule: Rule = { type: 'group', value: group.key, excluded: false };
        if (!rules.some(r => r.type === 'group' && r.value === rule.value)) {
            setRules(prev => [...prev, rule]);
        }
        setShowGroupSelector(false);
    };

    const onRuleRemoved = (index: number) => {
        setRules(prev => prev.filter((_, i) => i !== index));
    };

    const toggleExclude = (index: number) => {
        setRules(prev => prev.map((r, i) => i === index ? { ...r, excluded: !r.excluded } : r));
    };

    const onCustomRuleSubmit = () => {
        const value = customRuleInput.trim();
        if (!value) return;

        const excluded = value.startsWith('!');
        const ruleValue = excluded ? value.slice(1) : value;

        let type: RuleType = 'wildcard';
        if (ruleValue === '*') {
            type = 'wildcard';
        } else if (ruleValue.endsWith('/*')) {
            type = 'prefixWildcard';
        } else if (ruleValue.includes('/')) {
            type = 'module';
        } else {
            type = 'group';
        }

        const rule: Rule = { type, value: ruleValue, excluded };

        if (!rules.some(r => r.value === rule.value && r.type === rule.type)) {
            setRules(prev => [...prev, rule]);
        }
        setCustomRuleInput('');
    };

    const getRuleIcon = (type: RuleType) => {
        switch (type) {
            case 'wildcard':
            case 'prefixWildcard':
            case 'group':
                return 'fa-th-large';
            case 'module':
                return 'fa-puzzle-piece';
            default:
                return 'fa-puzzle-piece';
        }
    };

    const getRuleLabel = (rule: Rule) => {
        if (rule.type === 'wildcard') {
            return 'All components';
        }
        if (rule.type === 'prefixWildcard') {
            const prefix = rule.value.slice(0, -2);
            return `All ${prefix}/* components`;
        }
        if (rule.type === 'module') {
            return formatComponentName(rule.value);
        }
        return rule.value;
    };

    function formatComponentName(name: string): string {
        return name.replace(/\.(tsx?|jsx?)$/, '');
    }

    return (
        <div className="component-group-editor component-group-page">
            <div className="mb-3">
                <label htmlFor="form-field-4" className="form-label">{props.label}</label>
                <input type="hidden" value={serializeRules(rules)} name={props.name} />

                <div className="d-flex gap-2 mb-3">
                    <Button onClick={() => setShowSelector(true)}>
                        {`Add component...`}
                    </Button>
                    <Button outlined onClick={() => setShowGroupSelector(true)}>
                        {`Add group...`}
                    </Button>
                </div>

                <div className="d-flex gap-2 mb-3">
                    <input
                        type="text"
                        className="form-control"
                        placeholder="Add wildcard (e.g., Email/*, !*, !Email/*)..."
                        value={customRuleInput}
                        onChange={(e) => setCustomRuleInput(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && onCustomRuleSubmit()}
                    />
                    <Button variant="secondary" outlined onClick={onCustomRuleSubmit}>
                        <i className="fa fa-plus" />
                    </Button>
                </div>

                <div className="component-list">
                    {rules.length === 0 ? (
                        <p className="text-muted">{`No rules have been added.`}</p>
                    ) : (
                        rules.map((rule, index) => (
                            <div
                                key={index}
                                className={`group-item ${rule.excluded ? 'excluded' : 'selected'}`}
                            >
                                <i className={`fa ${getRuleIcon(rule.type)} me-2`} />
                                <span className="me-2">{rule.excluded ? '-' : '+'}</span>
                                <span className={rule.excluded ? 'text-danger' : ''}>
                                    {getRuleLabel(rule)}
                                </span>
                                <Button sm outlined variant="secondary" className="float-end"
                                    onClick={() => toggleExclude(index)}
                                    title={rule.excluded ? 'Include' : 'Exclude'}
                                >
                                    <i className={`fa ${rule.excluded ? 'fa-plus' : 'fa-minus'}`} />
                                </Button>
                                <Button sm outlined variant="secondary" className="float-end me-2"
                                    onClick={() => onRuleRemoved(index)}
                                >
                                    <i className="fa fa-trash" />
                                </Button>
                            </div>
                        ))
                    )}
                </div>
            </div>

            <ModuleSelector
                selectOpenFor={showSelector}
                onClose={() => setShowSelector(false)}
                onSelected={onComponentAdded}
            />

            <ComponentGroupSelector
                selectOpenFor={showGroupSelector}
                onClose={() => setShowGroupSelector(false)}
                onSelected={onGroupAdded}
            />
        </div>
    );
};

export default Editor;
