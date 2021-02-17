function getMobileSmall() {
    return { from: 0, to: 320 };
}

function getMobileMedium() {
    var mobileSmall = getMobileSmall();
    var mobileLarge = getMobileLarge();

    return { from: mobileSmall.to + 1, to: mobileLarge.from - 1 };
}

function getMobileLarge() {
    return { from: 412, to: 767 };
}

function getMobileInfo() {
    var mobileInfo = {
        isMobile: false,
        small: false,
        medium: false,
        large: false
    };

    var mobileSmall = getMobileSmall();
    var mobileMedium = getMobileMedium();
    var mobileLarge = getMobileLarge();

    // small mobile (>= 320px)
    if (window.matchMedia('(max-width: ' + mobileSmall.to + 'px) and (pointer: coarse) and (orientation: portrait)').matches ||
        window.matchMedia('(max-height: ' + mobileSmall.to + 'px) and (pointer: coarse) and (orientation: landscape)').matches) {
        mobileInfo.isMobile = true;
        mobileInfo.small = true;
    }

    // medium mobile (321px - 411px)
    if (window.matchMedia('(min-width: ' + mobileMedium.from + 'px) and (max-width: ' + mobileMedium.to + 'px) and (pointer: coarse) and (orientation: portrait)').matches ||
        window.matchMedia('(min-height: ' + mobileMedium.from + 'px) and (max-height: ' + mobileMedium.to + 'px) and (pointer: coarse) and (orientation: landscape)').matches) {
        mobileInfo.isMobile = true;
        mobileInfo.medium = true;
    }

    // large mobile (412px - 767px)
    if (window.matchMedia('(min-width: ' + mobileLarge.from + 'px) and (max-width: ' + mobileLarge.to + 'px) and (pointer: coarse) and (orientation: portrait)').matches ||
        window.matchMedia('(min-height: ' + mobileLarge.from + 'px) and (max-height: ' + mobileLarge.to + 'px) and (pointer: coarse) and (orientation: landscape)').matches) {
        mobileInfo.isMobile = true;
        mobileInfo.large = true;
    }

    return mobileInfo;
}

function isMobile() {
    var mobileInfo = getMobileInfo();

    return mobileInfo.isMobile;
}

// TODO: extend to cover non-Apple tablet devices
function getTabletInfo() {
    var tabletInfo = {
        isTablet: false,
        isIPad: false,
        isIPadMini: false,
        isIPadPro: false,
        isIPad10_2: false, // 10.2"
        isIPad10_5: false, // 10.5"
        isIPad11: false, // 11"
        isIPad12_9: false // 12.9"
    };

    // iPad Mini
    if (window.matchMedia('(device-width: 768px) and (device-height: 1024px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1024px) and (device-height: 768px) and (orientation: landscape)').matches) {
        tabletInfo.isTablet = true;
        tabletInfo.isIPad = true;
        tabletInfo.isIPadMini = true;
    }

    // iPad Pro 10.2"
    if (window.matchMedia('(device-width: 810px) and (device-height: 1080px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1080px) and (device-height: 810px) and (orientation: landscape)').matches) {
        tabletInfo.isTablet = true;
        tabletInfo.isIPad = true;
        tabletInfo.isIPadPro = true;
        tabletInfo.isIPad10_2 = true;
    }

    // iPad Pro 10.5"
    if (window.matchMedia('(device-width: 834px) and (device-height: 1112px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1112px) and (device-height: 834px) and (orientation: landscape)').matches) {
        tabletInfo.isTablet = true;
        tabletInfo.isIPad = true;
        tabletInfo.isIPadPro = true;
        tabletInfo.isIPad10_5 = true;
    }

    // iPad Pro 11"
    if (window.matchMedia('(device-width: 834px) and (device-height: 1194px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1194px) and (device-height: 834px) and (orientation: landscape)').matches) {
        tabletInfo.isTablet = true;
        tabletInfo.isIPad = true;
        tabletInfo.isIPadPro = true;
        tabletInfo.isIPad11 = true;
    }

    // iPad Pro 12.9"
    if (window.matchMedia('(device-width: 1024px) and (device-height: 1366px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1366px) and (device-height: 1024px) and (orientation: landscape)').matches) {
        tabletInfo.isTablet = true;
        tabletInfo.isIPad = true;
        tabletInfo.isIPadPro = true;
        tabletInfo.isIPad12_9 = true;
    }

    return tabletInfo;
}

function isTablet() {
    var tabletInfo = getTabletInfo();

    return tabletInfo.isTablet;
}

function isIPad() {
    var tabletInfo = getTabletInfo();

    return tabletInfo.isIPad;
}

