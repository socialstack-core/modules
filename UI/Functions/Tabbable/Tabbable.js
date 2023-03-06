// ref: https://github.com/focus-trap/tabbable/blob/master/src/index.js
const candidateSelectors = [
  'input',
  'select',
  'textarea',
  'a[href]',
  'button',
  '[tabindex]',
  'audio[controls]',
  'video[controls]',
  '[contenteditable]:not([contenteditable="false"])',
  'details>summary:first-of-type',
  'details',
];
const candidateSelector = /* #__PURE__ */ candidateSelectors.join(',');

const matches =
  typeof Element === 'undefined'
    ? function () {}
    : Element.prototype.matches ||
      Element.prototype.msMatchesSelector ||
      Element.prototype.webkitMatchesSelector;

const getCandidates = function (el, includeContainer, filter) {
  let candidates = Array.prototype.slice.apply(
    el.querySelectorAll(candidateSelector)
  );
  if (includeContainer && matches.call(el, candidateSelector)) {
    candidates.unshift(el);
  }
  candidates = candidates.filter(filter);
  return candidates;
};

const isContentEditable = function (node) {
  return node.contentEditable === 'true';
};

const getTabindex = function (node) {
  const tabindexAttr = parseInt(node.getAttribute('tabindex'), 10);

  if (!isNaN(tabindexAttr)) {
    return tabindexAttr;
  }

  // Browsers do not return `tabIndex` correctly for contentEditable nodes;
  // so if they don't have a tabindex attribute specifically set, assume it's 0.
  if (isContentEditable(node)) {
    return 0;
  }

  // in Chrome, <details/>, <audio controls/> and <video controls/> elements get a default
  //  `tabIndex` of -1 when the 'tabindex' attribute isn't specified in the DOM,
  //  yet they are still part of the regular tab order; in FF, they get a default
  //  `tabIndex` of 0; since Chrome still puts those elements in the regular tab
  //  order, consider their tab index to be 0.
  if (
    (node.nodeName === 'AUDIO' ||
      node.nodeName === 'VIDEO' ||
      node.nodeName === 'DETAILS') &&
    node.getAttribute('tabindex') === null
  ) {
    return 0;
  }

  return node.tabIndex;
};

const sortOrderedTabbables = function (a, b) {
  return a.tabIndex === b.tabIndex
    ? a.documentOrder - b.documentOrder
    : a.tabIndex - b.tabIndex;
};

const isInput = function (node) {
  return node.tagName === 'INPUT';
};

const isHiddenInput = function (node) {
  return isInput(node) && node.type === 'hidden';
};

const isDetailsWithSummary = function (node) {
  const r =
    node.tagName === 'DETAILS' &&
    Array.prototype.slice
      .apply(node.children)
      .some((child) => child.tagName === 'SUMMARY');
  return r;
};

const getCheckedRadio = function (nodes, form) {
  for (let i = 0; i < nodes.length; i++) {
    if (nodes[i].checked && nodes[i].form === form) {
      return nodes[i];
    }
  }
};

const isTabbableRadio = function (node) {
  if (!node.name) {
    return true;
  }
  const radioScope = node.form || node.ownerDocument;

  const queryRadios = function (name) {
    return radioScope.querySelectorAll(
      'input[type="radio"][name="' + name + '"]'
    );
  };

  let radioSet;
  if (
    typeof window !== 'undefined' &&
    typeof window.CSS !== 'undefined' &&
    typeof window.CSS.escape === 'function'
  ) {
    radioSet = queryRadios(window.CSS.escape(node.name));
  } else {
    try {
      radioSet = queryRadios(node.name);
    } catch (err) {
      // eslint-disable-next-line no-console
      console.error(
        'Looks like you have a radio button with a name attribute containing invalid CSS selector characters and need the CSS.escape polyfill: %s',
        err.message
      );
      return false;
    }
  }

  const checked = getCheckedRadio(radioSet, node.form);
  return !checked || checked === node;
};

const isRadio = function (node) {
  return isInput(node) && node.type === 'radio';
};

const isNonTabbableRadio = function (node) {
  return isRadio(node) && !isTabbableRadio(node);
};

const isHidden = function (node, displayCheck) {
  if (getComputedStyle(node).visibility === 'hidden') {
    return true;
  }

  const isDirectSummary = matches.call(node, 'details>summary:first-of-type');
  const nodeUnderDetails = isDirectSummary ? node.parentElement : node;
  if (matches.call(nodeUnderDetails, 'details:not([open]) *')) {
    return true;
  }
  if (!displayCheck || displayCheck === 'full') {
    while (node) {
      if (getComputedStyle(node).display === 'none') {
        return true;
      }
      node = node.parentElement;
    }
  } else if (displayCheck === 'non-zero-area') {
    const { width, height } = node.getBoundingClientRect();
    return width === 0 && height === 0;
  }

  return false;
};

const isNodeMatchingSelectorFocusable = function (options, node) {
  if (
    node.disabled ||
    isHiddenInput(node) ||
    isHidden(node, options.displayCheck) ||
    /* For a details element with a summary, the summary element gets the focused  */
    isDetailsWithSummary(node)
  ) {
    return false;
  }
  return true;
};

const isNodeMatchingSelectorTabbable = function (options, node) {
  if (
    !isNodeMatchingSelectorFocusable(options, node) ||
    isNonTabbableRadio(node) ||
    getTabindex(node) < 0
  ) {
    return false;
  }
  return true;
};

