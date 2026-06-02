import componentGroupApi from 'Api/ComponentGroup';

type RuleType = 'module' | 'group' | 'wildcard' | 'prefixWildcard';

type Rule = {
	type: RuleType;
	value: string;
	excluded: boolean;
};

type ComponentGroupCache = Record<string, Rule[]>;

var componentGroupCache: ComponentGroupCache | null = null;

function parseRule(rule: string): Rule {
	const excluded = rule.startsWith('!');
	const value = excluded ? rule.slice(1) : rule;
	const type: RuleType = value === '*' ? 'wildcard'
		: value.endsWith('/*') ? 'prefixWildcard'
		: value.includes('/') ? 'module'
		: 'group';
	return { type, value, excluded };
}

function parseRules(json: string): Rule[] {
	try {
		const rules = JSON.parse(json || "[]");
		return (Array.isArray(rules) ? rules : []).map((r: string) => parseRule(r));
	} catch {
		return [];
	}
}

function matchRule(rule: Rule, moduleName: string, cache: ComponentGroupCache): boolean {
	switch (rule.type) {
		case 'wildcard':
			return !rule.excluded;
		case 'module':
			return (moduleName === rule.value) !== rule.excluded;
		case 'prefixWildcard': {
			const prefix = rule.value.slice(0, -2);
			return moduleName.startsWith(prefix + '/') !== rule.excluded;
		}
		case 'group': {
			const groupRules = cache[rule.value];
			if (!groupRules) {
				console.warn(`ComponentGroups: Group '${rule.value}' not found`);
				return !rule.excluded;
			}
			return matchRules(groupRules, moduleName, cache) !== rule.excluded;
		}
	}
}

function matchRules(rules: Rule[], moduleName: string, cache: ComponentGroupCache): boolean {
	var result = false;
	for (const rule of rules) {
		if (matchRule(rule, moduleName, cache)) {
			result = true;
		} else if (rule.excluded) {
			result = false;
		}
	}
	return result;
}

async function loadComponentGroups(): Promise<void> {
	const result = await componentGroupApi.listAll();

	componentGroupCache = {};

	for (const group of result.results || []) {
		if (group.key) {
			componentGroupCache[group.key] = parseRules(group.allowedComponents || '');
		}
	}
}

export async function getComponentFilter(rules: string[] | null): Promise<((moduleName: string) => boolean) | null> {
	if (!rules || rules.length === 0) {
		return null;
	}

	if (!componentGroupCache) {
		await loadComponentGroups();
	}

	const parsedRules = rules.map(parseRule);

	return (moduleName: string): boolean => {
		return matchRules(parsedRules, moduleName, componentGroupCache!);
	};
}