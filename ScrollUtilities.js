var scrollTimer = null;
var ignoreMouseWheel = false;

// if scrolling up / down from the current position would mean only moving this far to the top / end of the current slide, skip a full page instead
// (prevents irritating small 'nudges')
const SCROLL_TOLERANCE = 48;

// translate wheel scroll into page up / down calls
function mouseWheelScroll(e) {
    var body = document.querySelector("body");

    if (body.classList.contains("burger-open")) {
        return;
    }

    e.preventDefault();

    if (scrollTimer || ignoreMouseWheel) {
        return;
    }

    ignoreMouseWheel = true;

    // ignore mouse wheel input for the next few ms
    setTimeout(function () {
        ignoreMouseWheel = false;
    }, 150);


    if (e.deltaY < 0) {
        pageUp();
    }

    if (e.deltaY > 0) {
        pageDown();
    }

}

/// keydown handler
function keyDownHandler(event) {
    var body = document.querySelector("body");

    if (body.classList.contains("burger-open")) {
        return;
    }

    var tag = event.target.tagName.toLowerCase();
    var ignoredTags = ['input', 'textarea', 'button', 'a', 'datalist', 'option', 'iframe', 'area', 'audio', 'video', 'embed', 'object'];

    for (var i = 0; i < ignoredTags.length; i++) {

        if (tag == ignoredTags[i]) {
            return;
        }

    }

    var keyCode = event.keyCode || event.which;

    // check for IME events
    if (event.isComposing || keyCode === 229) {
        return;
    }

    switch (keyCode) {
        // handle <space> / [SHIFT]+<space>
        case KeyEvent.DOM_VK_SPACE:
            event.preventDefault();
            if (event.shiftKey) {
                pageUp();
            } else {
                pageDown();
            }

            break;

        // handle cursor up / page up
        case KeyEvent.DOM_VK_PAGE_UP:
        case KeyEvent.DOM_VK_UP:
            event.preventDefault();
            pageUp();
            break;

        // handle cursor down / page down
        case KeyEvent.DOM_VK_PAGE_DOWN:
        case KeyEvent.DOM_VK_DOWN:
            event.preventDefault();
            pageDown();
            break;

        // handle home
        case KeyEvent.DOM_VK_HOME:
            event.preventDefault();
            home();
            break;

        // handle end
        case KeyEvent.DOM_VK_END:
            event.preventDefault();
            end();
            break;
    }
}

// get current page scroll position
function getScrollPos() {
    return global.pageYOffset != undefined ? global.pageYOffset :
        document.documentElement.scrollTop || document.body.scrollTop || 0;
}

// get viewport height
function getViewportHeight() {
    return Math.max(document.documentElement.clientHeight, global.innerHeight);
}

// get full page height
function getDocumentHeight() {
    var body = document.body,
        html = document.documentElement;

    return Math.max(body.scrollHeight, body.offsetHeight, html.clientHeight, html.scrollHeight, html.offsetHeight);
}

// return number of steps on this page
function getPageCount() {
    var pages = document.querySelectorAll("[data-slide]");

    return pages ? pages.length : 0;
}

// return given page
function getPage(index) {
    var pages = document.querySelectorAll("[data-slide]");

    return pages && index <= pages.length ? pages[index - 1] : null;
}

// get y value at top of given page
function getPageTop(index) {
    var pages = document.querySelectorAll("[data-slide]");
    var startY = 0;

    if (index > pages.length) {
        index = pages.length;
    }

    for (var i = 0; i < index-1; i++) {
        startY += pages[i].offsetHeight;
    }

    return startY;
}

// get y value at bottom of given page
function getPageBottom(index) {
    var pages = document.querySelectorAll("[data-slide]");
    var endY = 0;

    if (index > pages.length) {
        index = pages.length;
    }

    for (var i = 0; i < index; i++) {
        endY += pages[i].offsetHeight;
    }

    return endY;
}

// return active page index
function getActiveIndex() {
    var pages = document.querySelectorAll("[data-slide]");
    var scrollPos = getScrollPos();
    var startY = 0;

    for (var i = 0; i < pages.length; i++) {
        var page = pages[i];
        var endY = startY + page.offsetHeight;

        if (scrollPos >= startY && scrollPos < endY) {
            return i + 1;
        }

        startY += page.offsetHeight;
    }

    return pages.length;
}