const tabbable = function (el, options) {
  options = options || {};

  const regularTabbables = [];
  const orderedTabbables = [];

  const candidates = getCandidates(
    el,
    options.includeContainer,
    isNodeMatchingSelectorTabbable.bind(null, options)
  );

  candidates.forEach(function (candidate, i) {
    const candidateTabindex = getTabindex(candidate);
    if (candidateTabindex === 0) {
      regularTabbables.push(candidate);
    } else {
      orderedTabbables.push({
        documentOrder: i,
        tabIndex: candidateTabindex,
        node: candidate,
      });
    }
  });

  const tabbableNodes = orderedTabbables
    .sort(sortOrderedTabbables)
    .map((a) => a.node)
    .concat(regularTabbables);

  return tabbableNodes;
};

const focusable = function (el, options) {
  options = options || {};

  const candidates = getCandidates(
    el,
    options.includeContainer,
    isNodeMatchingSelectorFocusable.bind(null, options)
  );

  return candidates;
};

const isTabbable = function (node, options) {
  options = options || {};
  if (!node) {
    throw new Error('No node provided');
  }
  if (matches.call(node, candidateSelector) === false) {
    return false;
  }
  return isNodeMatchingSelectorTabbable(options, node);
};

const focusableCandidateSelector = /* #__PURE__ */ candidateSelectors
  .concat('iframe')
  .join(',');

const isFocusable = function (node, options) {
  options = options || {};
  if (!node) {
    throw new Error('No node provided');
  }
  if (matches.call(node, focusableCandidateSelector) === false) {
    return false;
  }
  return isNodeMatchingSelectorFocusable(options, node);
};

// returns next element in the tab order after the given element
// params:
//   el: source element (defaults to current active element if not supplied)
//   region: parent node to limit tab order to (defaults to entire page if not supplied)
// returns:
//   next focusable element if available (otherwise null)
const getNextTab = function (el, region) {
    el = el || document.activeElement;
    region = region || document.body;
    var tabbableNodes = tabbable(region);

    if (!tabbableNodes.includes(el)) {
        return null;
    }

    var tabIndex = tabbableNodes.indexOf(el);

    if (tabIndex == tabbableNodes.length - 1) {
        return tabbableNodes[0];
    } else {
        return tabbableNodes[tabIndex + 1];
    }
};

// returns previous element in the tab order before the given element
// params:
//   el: source element (defaults to current active element if not supplied)
//   region: parent node to limit tab order to (defaults to entire page if not supplied)
// returns:
//   previous focusable element if available (otherwise null)
const getPrevTab = function (el, region) {
    el = el || document.activeElement;
    region = region || document.body;
    var tabbableNodes = tabbable(region);

    if (!tabbableNodes.includes(el)) {
        return null;
    }

    var tabIndex = tabbableNodes.indexOf(el);

    if (tabIndex == 0) {
        return tabbableNodes[tabbableNodes.length - 1];
    } else {
        return tabbableNodes[tabIndex - 1];
    }
};

// NB: deprecated - use stepForwardInTabOrder() instead
const tabNext = function (el) {
    el = el || document.body;
    var tabbableNodes = tabbable(el);

    if (!tabbableNodes.includes(document.activeElement)) {
        return;
    }

    var tabIndex = tabbableNodes.indexOf(document.activeElement);

    if (tabIndex == tabbableNodes.length - 1) {
        tabbableNodes[0].focus();
    } else {
        tabbableNodes[tabIndex + 1].focus();
    }
};

// NB: deprecated - use stepBackwardInTabOrder() instead
const tabPrev = function (el) {
    el = el || document.body;
    var tabbableNodes = tabbable(el);

    if (!tabbableNodes.includes(document.activeElement)) {
        return;
    }

    var tabIndex = tabbableNodes.indexOf(document.activeElement);

    if (tabIndex == 0) {
        tabbableNodes[tabbableNodes.length - 1].focus();
    } else {
        tabbableNodes[tabIndex - 1].focus();
    }
};

// sets focus to the next element in the tab order after the given element
// params:
//   el: source element (defaults to current active element if not supplied)
//   region: parent node to limit tab order to (defaults to entire page if not supplied)
const stepForwardInTabOrder = function (el, region) {
    el = el || document.activeElement;
    region = region || document.body;
    var tabbableNodes = tabbable(region);

    if (!tabbableNodes.includes(el)) {
        return;
    }

    var tabIndex = tabbableNodes.indexOf(el);

    if (tabIndex == tabbableNodes.length - 1) {
        tabbableNodes[0].focus();
    } else {
        tabbableNodes[tabIndex + 1].focus();
    }
};

// sets focus to the previous element in the tab order before the given element
// params:
//   el: source element (defaults to current active element if not supplied)
//   region: parent node to limit tab order to (defaults to entire page if not supplied)
const stepBackwardInTabOrder = function (el, region) {
    el = el || document.activeElement;
    region = region || document.body;
    var tabbableNodes = tabbable(region);

    if (!tabbableNodes.includes(el)) {
        return;
    }

    var tabIndex = tabbableNodes.indexOf(el);

    if (tabIndex == 0) {
        tabbableNodes[tabbableNodes.length - 1].focus();
    } else {
        tabbableNodes[tabIndex - 1].focus();
    }
};

export { 
    tabbable, 
    focusable, 
    isTabbable, 
    isFocusable,
    getNextTab,
    getPrevTab,
    tabNext, // deprecated
    tabPrev, // deprecated
    stepForwardInTabOrder,
    stepBackwardInTabOrder
};