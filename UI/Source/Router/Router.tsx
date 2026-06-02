import pageApi from 'Api/Page';
import { PageStateResult } from 'Api/Pages';
import { Content } from 'Api/Database';
import Canvas from 'UI/Canvas';
import { ContentChangeDetail } from 'UI/Functions/ContentChange';
import { expandIncludes } from 'UI/Functions/WebRequest';
import getBuildDate from 'UI/Functions/GetBuildDate';
import AdminTrigger from 'UI/AdminTrigger';
import { useRef, useState, useEffect } from 'react';
import { useRouter, routerCtx, RouterContext, PageState } from 'UI/Router/RouterCtx';

export { useRouter, routerCtx, RouterContext, PageState };

var pgRouterConfig: any = __cfg.pageRouter;
var hashMode = pgRouterConfig?.hash;

function currentUrl() {
	return hashMode ? location.hash?.substring(1) ?? "/" : `${location.pathname}${location.search}`;
}

function triggerEvent(pgInfo : PageStateResult) {
	if(pgInfo){
		var e = new CustomEvent('xpagechange', {
			detail: pgInfo
		});
		document.dispatchEvent && document.dispatchEvent(e);
	}
}

function historyLength(){
	return window && window.history ? window.history.length : 0;
}

var initLength = historyLength();

function canGoBack(){
	return historyLength() > initLength;
}

interface ScrollTarget {
	x: number,
	y: number
}