function isDesktop() {
    var mobileInfo = getMobileInfo();
    var tabletInfo = getTabletInfo();

    // NB: fallback for IE10+ which doesn't support pointer media queries
    return !mobileInfo.isMobile && !tabletInfo.isTablet && (window.matchMedia('(pointer: fine)').matches || isIE10Plus);
}

function isPortrait() {
    return window.matchMedia('(orientation: portrait)').matches;
}

function isLandscape() {
    return window.matchMedia('(orientation: landscape)').matches;
}

if (document.getElementsByTagName && document.querySelector("html")) {
    // user agent detection
    var userAgent = navigator.userAgent || navigator.vendor || window.opera;
    var isWindowsPhone = /windows phone/i.test(userAgent);
    var isAndroid = /android/i.test(userAgent);
    var isIOS = (/iPad|iPhone|iPod/.test(navigator.platform) ||
        (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1)) &&
        !window.MSStream;
    var isChromeIOS = userAgent.match("CriOS");
    var isSafari = /^((?!chrome|android).)*safari/i.test(userAgent);
    var isOpera = typeof window.opr !== "undefined";
    var isEdge = userAgent.indexOf("Edge") > -1;
    var isChromium = window.chrome;
    var isOpera = typeof window.opr !== "undefined";
    var isChrome = !isChromeIOS &&
        window.chrome !== null &&
        typeof isChromium !== "undefined" &&
        // now MS has Edgium
        //window.navigator.vendor === "Google Inc." &&
        !isOpera &&
        !isEdge;
    var isIE = window.document.documentMode;
    var isIE10Plus = window.matchMedia("(-ms-high-contrast: active)").matches || window.matchMedia("(-ms-high-contrast: none)").matches;

    var htmlClassList = document.querySelector("html").classList;

    if (isWindowsPhone) {
        htmlClassList.add("windows-phone");
    }
    if (isAndroid) {
        htmlClassList.add("android");
    }
    if (isIOS) {
        htmlClassList.add("ios");
    }
    if (isChromeIOS) {
        htmlClassList.add("chrome-ios");
    }
    if (isSafari) {
        htmlClassList.add("safari");
    }
    if (isEdge) {
        htmlClassList.add("edge");
    }
    if (isChrome) {
        htmlClassList.add("chrome");
    }
    if (isIE) {
        htmlClassList.add("ie");
    }
    if (isIE10Plus) {
        htmlClassList.add("ie10-plus");
    }

    // platform checks
    var platform = navigator.platform.toLowerCase();

    if (platform.startsWith("win")) {
        htmlClassList.add("platform-win");
    }

    if (platform.startsWith("mac")) {
        htmlClassList.add("platform-mac");
    }

    // mobile checks
    var mobileInfo = getMobileInfo();

    if (mobileInfo.isMobile) {
        htmlClassList.add("device-mobile");

        if (mobileInfo.small) {
            htmlClassList.add("device-mobile-small");
        }

        if (mobileInfo.medium) {
            htmlClassList.add("device-mobile-medium");
        }

        if (mobileInfo.large) {
            htmlClassList.add("device-mobile-large");
        }

    }

    // tablet checks
    var tabletInfo = getTabletInfo();

    if (tabletInfo.isTablet) {
        htmlClassList.add("device-tablet");

        if (tabletInfo.isIPad) {
            htmlClassList.add("device-ipad");

            if (tabletInfo.isIPadMini) {
                htmlClassList.add("device-ipad-mini");
            }

            if (tabletInfo.isIPadPro) {
                htmlClassList.add("device-ipad-pro");
            }

            if (tabletInfo.isIPad10_2) {
                htmlClassList.add("device-ipad-pro-10-2");
            }

            if (tabletInfo.isIPad10_5) {
                htmlClassList.add("device-ipad-pro-10-5");
            }

            if (tabletInfo.isIPad11) {
                htmlClassList.add("device-ipad-pro-11");
            }

            if (tabletInfo.isIPad12_9) {
                htmlClassList.add("device-ipad-pro-12-9");
            }

        }

    }

    // desktop (approximate!)
    if (isDesktop()) {
        htmlClassList.add("device-desktop");
    }

}

module.exports = {
    getMobileSmall,
    getMobileMedium,
    getMobileLarge,
    getMobileInfo,
    isMobile,
    getTabletInfo,
    isTablet,
    isIPad,
    isDesktop,
    // NB: be sure to refresh orientation checks by watching for orientationChange events
    // (iOS < 14.3 will need a resize event handler - check isPortrait() / isLandscape() within the resize event)
    isPortrait,
    isLandscape
};