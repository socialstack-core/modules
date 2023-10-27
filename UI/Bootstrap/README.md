# Bootstrap 5.1.3

This is a modular version of Bootstrap based on the SASS source available from [www.getbootstrap.com](https://getbootstrap.com/docs/5.1/getting-started/download/).  Core styling has been separated out, with styling for components (such as Alert, Modal, etc) moved to each individual submodule.


## Structure

The following filename changes have been made:

```
_functions.scss
global.functions.1.scss

_variables.scss
global.variables.2.scss

_mixins.scss (removed)

vendor/rfs
vendor/global.rfs.3.scss

_utilities.scss
global.utilities.20.scss

root
root.21.scss

reboot
reboot.22.scss

images
images.24.scss

grid
grid.25.scss

tables.
tables.26.scss

transitions
transitions.40.scss

placeholders
placeholders.50.scss

forms/[name]
[forms/[name].30.scss]

helpers/[name]
[helpers/[name].60.scss]

utilities/api
utilities/api.70.scss

mixins/[name]
[mixins/global.[name].10.scss]
```

Please note, the following mixins have been moved to the corresponding submodule (e.g. `mixins/alert` can now be found within `UI/Source/ThirdParty/Alert`):

```
mixins/alert
mixins/buttons
mixins/container
mixins/pagination
```

The `type` file has also undergone some changes:

* renamed to `type.23.scss`
* `@extend` references replaced with copies of the original styles from `reboot.22.scss`


## Modular Components

The following Bootstrap components are available as shared submodules (use `socialstack i UI/[ModuleName]` to add to your project):

* Accordion (TODO)
* Alert
* Badge
* BootstrapCarousel (WIP)
* BootstrapSpinner
* Breadcrumb (WIP)
* Button (WIP)
* ButtonGroup (TODO)
* Card (TODO)
* CloseButton
* Column
* Container
* Dropdown
* ListGroup (TODO)
* Modal (WIP)
* Nav (TODO)
* NavBar (TODO)
* OffCanvas (TODO)
* Paginator (WIP)
* Popover (TODO)
* Progress
* Row
* Toast (TODO)
* Tooltip (TODO)
