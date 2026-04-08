import Button from 'UI/Button';
import Link from 'UI/Link';
import Input from 'UI/Input';

/**
 * Props for the ButtonTest component.
 */
interface ButtonTestProps {
	/**
	 * An example optional fileRef prop.
	 */
	// logoRef?: FileRef
}

/**
 * The ButtonTest React component.
 * @param props React props.
 */
const ButtonTest: React.FC<ButtonTestProps> = (props) => {
	return (
		<div className="ui-button-test">

			{/* buttons */}
			<section>
				<label className="name">Extra small</label>
				<Button xs>
					Button primary
				</Button>
				<Button variant="secondary" xs outlined>
					Button secondary
				</Button>
				<Button variant="secondary" xs outlined>
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Button with icon
				</Button>
				<Button xs outlined className="btn--next">
					Button with arrow next
					<i className="fr fr-arrow-right"></i>
				</Button>
				<Button xs outlined className="btn--next" disabled>
					Disabled button
					<i className="fr fr-arrow-right"></i>
				</Button>
			</section>

			<section>
				<label className="name">Small</label>
				<Button sm>
					Button primary
				</Button>
				<Button variant="secondary" sm outlined>
					Button secondary
				</Button>
				<Button variant="secondary" sm outlined>
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Button with icon
				</Button>
				<Button sm outlined className="btn--next">
					Button with arrow next
					<i className="fr fr-arrow-right"></i>
				</Button>
				<Button sm outlined className="btn--next" disabled>
					Disabled button
					<i className="fr fr-arrow-right"></i>
				</Button>
			</section>

			<section>
				<label className="name">Regular</label>
				<Button>
					Button primary
				</Button>
				<Button variant="secondary" outlined>
					Button secondary
				</Button>
				<Button variant="secondary" outlined>
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Button with icon
				</Button>
				<Button outlined className="btn--next">
					Button with arrow next
					<i className="fr fr-arrow-right"></i>
				</Button>
				<Button outlined className="btn--next" disabled>
					Disabled button
					<i className="fr fr-arrow-right"></i>
				</Button>
			</section>

			<section>
				<label className="name">Large</label>
				<Button lg>
					Button primary
				</Button>
				<Button variant="secondary" lg outlined>
					Button secondary
				</Button>
				<Button variant="secondary" lg outlined>
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Button with icon
				</Button>
				<Button lg outlined className="btn--next">
					Button with arrow next
					<i className="fr fr-arrow-right"></i>
				</Button>
				<Button lg outlined className="btn--next" disabled>
					Disabled button
					<i className="fr fr-arrow-right"></i>
				</Button>
			</section>

			<section>
				<label className="name">Extra Large</label>
				<Button xl>
					Button primary
				</Button>
				<Button variant="secondary" xl outlined>
					Button secondary
				</Button>
				<Button variant="secondary" xl outlined>
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Button with icon
				</Button>
				<Button xl outlined className="btn--next">
					Button with arrow next
					<i className="fr fr-arrow-right"></i>
				</Button>
				<Button xl outlined className="btn--next" disabled>
					Disabled button
					<i className="fr fr-arrow-right"></i>
				</Button>
			</section>

			<hr/>

			{/* links */}
			<section>
				<label className="name">Extra small</label>
				<Link xs href="#">
					Standard link
				</Link>
				<Link variant="primary" xs href="#">
					Link primary
				</Link>
				<Link variant="secondary" xs outlined href="#">
					Link secondary
				</Link>
				<Link variant="secondary" xs outlined href="#">
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Link with icon
				</Link>
				<Link xs outlined className="btn--next" href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link xs href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link xs outlined className="btn--next" href="#" disabled>
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link xs href="#" disabled>
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
			</section>

			<section>
				<label className="name">Small</label>
				<Link sm href="#">
					Standard link
				</Link>
				<Link variant="primary" sm href="#">
					Link primary
				</Link>
				<Link variant="secondary" sm outlined href="#">
					Link secondary
				</Link>
				<Link variant="secondary" sm outlined href="#">
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Link with icon
				</Link>
				<Link sm outlined className="btn--next" href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link sm href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link sm outlined className="btn--next" disabled href="#">
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link sm disabled href="#">
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
			</section>

			<section>
				<label className="name">Regular</label>
				<Link href="#">
					Standard link
				</Link>
				<Link variant="primary" href="#">
					Link primary
				</Link>
				<Link variant="secondary" outlined href="#">
					Link secondary
				</Link>
				<Link variant="secondary" outlined href="#">
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Link with icon
				</Link>
				<Link outlined className="btn--next" href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link outlined className="btn--next" disabled href="#">
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link disabled href="#">
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
			</section>

			<section>
				<label className="name">Large</label>
				<Link lg href="#">
					Standard link
				</Link>
				<Link variant="primary" lg href="#">
					Link primary
				</Link>
				<Link variant="secondary" lg outlined href="#">
					Link secondary
				</Link>
				<Link variant="secondary" lg outlined href="#">
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Link with icon
				</Link>
				<Link lg outlined className="btn--next" href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link lg href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link lg outlined className="btn--next" disabled href="#">
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link lg disabled href="#">
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
			</section>

			<section>
				<label className="name">Extra Large</label>
				<Link xl href="#">
					Standard link
				</Link>
				<Link variant="primary" xl href="#">
					Link primary
				</Link>
				<Link variant="secondary" xl outlined href="#">
					Link secondary
				</Link>
				<Link variant="secondary" xl outlined href="#">
					<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 23 23" fill="none">
						<path d="M5.45938 19.2407H18.2088C20.7223 19.2407 22.7622 17.2008 22.7622 14.6874V8.31264C22.7622 5.79917 20.7223 3.75926 18.2088 3.75926H5.45938C2.94592 3.75926 0.906006 5.79917 0.906006 8.31264V14.6874C0.906006 17.2008 2.94592 19.2407 5.45938 19.2407ZM18.2088 17.4194H5.45938C3.95677 17.4194 2.72736 16.19 2.72736 14.6874V11.0447H20.9409V14.6874C20.9409 16.19 19.7115 17.4194 18.2088 17.4194ZM2.72736 8.31264C2.72736 6.81002 3.95677 5.58061 5.45938 5.58061H18.2088C19.7115 5.58061 20.9409 6.81002 20.9409 8.31264V9.22331H2.72736V8.31264Z" fill="currentColor" />
						<path d="M18.2088 13.7767H16.3874C15.8866 13.7767 15.4768 14.1865 15.4768 14.6873C15.4768 15.1882 15.8866 15.598 16.3874 15.598H18.2088C18.7097 15.598 19.1195 15.1882 19.1195 14.6873C19.1195 14.1865 18.7097 13.7767 18.2088 13.7767Z" fill="currentColor" />
					</svg>
					Link with icon
				</Link>
				<Link xl outlined className="btn--next" href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link xl href="#">
					<span>Link with arrow next</span>
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link xl outlined className="btn--next" disabled href="#">
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
				<Link xl disabled href="#">
					Disabled link
					<i className="fr fr-arrow-right"></i>
				</Link>
			</section>

			<hr />

			{/* checkboxes */}
			<section>
				<label className="name">Checkboxes</label>
				<Input type="checkbox" xs label={`Extra small checkbox`} noWrapper />
				<Input type="checkbox" xs label={`Extra small checked checkbox`} checked noWrapper />
				<Input type="checkbox" xs label={`Extra small disabled checkbox`} disabled noWrapper />
				<Input type="checkbox" xs label={`Extra small disabled checked checkbox`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="checkbox" sm label={`Small checkbox`} noWrapper />
				<Input type="checkbox" sm label={`Small checked checkbox`} checked noWrapper />
				<Input type="checkbox" sm label={`Small disabled checkbox`} disabled noWrapper />
				<Input type="checkbox" sm label={`Small disabled checked checkbox`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="checkbox" label={`Default checkbox`} noWrapper />
				<Input type="checkbox" label={`Checked checkbox`} checked noWrapper />
				<Input type="checkbox" label={`Disabled checkbox`} disabled noWrapper />
				<Input type="checkbox" label={`Disabled checked checkbox`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="checkbox" lg label={`Large checkbox`} noWrapper />
				<Input type="checkbox" lg label={`Large checked checkbox`} checked noWrapper />
				<Input type="checkbox" lg label={`Large disabled checkbox`} disabled noWrapper />
				<Input type="checkbox" lg label={`Large disabled checked checkbox`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="checkbox" xl label={`Extra large checkbox`} noWrapper />
				<Input type="checkbox" xl label={`Extra large checked checkbox`} checked noWrapper />
				<Input type="checkbox" xl label={`Extra large disabled checkbox`} disabled noWrapper />
				<Input type="checkbox" xl label={`Extra large disabled checked checkbox`} checked disabled noWrapper />
			</section>

			<hr />

			{/* switches */}
			<section>
				<label className="name">Switches</label>
				<Input type="checkbox" xs isSwitch flipped label={`Extra small switch`} noWrapper />
				<Input type="checkbox" xs isSwitch flipped label={`Extra small checked switch`} checked noWrapper />
				<Input type="checkbox" xs isSwitch flipped label={`Extra small disabled switch`} disabled noWrapper />
				<Input type="checkbox" xs isSwitch flipped label={`Extra small disabled switch`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="checkbox" sm isSwitch flipped label={`Small switch`} noWrapper />
				<Input type="checkbox" sm isSwitch flipped label={`Small checked switch`} checked noWrapper />
				<Input type="checkbox" sm isSwitch flipped label={`Small disabled switch`} disabled noWrapper />
				<Input type="checkbox" sm isSwitch flipped label={`Small disabled switch`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="checkbox" isSwitch flipped label={`Switch`} noWrapper />
				<Input type="checkbox" isSwitch flipped label={`Checked switch`} checked noWrapper />
				<Input type="checkbox" isSwitch flipped label={`Disabled switch`} disabled noWrapper />
				<Input type="checkbox" isSwitch flipped label={`Disabled switch`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="checkbox" lg isSwitch flipped label={`Large switch`} noWrapper />
				<Input type="checkbox" lg isSwitch flipped label={`Large checked switch`} checked noWrapper />
				<Input type="checkbox" lg isSwitch flipped label={`Large disabled switch`} disabled noWrapper />
				<Input type="checkbox" lg isSwitch flipped label={`Large disabled switch`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="checkbox" xl isSwitch flipped label={`Extra large switch`} noWrapper />
				<Input type="checkbox" xl isSwitch flipped label={`Extra large checked switch`} checked noWrapper />
				<Input type="checkbox" xl isSwitch flipped label={`Extra large disabled switch`} disabled noWrapper />
				<Input type="checkbox" xl isSwitch flipped label={`Extra large disabled switch`} checked disabled noWrapper />
			</section>

			<hr />

			{/* radio buttons */}
			<section>
				<label className="name">Radio buttons</label>
				<Input type="radio" xs name="xs-radios" label={`Extra small radio`} noWrapper />
				<Input type="radio" xs name="xs-radios" label={`Extra small checked radio`} checked noWrapper />
				<Input type="radio" xs name="xs-disabled-radios" label={`Extra small disabled radio`} disabled noWrapper />
				<Input type="radio" xs name="xs-disabled-radios" label={`Extra small disabled checked radio`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="radio" sm name="sm-radios" label={`Small radio`} noWrapper />
				<Input type="radio" sm name="sm-radios" label={`Small checked radio`} checked noWrapper />
				<Input type="radio" sm name="sm-disabled-radios" label={`Small disabled radio`} disabled noWrapper />
				<Input type="radio" sm name="sm-disabled-radios" label={`Small disabled checked radio`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="radio" name="radios" label={`Radio`} noWrapper />
				<Input type="radio" name="radios" label={`Checked radio`} checked noWrapper />
				<Input type="radio" name="disabled-radios" label={`Disabled radio`} disabled noWrapper />
				<Input type="radio" name="disabled-radios" label={`Disabled checked radio`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="radio" lg name="lg-radios" label={`Large radio`} noWrapper />
				<Input type="radio" lg name="lg-radios" label={`Large checked radio`} checked noWrapper />
				<Input type="radio" lg name="lg-disabled-radios" label={`Large disabled radio`} disabled noWrapper />
				<Input type="radio" lg name="lg-disabled-radios" label={`Large disabled checked radio`} checked disabled noWrapper />
			</section>
			<section>
				<Input type="radio" xl name="xl-radios" label={`Extra large radio`} noWrapper />
				<Input type="radio" xl name="xl-radios" label={`Extra large checked radio`} checked noWrapper />
				<Input type="radio" xl name="xl-disabled-radios" label={`Extra large disabled radio`} disabled noWrapper />
				<Input type="radio" xl name="xl-disabled-radios" label={`Extra large disabled checked radio`} checked disabled noWrapper />
			</section>
		</div>
	);
}

export default ButtonTest;