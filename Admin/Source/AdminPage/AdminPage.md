# Admin/AdminPage

```tsx
import AdminPage from 'Admin/AdminPage';
import Footer from 'Admin/Footer';
```

A collection of components dedicated to defining the structure of an admin page.

# Usage Examples

## Default

```tsx

	<AdminPage.SubHeader title={`Dashboard`} breadcrumbs={[
			{
				title: `Dashboard`
			}
		]}>
		{/* content added here will appear right-aligned in the subheader */}
	</AdminPage.SubHeader>
	<AdminPage.ContentWrapper>
		<AdminPage.Filters>
			{/* sidebar to hold filtering options */}
		</AdminPage.Filters>
		<AdminPage.Content>
			{/* optional notice */}
			<AdminPage.Notice title={`Please Note`} notice={`Notice message content`} />
			{/* main page content (scrollable) */}
		</AdminPage.Content>
	</AdminPage.ContentWrapper>
	<AdminPage.Feedback variant="success">
		Feedback message content
	</AdminPage.Feedback>
	<Footer>
		<Footer.BulkActions>
			{/* left-aligned options, typically bulk actions, e.g. Delete selected items */}
		</Footer.BulkActions>
		<Footer.CallsToAction>
			{/* right-aligned options, typically main calls to action, e.g. Save / Create */}
		</Footer.CallsToAction>
	</Footer>

```

## SubHeader

Supports:

- Breadcrumbs
- Title
- Subtitle (optional)

Additional children supplied are rendered right-aligned; best to keep this to a minimum as we need to consider smaller displays 
where wrapping / clipping may be an issue.


## ContentWrapper

Defines the main content area, along with space for a left-aligned filters bar, if required.


## Filters

Defines a left-aligned scrollable region designed to hold filters relating to the current view. To display a consistent search field, 
supply the following props:

- searchText: string
- onInput: () => void
- onChange: () => void


## Notice

Optional styled notice panel designed for use as the first child in AdminPage.Content (UI/Alert can also use be used for this).


## Content

The main scrollable content region for the page.


## Feedback

Optional feedback area (remains docked below content and above footer, unaffected by scrolling).  Uses variant prop as with UI/Alert.


## Footer

Docked footer region; content is automatically right-aligned, unless added to a <Footer.BulkActions> child region (left-aligned).


# Admin Template Structure

Admin templates typically inject these components into a parent <AdminPage> component (along with an <AdminHeader>), as shown below
in this excerpt from Admin/Templates/BaseAdminTemplate:

```tsx
		<div className="admin-page">
			<AdminHeader />
			<AdminPage>
				{children}
			</AdminPage>
		</div>
```


# Custom pages

AdminPage accepts custom components either added standalone or mixed with header / footer components, as shown below (in each case,
the custom component automatically inherits vertical overflow scrolling):

## Standalone

```tsx
	<MyComponent />
```

## With a consistent subheader

```tsx
	{/* automatically inherits vertical overflow scrolling as the sole child of AdminPage */}
	<AdminPage.SubHeader title={`My Title`} breadcrumbs={[
			{
				title: `My Title`
			}
		]} />
	<MyComponent />
```


## With a consistent footer

```tsx
	{/* automatically inherits vertical overflow scrolling as the sole child of AdminPage */}
	<MyComponent />
	<Footer>
		<Link href={myUrl}>
			Continue
		</Link>
	</Footer>
```


## With a consistent subheader and footer

```tsx
	{/* automatically inherits vertical overflow scrolling as the sole child of AdminPage */}
	<AdminPage.SubHeader title={`My Title`} breadcrumbs={[
			{
				title: `My Title`
			}
		]} />
	<MyComponent />
	<Footer>
		<Link href={myUrl}>
			Continue
		</Link>
	</Footer>
```
