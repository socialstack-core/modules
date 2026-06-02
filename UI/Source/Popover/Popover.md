# UI/Popover

```tsx
import Popover from 'UI/Popover';
```

Handles rendering a panel using the popover API.

# Usage Examples

Note, a unique ID is required - visibility of the popover can then be controlled without reliance on JavaScript
by including a trigger somewhere (either a button or a link) with a corresponding popovertarget attribute set
to the same ID.

## Default

```tsx

	{/* import either UI/Button or UI/Link to use as a trigger */}
	<Button popoverTarget="admin_menu">
		{`Menu`}
	</Button>
	<Popover id="admin_menu">
		{/* popover content */}
	</Popover>

```

## Advanced

A more advanced use case is using the popover API to support a panel which should be visible all the time,
except at lower resolutions where having it temporarily pop up as needed makes more sense.  A good example
of this is a page of search results which has a sidebar of associated filters - there's probably room for
this at larger resolutions (such as tablet landscape and desktop), but no room at mobile resolution.

```tsx

	<PopoverWrapper>
		{/* note, the associated trigger doesn't need to be a) before the popover or b) even a sibling,
		    but it does need to be parented by the wrapper component.  Styling will ensure the trigger
			is automatically hidden if there's enough room to display the associated popover
		*/}
		<Button popoverTarget="search_filters">
			{`Filters`}
		</Button>
		
		{/* the component supports the following props:
		  - tabletPortraitVisible: docks the popover panel in / hides trigger @ 753px and above
		  - tabletLandscapeVisible: docks the popover panel in / hides trigger @ 1024px and above
		  - desktopVisible: docks the popover panel in / hides trigger @ 1360px and above
		*/}
		<Popover id="search_filters" tabletLandscapeVisible>
			{/* search filters content */}
		</Popover>
	</PopoverWrapper>

```

One caveat to the above - when using the PopoverWrapper, ensure there's only one popover within the content
(otherwise _all_ popovers within that block will be affected).
