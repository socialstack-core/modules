# HuddleChat


## Theming

The following CSS variables can be defined in order to override the appearance of the chat UI (example values shown are current defaults):

```
:root {
	--huddle-bg: rgb(10,10,10);
	--huddle-font-family: var(--font);
	--huddle-font-weight: var(--bs-body-font-weight);
    --huddle-sidebar-bg: #eee;
    --huddle-sidebar-header-bg: transparent;
    --huddle-sidebar-header-fg: currentColor;
    --huddle-sidebar-header-font-size: 14px;
    --huddle-sidebar-footer-bg: transparent;
    --huddle-sidebar-footer-fg: currentColor;
    --huddle-audience-member-bg: #444;
    --huddle-user-bg: rgba(255,255,255, .25);
    --huddle-user-header-bg: linear-gradient(0, transparent, rgba(0,0,0,.1));
    --huddle-user-footer-bg: linear-gradient(0, rgba(0,0,0,.1), transparent);
    --huddle-user-footer-bg-active: linear-gradient(0, rgba(0, 0, 0, 0.75) 25%, transparent);
    --huddle-user-text-shadow: 2px 2px rgb(0,0,0,.85);
    --huddle-user-options-border: 1px solid rgba(255,255,255,.5);
    --huddle-user-gone-bg: rgba(255, 255, 255, 0.35);
    --huddle-user-active-box-shadow: 0 0 10px 6px var(--info);
    --huddle-stage-fg: #fff;
    --huddle-pinned-bg: #ccc;
    --huddle-pinned-user-bg: #444;
    --huddle-pinned-user-fg: #fff;
    --huddle-footer-bg: transparent;
    --huddle-footer-fg: rgba(0,0,0, .75);
    --huddle-header-bg: transparent;
    --huddle-header-fg: #fff;
    --huddle-header-title-font-family: var(--font);
    --huddle-header-title-font-weight: bold;
    --huddle-header-title-font-size: 24px;
    --huddle-header-description-font-size: 14px;
    --huddle-header-btn-bg: transparent;
    --huddle-header-btn-fg: #fff;
    --huddle-header-btn-bg-hover: #444;
    --huddle-header-btn-fg-hover: #fff;
    --huddle-header-btn-bg-active: var(--primary);
    --huddle-header-btn-fg-active: var(--primary-fg);
}
```
