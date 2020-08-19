// startsWith polyfill
if (!String.prototype.startsWith) {
    Object.defineProperty(String.prototype, 'startsWith', {
        value: function (search, rawPos) {
            var pos = rawPos > 0 ? rawPos | 0 : 0;
            return this.substring(pos, pos + search.length) === search;
        }
    });
}

/**
 * String.prototype.padStart() polyfill
 * https://github.com/uxitten/polyfill/blob/master/string.polyfill.js
 * https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/padStart
 */
if (!String.prototype.padStart) {
    String.prototype.padStart = function padStart(targetLength, padString) {
        targetLength = targetLength >> 0; //truncate if number or convert non-number to 0;
        padString = String((typeof padString !== 'undefined' ? padString : ' '));
        if (this.length > targetLength) {
            return String(this);
        }
        else {
            targetLength = targetLength - this.length;
            if (targetLength > padString.length) {
                padString += padString.repeat(targetLength / padString.length); //append to original to ensure we are longer than needed
            }
            return padString.slice(0, targetLength) + String(this);
        }
    };
}

// check CSS selector
function supportsSelector(selector) {
    try {
        document.querySelector(selector)
    } catch (error) {
        return false;
    }
    return true;
}

// patch missing :focus-within support
if (!supportsSelector(':focus-within')) {
    var slice = [].slice;
    var removeClass = function (elem) {
        elem.classList.remove('focus-within');
    };

    var update = (function () {
        var running, last;
        var action = function () {
            var element = document.activeElement;

            running = false;

            if (last !== element) {
                last = element;
                slice.call(document.getElementsByClassName('focus-within')).forEach(removeClass);

                while (element && element.classList) {
                    element.classList.add('focus-within');
                    element = element.parentNode;
                }
            }
        };

        return function () {
            if (!running) {
                requestAnimationFrame(action);
                running = true;
            }
        };
    })();

    document.addEventListener('focus', update, true);
    document.addEventListener('blur', update, true);

    update();
}

// Promise polyfill for IE11, source: https://www.npmjs.com/package/promise-polyfill
!function (e, n) { "object" == typeof exports && "undefined" != typeof module ? n() : "function" == typeof define && define.amd ? define(n) : n() }(0, function () { "use strict"; function e(e) { var n = this.constructor; return this.then(function (t) { return n.resolve(e()).then(function () { return t }) }, function (t) { return n.resolve(e()).then(function () { return n.reject(t) }) }) } function n(e) { return !(!e || "undefined" == typeof e.length) } function t() { } function o(e) { if (!(this instanceof o)) throw new TypeError("Promises must be constructed via new"); if ("function" != typeof e) throw new TypeError("not a function"); this._state = 0, this._handled = !1, this._value = undefined, this._deferreds = [], c(e, this) } function r(e, n) { for (; 3 === e._state;)e = e._value; 0 !== e._state ? (e._handled = !0, o._immediateFn(function () { var t = 1 === e._state ? n.onFulfilled : n.onRejected; if (null !== t) { var o; try { o = t(e._value) } catch (r) { return void f(n.promise, r) } i(n.promise, o) } else (1 === e._state ? i : f)(n.promise, e._value) })) : e._deferreds.push(n) } function i(e, n) { try { if (n === e) throw new TypeError("A promise cannot be resolved with itself."); if (n && ("object" == typeof n || "function" == typeof n)) { var t = n.then; if (n instanceof o) return e._state = 3, e._value = n, void u(e); if ("function" == typeof t) return void c(function (e, n) { return function () { e.apply(n, arguments) } }(t, n), e) } e._state = 1, e._value = n, u(e) } catch (r) { f(e, r) } } function f(e, n) { e._state = 2, e._value = n, u(e) } function u(e) { 2 === e._state && 0 === e._deferreds.length && o._immediateFn(function () { e._handled || o._unhandledRejectionFn(e._value) }); for (var n = 0, t = e._deferreds.length; t > n; n++)r(e, e._deferreds[n]); e._deferreds = null } function c(e, n) { var t = !1; try { e(function (e) { t || (t = !0, i(n, e)) }, function (e) { t || (t = !0, f(n, e)) }) } catch (o) { if (t) return; t = !0, f(n, o) } } var a = setTimeout; o.prototype["catch"] = function (e) { return this.then(null, e) }, o.prototype.then = function (e, n) { var o = new this.constructor(t); return r(this, new function (e, n, t) { this.onFulfilled = "function" == typeof e ? e : null, this.onRejected = "function" == typeof n ? n : null, this.promise = t }(e, n, o)), o }, o.prototype["finally"] = e, o.all = function (e) { return new o(function (t, o) { function r(e, n) { try { if (n && ("object" == typeof n || "function" == typeof n)) { var u = n.then; if ("function" == typeof u) return void u.call(n, function (n) { r(e, n) }, o) } i[e] = n, 0 == --f && t(i) } catch (c) { o(c) } } if (!n(e)) return o(new TypeError("Promise.all accepts an array")); var i = Array.prototype.slice.call(e); if (0 === i.length) return t([]); for (var f = i.length, u = 0; i.length > u; u++)r(u, i[u]) }) }, o.resolve = function (e) { return e && "object" == typeof e && e.constructor === o ? e : new o(function (n) { n(e) }) }, o.reject = function (e) { return new o(function (n, t) { t(e) }) }, o.race = function (e) { return new o(function (t, r) { if (!n(e)) return r(new TypeError("Promise.race accepts an array")); for (var i = 0, f = e.length; f > i; i++)o.resolve(e[i]).then(t, r) }) }, o._immediateFn = "function" == typeof setImmediate && function (e) { setImmediate(e) } || function (e) { a(e, 0) }, o._unhandledRejectionFn = function (e) { void 0 !== console && console && console.warn("Possible Unhandled Promise Rejection:", e) }; var l = function () { if ("undefined" != typeof self) return self; if ("undefined" != typeof window) return window; if ("undefined" != typeof global) return global; throw Error("unable to locate global object") }(); "Promise" in l ? l.Promise.prototype["finally"] || (l.Promise.prototype["finally"] = e) : l.Promise = o });

