if (global.document && global.document.getElementsByTagName("html").length) {
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
    var isIE10Plus = window.matchMedia("(-ms-high-contrast: active)").matches || window.matchMedia("(-ms-high-contrast: none)").matches;

    var htmlClassList = global.document.getElementsByTagName("html")[0].classList;

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

    // mobile checks (appproximate!)
    var isMobile = false;

    // small mobile (>= 320px)
    if (window.matchMedia('(max-width: 320px) and (pointer: coarse) and (orientation: portrait)').matches ||
        window.matchMedia('(max-height: 320px) and (pointer: coarse) and (orientation: landscape)').matches) {
        htmlClassList.add("device-mobile", "device-mobile-small");
        isMobile = true;
    }

    // medium mobile (321px - 411px)
    if (window.matchMedia('(min-width: 321px) and (max-width: 411px) and (pointer: coarse) and (orientation: portrait)').matches ||
        window.matchMedia('(min-height: 321px) and (max-height: 411px) and (pointer: coarse) and (orientation: landscape)').matches) {
        htmlClassList.add("device-mobile", "device-mobile-medium");
        isMobile = true;
    }

    // large mobile (412px - 767px)
    if (window.matchMedia('(min-width: 412px) and (max-width: 767px) and (pointer: coarse) and (orientation: portrait)').matches ||
        window.matchMedia('(min-height: 412px) and (max-height: 767px) and (pointer: coarse) and (orientation: landscape)').matches) {
        htmlClassList.add("device-mobile", "device-mobile-large");
        isMobile = true;
    }


    // iPad checks
    var isiPad = false;

    // iPad Mini
    if (window.matchMedia('(device-width: 768px) and (device-height: 1024px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1024px) and (device-height: 768px) and (orientation: landscape)').matches) {
        htmlClassList.add("device-ipad", "device-ipad-mini");
        isiPad = true;
    }

    // iPad Pro 10.2"
    if (window.matchMedia('(device-width: 810px) and (device-height: 1080px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1080px) and (device-height: 810px) and (orientation: landscape)').matches) {
        htmlClassList.add("device-ipad", "device-ipad-pro", "device-ipad-pro-10-2");
        isiPad = true;
    }

    // iPad Pro 10.5"
    if (window.matchMedia('(device-width: 834px) and (device-height: 1112px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1112px) and (device-height: 834px) and (orientation: landscape)').matches) {
        htmlClassList.add("device-ipad", "device-ipad-pro", "device-ipad-pro-10-5");
        isiPad = true;
    }

    // iPad Pro 11"
    if (window.matchMedia('(device-width: 834px) and (device-height: 1194px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1194px) and (device-height: 834px) and (orientation: landscape)').matches) {
        htmlClassList.add("device-ipad", "device-ipad-pro", "device-ipad-pro-11");
        isiPad = true;
    }

    // iPad Pro 12.9"
    if (window.matchMedia('(device-width: 1024px) and (device-height: 1366px) and (orientation: portrait)').matches ||
        window.matchMedia('(device-width: 1366px) and (device-height: 1024px) and (orientation: landscape)').matches) {
        htmlClassList.add("device-ipad", "device-ipad-pro", "device-ipad-pro-12-9");
        isiPad = true;
    }

    // desktop (approximate!)
    // NB: fallback for IE10+ which doesn't support pointer media queries
    if (!isMobile && !isiPad && (window.matchMedia('(pointer: fine)').matches || isIE10Plus)) {
        htmlClassList.add("device-desktop");
    }

}