const Router: React.FC<{}> = () => {
	var [pageState, setPage] = useState<PageState>(() => {
		const initialUrl = currentUrl();

		// Initial event:

		var pgStateHold = document.getElementById('pgState');
		const initState = pgStateHold ? JSON.parse(pgStateHold.innerHTML) : {};

		if (initState.po) {
			initState.po = expandIncludes(initState.po);
		}

		triggerEvent(initState.page);

		return { url: initialUrl, ...initState, query: new URLSearchParams(location.search) };
	});

	var [scrollTarget, setScrollTarget] = useState<ScrollTarget | null>(null);
	const scrollTimer = useRef<number | null>(null);
	
	function go(url : string) {
		if(window.beforePageLoad){
			window.beforePageLoad(url).then(() => {
				window.beforePageLoad = null;
				goNow(url);
			}).catch(e => console.log(e));
		}else{
			goNow(url);
		}
	}

	const internalSetQueryOnly = (url: string) => {
		const parts = url.split('?');
		const query = new URLSearchParams((parts.length > 1) ? parts[1] : '');
		var pgState = { ...pageState, url, query };
		setPage(pgState);
	}
	
	function urlChangeMode(url: string) {
		var current = pageState.url;

		if (url == current) {
			// Unchanged.
			return 0;
		}

		// Only QS?
		var targetPagePart = url?.split('?')[0];
		var currentPagePart = current?.split('?')[0];

		if (targetPagePart != currentPagePart) {
			// Main page nav
			return 1;
		}

		// QS only change
		return 2;
	}

	function goNow(url: string) {
		if (defaultNav(hashMode ? '' : document.location.pathname, url)){
			document.location = url;
			return;
		}

		var changeMode = urlChangeMode(url);

		if (changeMode == 0) {
			// Noop
			return;
		}

		if (changeMode == 2) {
			// QS only - push and update the query only.
			window.history.pushState({
				scrollTop: 0
			}, '', hashMode ? '#' + url : url);

			internalSetQueryOnly(url);
			return;
		}

		// Store the scroll position:
		var html = document.body.parentNode as HTMLHtmlElement;
		window.history.replaceState({
			scrollTop: html.scrollTop,
			scrollLeft: html.scrollLeft
		}, '');
		
		// Push nav event:
		window.history.pushState({
			scrollTop: 0
		}, '', hashMode ? '#' + url : url);
		
		return setPageState(url).then(() => {
			html.scrollTo({top: 0, left: 0, behavior: 'instant'});
		});
	}
	
	function defaultNav(a : string,b : string){
		if(b.indexOf(':') != -1 || b[0]=='#' || (b[0] == '/' && (b.length>1 && b[1] == '/'))){
			return true;
		}
		
		var isOnExternPage = a.indexOf('/en-admin') == 0 || a.indexOf('/v1') == 0;
		var targetIsExternPage = b[0] == '/' ? (b.indexOf('/en-admin') == 0 || b.indexOf('/v1') == 0) : isOnExternPage;
		
		return isOnExternPage != targetIsExternPage;
	}
	 
	function setPageState(url: string) {

		return pageApi.pageState({
			url,
			version: getBuildDate().timestamp
		}).then(res => {
			if (res.oldVersion) {
				console.log("UI updated - forced reload");
				document.location = url;
				return;
			} else if (res.redirect) {
				// Bounce:
				console.log("Redirect");
				document.location = res.redirect;
				return;
			}
			
			var {config} = res;
			
			if(config){
				window.__cfg = config;
			}

			if (res.po) {
				(res as any).po = expandIncludes(res.po);
			}

			var pgState = { url, ...res, query: new URLSearchParams(location.search) };
			setPage(pgState);
			triggerEvent(res);
		});
	}

	const onPopState = (e : PopStateEvent) => {
		var newScrollTarget : ScrollTarget | null = null;
		
		if(e && e.state && e.state.scrollTop !== undefined){
			newScrollTarget = {
				x: e.state.scrollLeft,
				y: e.state.scrollTop
			};
		}

		const url = currentUrl();
		const changeMode = urlChangeMode(url);

		if (changeMode == 0) {
			// No change
			return;
		} else if (changeMode == 2) {
			// QS only
			internalSetQueryOnly(url);
			return;
		}

		setPageState(url).then(() => {
			setScrollTarget(newScrollTarget);
		});
	}
	
    const onLinkClick = (e : MouseEvent) => {
        if (e.button != 0 || e.defaultPrevented) {
            // Browser default action for right/ middle clicks
            return;
        }
        var cur = e.target as Node;
        while (cur && cur !== document) {
			if (cur.nodeName === 'A') {
				var ele = cur as HTMLAnchorElement;
				var href = ele.getAttribute('href'); // cur.href is absolute
				if (ele.getAttribute('target') || ele.getAttribute('download')) {
                    return;
                }

                if (href && href.length) {
                    if (href.includes('#')) {
                        const [pathname, hash] = href.split('#');
                        if (pathname === '' || pathname === window.location.pathname) {
                            e.preventDefault();
                            const targetElement = document.getElementById(hash);
                            if (targetElement) {
                                targetElement.scrollIntoView({ behavior: 'smooth' });
                                history.pushState(null, '', `${window.location.pathname}#${hash}`);
                            }
                            return;
                        }
                    } else {
                        if (defaultNav(document.location.pathname, href)) {
                            return;
                        }
                        e.preventDefault();
                        go(href);
                        return;
                    }
                }
            }
            cur = cur.parentNode as Node;
        }
    };

	const scroll = () => {
		if (scrollTarget) {
			var html = document.body.parentNode as HTMLHtmlElement;
			html.scrollTo({ top: scrollTarget.y, left: scrollTarget.x, behavior: 'instant' });
		}
	}

	useEffect(() => {
		if (scrollTimer.current) {
			clearInterval(scrollTimer.current);
			scrollTimer.current = null;
		}

		if(scrollTarget){
			scrollTimer.current = setTimeout(scroll, 100);
		}
	}, [scrollTarget]);
	
	useEffect(() => {
		
		const onContentChange = (e: Event) => {
			const po = pageState.po as Content<uint>;
			var ce = e as CustomEvent<ContentChangeDetail>;
			var detail = ce.detail;
			if (po && po.type == detail.endpointType && po.id == detail.entity.id){
				var pgState: PageState = {...pageState, po: detail.entity};
				setPage(pgState);
			}
		};
		
		const onWsMessage = (e : Event) => {
			const po = pageState.po as Content<uint>;
			var ce = e as CustomEvent<any>;
			var message = ce.detail;
			if(po && po.type == message.type && po.id == message.entity.id){
				var pgState = {...pageState, po: message.entity};
				setPage(pgState);
			}
		};
		
		window.addEventListener("popstate", onPopState);
		document.addEventListener("click", onLinkClick);
		document.addEventListener("contentchange", onContentChange);
		document.addEventListener("websocketmessage", onWsMessage);
		
		return () => {
			if (scrollTimer.current) {
				clearInterval(scrollTimer.current);
			}

			window.removeEventListener("popstate", onPopState);
			document.removeEventListener("click", onLinkClick);
			document.removeEventListener("contentchange", onContentChange);
			document.removeEventListener("websocketmessage", onWsMessage);
		};
	}, [pageState]);
	
	var { page } = pageState;
	
	useEffect(() => {
		if (pageState && pageState.title) {
			// The page state title includes resolved tokens
			document.title = pageState.title;
		}

		if (pageState && pageState.description) {
			// The page state description includes resolved tokens
			document.querySelector('meta[name="description"]')?.setAttribute("content", pageState.description);
		}
	});
	
	const changeQuery = (urlParams: URLSearchParams) => {
		let currentUrl = pageState.url.split('?')[0];
		const qs = urlParams.toString();
		const nextUrl = qs ? `${currentUrl}?${qs}` : currentUrl;
		go(nextUrl);
	}
	
	return <routerCtx.Provider
			value={{
				canGoBack,
				pageState,
				setPage: go,
				changeQuery,
				getPageIncludes: () => {
					return pageState?.page?.primaryContentIncludes || undefined;
				},
				updateQuery: (update: Record<string, string | number | boolean | (string | number | boolean)[] | null | undefined>) => {
					const urlParams = new URLSearchParams(pageState.query);

					for (const key of Object.keys(update)) {
						const raw = update[key];

						// delete if nullish/empty string
						if (raw === undefined || raw === null || (typeof raw === "string" && raw.trim() === "")) {
							urlParams.delete(key);
							continue;
						}

						// arrays => repeated keys
						if (Array.isArray(raw)) {
							urlParams.delete(key);
							for (const item of raw) {
								if (item !== undefined && item !== null) {
									urlParams.append(key, String(item));
								}
							}
							continue;
						}

						// scalar => single value
						urlParams.set(key, String(raw));
						}

					changeQuery(urlParams);
				},

				removeQueryItems: (items: string[]) => {
					const urlParams = new URLSearchParams(pageState.query);
					items.forEach(item => urlParams.delete(item));
					changeQuery(urlParams);
				},

				setPrimaryObject: (obj: any) => {
					const po = pageState.po as Content<uint>;
					if (!po || !obj) {
						return;
					}
					if (po.type == obj.type && po.id == obj.id) {
						var pgState: PageState = {...pageState, po: obj};
						setPage(pgState);
					}
				}
			}}
		>
		{
			page ? (typeof page.bodyJson == 'string' ? <Canvas>{page.bodyJson}</Canvas> : <Canvas bodyJson={ page.bodyJson } />) : null
		}
		<AdminTrigger page={page}/>
	</routerCtx.Provider>;
}

export default Router;
