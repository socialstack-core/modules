# UI/Tabs

```tsx
import Tabs from 'UI/Tabs';
```

# Usage Examples

## Default

```tsx
<TabSet name="months">
	<Tab label={`Jan`}>
		<p>
			Lorem ipsum dolor sit amet, consectetur adipisicing elit. Consequatur quis nihil delectus obcaecati unde ad quas itaque, 
			quaerat sint tempore corporis in voluptatum consequuntur earum cum aliquid facere. Et, accusantium!
		</p>
	</Tab>
</TabSet>


<Tabs variant="success" title={`My alert`} dismissable className="my-custom-class">
	<p>
		Lorem ipsum dolor sit amet, consectetur adipisicing elit. Consequatur quis nihil delectus obcaecati unde ad quas itaque, 
		quaerat sint tempore corporis in voluptatum consequuntur earum cum aliquid facere. Et, accusantium!
	</p>
</Tabs>
```