// Fetch:
var Promise = global.Promise;
global.fetch || (global.fetch = function (e, n) { return n = n || {}, new Promise(function (t, s) { var r = new XMLHttpRequest, o = [], u = [], i = {}, a = function () { return { ok: 2 == (r.status / 100 | 0), statusText: r.statusText, status: r.status, url: r.responseURL, text: function () { return Promise.resolve(r.responseText) }, json: function () { return Promise.resolve(JSON.parse(r.responseText)) }, blob: function () { return Promise.resolve(new Blob([r.response])) }, clone: a, headers: { keys: function () { return o }, entries: function () { return u }, get: function (e) { return i[e.toLowerCase()] }, has: function (e) { return e.toLowerCase() in i } } } }; for (var c in r.open(n.method || "get", e, !0), r.onload = function () { r.getAllResponseHeaders().replace(/^(.*?):[^\S\n]*([\s\S]*?)$/gm, function (e, n, t) { o.push(n = n.toLowerCase()), u.push([n, t]), i[n] = i[n] ? i[n] + "," + t : t }), t(a()) }, r.onerror = s, r.withCredentials = "include" == n.credentials, n.headers) r.setRequestHeader(c, n.headers[c]); r.send(n.body || null) }) });

// CustomEvent polyfill
if (!("CustomEvent" in window && typeof window.CustomEvent === "function")) {
    function CustomEvent(event, params) {
        params = params || {
            bubbles: false,
            cancelable: false,
            detail: undefined
        };
        var evt = document.createEvent("CustomEvent");
        evt.initCustomEvent(
            event,
            params.bubbles,
            params.cancelable,
            params.detail
        );
        return evt;
    }

    CustomEvent.prototype = window.Event.prototype;

    window.CustomEvent = CustomEvent;
}

// array.includes polyfill
// https://tc39.github.io/ecma262/#sec-array.prototype.includes
if (!Array.prototype.includes) {
    Object.defineProperty(Array.prototype, "includes", {
        value: function (searchElement, fromIndex) {
            if (this == null) {
                throw new TypeError('"this" is null or not defined');
            }

            // 1. Let O be ? ToObject(this value).
            var o = Object(this);

            // 2. Let len be ? ToLength(? Get(O, "length")).
            var len = o.length >>> 0;

            // 3. If len is 0, return false.
            if (len === 0) {
                return false;
            }

            // 4. Let n be ? ToInteger(fromIndex).
            //    (If fromIndex is undefined, this step produces the value 0.)
            var n = fromIndex | 0;

            // 5. If n >= 0, then
            //  a. Let k be n.
            // 6. Else n < 0,
            //  a. Let k be len + n.
            //  b. If k < 0, let k be 0.
            var k = Math.max(n >= 0 ? n : len - Math.abs(n), 0);

            function sameValueZero(x, y) {
                return (
                    x === y ||
                    (typeof x === "number" &&
                        typeof y === "number" &&
                        isNaN(x) &&
                        isNaN(y))
                );
            }

            // 7. Repeat, while k < len
            while (k < len) {
                // a. Let elementK be the result of ? Get(O, ! ToString(k)).
                // b. If SameValueZero(searchElement, elementK) is true, return true.
                if (sameValueZero(o[k], searchElement)) {
                    return true;
                }
                // c. Increase k by 1.
                k++;
            }

            // 8. Return false
            return false;
        }
    });
}