// move up one page
function pageUp() {
    var html = document.querySelector("html");

    if (html.classList.contains("admin")) {
        return;
    }

    var activeIndex = getActiveIndex();
    var pageTop = getPageTop(activeIndex);
    var page = getPage(activeIndex);

    if (!page) {
        return;
    }

    var partialScrollingDisabled = page.getAttribute("data-disable-partial-scroll") !== null;
    var viewportHeight = getViewportHeight();
    var scrollPos = getScrollPos();

    // skip if we're already at the top of the document
    if (scrollPos <= 0) {
        return;
    }

    // if this page is longer than the viewport height and we're not yet at the top, scroll up
    if (((scrollPos - pageTop) > SCROLL_TOLERANCE) && !partialScrollingDisabled) {
        var diff = scrollPos - pageTop;
        global.scrollBy({ left: 0, top: -Math.min(diff, viewportHeight), behavior: 'smooth' });
        return;
    }

    if (activeIndex == 0) {
        home();
    }

    global.addEventListener('scroll', checkScrollingActive, false);
    global.scrollTo({ top: getPageTop(activeIndex - 1), behavior: 'smooth' });
}

// move down one page
function pageDown() {
    var html = document.querySelector("html");

    if (html.classList.contains("admin")) {
        return;
    }

    var activeIndex = getActiveIndex();
    var pageBottom = getPageBottom(activeIndex);
    var page = getPage(activeIndex);

    if (!page) {
        return;
    }

    var partialScrollingDisabled = page.getAttribute("data-disable-partial-scroll") !== null;
    var viewportHeight = getViewportHeight();
    var scrollPos = getScrollPos(); 

    // skip if we're already at the bottom of the document
    if (scrollPos + viewportHeight >= getDocumentHeight()) {
        return;
    }

    // if this page is longer than the viewport height and we're not yet at the bottom, scroll down
    if ((pageBottom - (scrollPos + viewportHeight) > SCROLL_TOLERANCE) && !partialScrollingDisabled) {
        var diff = getPageBottom(activeIndex) - (scrollPos + viewportHeight);
        global.scrollBy({ left: 0, top: Math.min(diff, viewportHeight), behavior: 'smooth' });
        return;
    }

    if (activeIndex == getPageCount()) {
        end();
    }

    // use scrollTo as opposed to scrollIntoView as the latter has been known to be out by 1px
    global.addEventListener('scroll', checkScrollingActive, false);
    global.scrollTo({ top: getPageTop(activeIndex + 1), behavior: 'smooth' });
}

// move to start of document
function home() {
    var html = document.querySelector("html");

    if (html.classList.contains("admin")) {
        return;
    }

    global.addEventListener('scroll', checkScrollingActive, false);
    global.scrollTo({ left: 0, top: 0, behavior: 'smooth' });
}

// move to end of document
function end() {
    var html = document.querySelector("html");

    if (html.classList.contains("admin")) {
        return;
    }

    global.addEventListener('scroll', checkScrollingActive, false);
    global.scrollTo({ left: 0, top: document.body.scrollHeight, behavior: 'smooth' });
}

// check if we're still scrolling
function checkScrollingActive() {

    if (scrollTimer !== null) {
        clearTimeout(scrollTimer);
    }

    scrollTimer = setTimeout(function () {
        window.removeEventListener('scroll', checkScrollingActive, false);
        scrollTimer = null;
        ignoreMouseWheel = false;
        var index = getActiveIndex();
        var page = getPage(index);
        page.classList.add("scroll-complete");
    }, 150);

}

// toggle scrolling utilities on/off
function toggle(enabled) {
    var html = document.querySelector("html");
    var isAdmin = html.classList.contains("admin");

    // check - does addEventListener support passive option?
    var passiveSupported = false;

    try {
        const options = {
            get passive() {
                // called when the browser attempts to access the passive property
                passiveSupported = true;
                return false;
            }
        };

        global.addEventListener("test", null, options);
        global.removeEventListener("test", null, options);
    } catch (err) {
        passiveSupported = false;
    }

    if (enabled && !isAdmin) {
        global.addEventListener('mousewheel', mouseWheelScroll, passiveSupported ? { passive: false } : false);
        document.addEventListener("keydown", keyDownHandler);
    } else {
        global.removeEventListener('mousewheel', mouseWheelScroll, passiveSupported ? { passive: false } : false)
        document.removeEventListener("keydown", keyDownHandler);
    }

}

module.exports = {
    pageUp,
    pageDown,
    home,
    end,
    toggle
};