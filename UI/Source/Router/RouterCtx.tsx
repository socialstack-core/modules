import { PageStateResult } from 'Api/Pages';
import { createContext, useContext } from 'react';

export interface PageState extends PageStateResult {
	url: string;
	query: URLSearchParams;
}

export interface RouterContext {
	setPage: (url: string) => void;
	changeQuery: (query: URLSearchParams) => void;
	updateQuery: (query: Record<string, string | number | boolean | (string | number | boolean)[] | null | undefined>) => void;
	removeQueryItems: (items: string[]) => void;
	pageState: PageState;
	canGoBack: () => boolean;
	getPageIncludes: () => string | undefined;
	setPrimaryObject: (obj: any) => void;
}

const routerCtx = createContext<RouterContext>({
	setPage: (url: string) => { },
	canGoBack: () => false,
	pageState: {},
	getPageIncludes: () => undefined,
	changeQuery: (query: URLSearchParams) => {},
	updateQuery: (query: Record<string, string | number | boolean | (string | number | boolean)[] | null | undefined>) => {},
	removeQueryItems: (items: string[]) => {},
	setPrimaryObject: (obj: any) => {}
} as RouterContext);

export { routerCtx };

export function useRouter() {
	// returns {page, setPage}
	return useContext(routerCtx);
};