// Polyfill for Element.closest
// https://developer.mozilla.org/en-US/docs/Web/API/Element/closest
if (!Element.prototype.matches) {
    Element.prototype.matches =
        Element.prototype.msMatchesSelector ||
        Element.prototype.webkitMatchesSelector;
}

if (!Element.prototype.closest) {
    Element.prototype.closest = function (s) {
        var el = this;

        do {
            if (el.matches(s)) return el;
            el = el.parentElement || el.parentNode;
        } while (el !== null && el.nodeType === 1);

        return null;
    };
}

//! npm.im/object-fit-images 3.2.4
var objectFitImages = (function () {
    "use strict";
    function t(t, e) {
        return (
            "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='" +
            t +
            "' height='" +
            e +
            "'%3E%3C/svg%3E"
        );
    }
    function e(t) {
        if (t.srcset && !p && window.picturefill) {
            var e = window.picturefill._;
            (t[e.ns] && t[e.ns].evaled) || e.fillImg(t, { reselect: !0 }),
                t[e.ns].curSrc ||
                ((t[e.ns].supported = !1), e.fillImg(t, { reselect: !0 })),
                (t.currentSrc = t[e.ns].curSrc || t.src);
        }
    }
    function i(t) {
        for (
            var e, i = getComputedStyle(t).fontFamily, r = {};
            null !== (e = u.exec(i));

        )
            r[e[1]] = e[2];
        return r;
    }
    function r(e, i, r) {
        var n = t(i || 1, r || 0);
        b.call(e, "src") !== n && h.call(e, "src", n);
    }
    function n(t, e) {
        t.naturalWidth ? e(t) : setTimeout(n, 100, t, e);
    }
    function c(t) {
        var c = i(t),
            o = t[l];
        if (((c["object-fit"] = c["object-fit"] || "fill"), !o.img)) {
            if ("fill" === c["object-fit"]) return;
            if (!o.skipTest && f && !c["object-position"]) return;
        }
        if (!o.img) {
            (o.img = new Image(t.width, t.height)),
                (o.img.srcset = b.call(t, "data-ofi-srcset") || t.srcset),
                (o.img.src = b.call(t, "data-ofi-src") || t.src),
                h.call(t, "data-ofi-src", t.src),
                t.srcset && h.call(t, "data-ofi-srcset", t.srcset),
                r(t, t.naturalWidth || t.width, t.naturalHeight || t.height),
                t.srcset && (t.srcset = "");
            try {
                s(t);
            } catch (t) {
                window.console && console.warn("https://bit.ly/ofi-old-browser");
            }
        }
        e(o.img),
            (t.style.backgroundImage =
                'url("' +
                (o.img.currentSrc || o.img.src).replace(/"/g, '\\"') +
                '")'),
            (t.style.backgroundPosition = c["object-position"] || "center"),
            (t.style.backgroundRepeat = "no-repeat"),
            (t.style.backgroundOrigin = "content-box"),
            /scale-down/.test(c["object-fit"])
                ? n(o.img, function () {
                    o.img.naturalWidth > t.width || o.img.naturalHeight > t.height
                        ? (t.style.backgroundSize = "contain")
                        : (t.style.backgroundSize = "auto");
                })
                : (t.style.backgroundSize = c["object-fit"]
                    .replace("none", "auto")
                    .replace("fill", "100% 100%")),
            n(o.img, function (e) {
                r(t, e.naturalWidth, e.naturalHeight);
            });
    }
    function s(t) {
        var e = {
            get: function (e) {
                return t[l].img[e ? e : "src"];
            },
            set: function (e, i) {
                return (
                    (t[l].img[i ? i : "src"] = e),
                    h.call(t, "data-ofi-" + i, e),
                    c(t),
                    e
                );
            }
        };
        Object.defineProperty(t, "src", e),
            Object.defineProperty(t, "currentSrc", {
                get: function () {
                    return e.get("currentSrc");
                }
            }),
            Object.defineProperty(t, "srcset", {
                get: function () {
                    return e.get("srcset");
                },
                set: function (t) {
                    return e.set(t, "srcset");
                }
            });
    }
    function o() {
        function t(t, e) {
            return t[l] && t[l].img && ("src" === e || "srcset" === e)
                ? t[l].img
                : t;
        }
        d ||
            ((HTMLImageElement.prototype.getAttribute = function (e) {
                return b.call(t(this, e), e);
            }),
                (HTMLImageElement.prototype.setAttribute = function (e, i) {
                    return h.call(t(this, e), e, String(i));
                }));
    }
    function a(t, e) {
        var i = !y && !t;
        if (((e = e || {}), (t = t || "img"), (d && !e.skipTest) || !m))
            return !1;
        "img" === t
            ? (t = document.getElementsByTagName("img"))
            : "string" == typeof t
                ? (t = document.querySelectorAll(t))
                : "length" in t || (t = [t]);
        for (var r = 0; r < t.length; r++)
            (t[r][l] = t[r][l] || { skipTest: e.skipTest }), c(t[r]);
        i &&
            (document.body.addEventListener(
                "load",
                function (t) {
                    "IMG" === t.target.tagName && a(t.target, { skipTest: e.skipTest });
                },
                !0
            ),
                (y = !0),
                (t = "img")),
            e.watchMQ &&
            window.addEventListener(
                "resize",
                a.bind(null, t, { skipTest: e.skipTest })
            );
    }
    var l = "bfred-it:object-fit-images",
        u = /(object-fit|object-position)\s*:\s*([-.\w\s%]+)/g,
        g =
            "undefined" == typeof Image
                ? { style: { "object-position": 1 } }
                : new Image(),
        f = "object-fit" in g.style,
        d = "object-position" in g.style,
        m = "background-size" in g.style,
        p = "string" == typeof g.currentSrc,
        b = g.getAttribute,
        h = g.setAttribute,
        y = !1;
    return (a.supportsObjectFit = f), (a.supportsObjectPosition = d), o(), a;
})();

/*
Details Element Polyfill 2.4.0
Copyright © 2019 Javan Makhmali
 */
(function () {
    "use strict";
    var element = document.createElement("details");
    var elementIsNative = typeof HTMLDetailsElement != "undefined" && element instanceof HTMLDetailsElement;
    var support = {
        open: "open" in element || elementIsNative,
        toggle: "ontoggle" in element
    };
    var styles = '\ndetails, summary {\n  display: block;\n}\ndetails:not([open]) > *:not(summary) {\n  display: none;\n}\nsummary::before {\n  content: ">";\n  padding-right: 0.3rem;\n  font-size: 0.6rem;\n  cursor: default;\n}\n[open] > summary::before {\n  content: ">";\n}\n';
    var _ref = [], forEach = _ref.forEach, slice = _ref.slice;
    if (!support.open) {
        polyfillStyles();
        polyfillProperties();
        polyfillToggle();
        polyfillAccessibility();
    }
    if (support.open && !support.toggle) {
        polyfillToggleEvent();
    }
    function polyfillStyles() {
        document.head.insertAdjacentHTML("afterbegin", "<style>" + styles + "</style>");
    }
    function polyfillProperties() {
        var prototype = document.createElement("details").constructor.prototype;
        var setAttribute = prototype.setAttribute, removeAttribute = prototype.removeAttribute;
        var open = Object.getOwnPropertyDescriptor(prototype, "open");
        Object.defineProperties(prototype, {
            open: {
                get: function get() {
                    if (this.tagName == "DETAILS") {
                        return this.hasAttribute("open");
                    } else {
                        if (open && open.get) {
                            return open.get.call(this);
                        }
                    }
                },
                set: function set(value) {
                    if (this.tagName == "DETAILS") {
                        return value ? this.setAttribute("open", "") : this.removeAttribute("open");
                    } else {
                        if (open && open.set) {
                            return open.set.call(this, value);
                        }
                    }
                }
            },
            setAttribute: {
                value: function value(name, _value) {
                    var _this = this;
                    var call = function call() {
                        return setAttribute.call(_this, name, _value);
                    };
                    if (name == "open" && this.tagName == "DETAILS") {
                        var wasOpen = this.hasAttribute("open");
                        var result = call();
                        if (!wasOpen) {
                            var summary = this.querySelector("summary");
                            if (summary) summary.setAttribute("aria-expanded", true);
                            triggerToggle(this);
                        }
                        return result;
                    }
                    return call();
                }
            },
            removeAttribute: {
                value: function value(name) {
                    var _this2 = this;
                    var call = function call() {
                        return removeAttribute.call(_this2, name);
                    };
                    if (name == "open" && this.tagName == "DETAILS") {
                        var wasOpen = this.hasAttribute("open");
                        var result = call();
                        if (wasOpen) {
                            var summary = this.querySelector("summary");
                            if (summary) summary.setAttribute("aria-expanded", false);
                            triggerToggle(this);
                        }
                        return result;
                    }
                    return call();
                }
            }
        });
    }
    function polyfillToggle() {
        onTogglingTrigger(function (element) {
            element.hasAttribute("open") ? element.removeAttribute("open") : element.setAttribute("open", "");
        });
    }
    function polyfillToggleEvent() {
        if (window.MutationObserver) {
            new MutationObserver(function (mutations) {
                forEach.call(mutations, function (mutation) {
                    var target = mutation.target, attributeName = mutation.attributeName;
                    if (target.tagName == "DETAILS" && attributeName == "open") {
                        triggerToggle(target);
                    }
                });
            }).observe(document.documentElement, {
                attributes: true,
                subtree: true
            });
        } else {
            onTogglingTrigger(function (element) {
                var wasOpen = element.getAttribute("open");
                setTimeout(function () {
                    var isOpen = element.getAttribute("open");
                    if (wasOpen != isOpen) {
                        triggerToggle(element);
                    }
                }, 1);
            });
        }
    }
    function polyfillAccessibility() {
        setAccessibilityAttributes(document);
        if (window.MutationObserver) {
            new MutationObserver(function (mutations) {
                forEach.call(mutations, function (mutation) {
                    forEach.call(mutation.addedNodes, setAccessibilityAttributes);
                });
            }).observe(document.documentElement, {
                subtree: true,
                childList: true
            });
        } else {
            document.addEventListener("DOMNodeInserted", function (event) {
                setAccessibilityAttributes(event.target);
            });
        }
    }
    function setAccessibilityAttributes(root) {
        findElementsWithTagName(root, "SUMMARY").forEach(function (summary) {
            var details = findClosestElementWithTagName(summary, "DETAILS");
            summary.setAttribute("aria-expanded", details.hasAttribute("open"));
            if (!summary.hasAttribute("tabindex")) summary.setAttribute("tabindex", "0");
            if (!summary.hasAttribute("role")) summary.setAttribute("role", "button");
        });
    }
    function eventIsSignificant(event) {
        return !(event.defaultPrevented || event.ctrlKey || event.metaKey || event.shiftKey || event.target.isContentEditable);
    }
    function onTogglingTrigger(callback) {
        addEventListener("click", function (event) {
            if (eventIsSignificant(event)) {
                if (event.which <= 1) {
                    var element = findClosestElementWithTagName(event.target, "SUMMARY");
                    if (element && element.parentNode && element.parentNode.tagName == "DETAILS") {
                        callback(element.parentNode);
                    }
                }
            }
        }, false);
        addEventListener("keydown", function (event) {
            if (eventIsSignificant(event)) {
                if (event.keyCode == 13 || event.keyCode == 32) {
                    var element = findClosestElementWithTagName(event.target, "SUMMARY");
                    if (element && element.parentNode && element.parentNode.tagName == "DETAILS") {
                        callback(element.parentNode);
                        event.preventDefault();
                    }
                }
            }
        }, false);
    }
    function triggerToggle(element) {
        var event = document.createEvent("Event");
        event.initEvent("toggle", false, false);
        element.dispatchEvent(event);
    }
    function findElementsWithTagName(root, tagName) {
        return (root.tagName == tagName ? [root] : []).concat(typeof root.getElementsByTagName == "function" ? slice.call(root.getElementsByTagName(tagName)) : []);
    }
    function findClosestElementWithTagName(element, tagName) {
        if (typeof element.closest == "function") {
            return element.closest(tagName);
        } else {
            while (element) {
                if (element.tagName == tagName) {
                    return element;
                } else {
                    element = element.parentNode;
                }
            }
        }
    }
})();

// DOM ready
document.addEventListener('DOMContentLoaded', function (event) {
    // trigger IE11 object-fit / object-position polyfill
    objectFitImages();
})
