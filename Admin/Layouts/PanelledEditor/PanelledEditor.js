import Input from 'UI/Input';
import { useSession, RouterConsumer } from 'UI/Session';

// left-hand tabs
var StructureEnum = {
	PAGE: 1
};

// right-hand tabs
var PropertiesEnum = {
	PAGE: 1,
	COMPONENT: 2
};

/**
 * Used to automatically generate forms used by the admin area based on fields from your entity declarations in the API.
 * To use this, use AutoService/ AutoController.
 * Most modules do this, so check any existing one for some examples.
 */


export default function PanelledEditor(props) {
	var { session, setSession } = useSession();
	return <PanelledEditorInternal {...props} session={session} setSession={setSession} />;
}

class PanelledEditorInternal extends React.Component {

	constructor(props) {
		super(props);
		
		this.state = {
			structureTab: StructureEnum.PAGE
		};
	}
	
	componentDidMount() {
		var html = document.querySelector("html");

		if (html) {
			html.classList.add("admin--page-editor");
		}
	}

	componentWillUnmount() {
		var html = document.querySelector("html");

		if (html) {
			html.classList.remove("admin--page-editor");
		}
    }

	capitalise(name) {
		return name && name.length ? name.charAt(0).toUpperCase() + name.slice(1) : "";
	}

	render() {
		if (this.props.name) {
			// Render as an input within some other form.
			return <div>
				<Input type='hidden' label={this.props.label} name={this.props.name} inputRef={ir => {
					this.ir = ir;
					if (ir) {
						ir.onGetValue = (val, ref) => {
							if (ref != this.ir) {
								return;
							}
							return JSON.stringify(this.state.value);
						};
					}
				}} />
				{this.renderFormFields()}
			</div>
		}

		return <RouterConsumer>{(pageState, setPage) => this.renderIntl(pageState, setPage)}</RouterConsumer>;
	}

	renderIntl(pageState, setPage) {
		
		var rightPanel = this.props.rightPanel && this.props.rightPanel();
		
		return (
			<div className="admin-page panelled-editor">
				{this.props.breadcrumbs && <header className="admin-page__subheader">
					<ul className="admin-page__breadcrumbs">
						{this.props.breadcrumbs}
					</ul>

					<div className="admin-page__supplemental">
						<div className="btn-group btn-group-sm admin-page__display-options" role="group" aria-label={`Display options`}>
							{!this.props.showSource && this.props.toggleLeftPanel && <>
								<input type="checkbox" className="btn-check" id="display_structure" autocomplete="off"
									onClick={(e) => this.props.toggleLeftPanel(!this.props.showLeftPanel)} defaultChecked={this.props.showLeftPanel} checked={this.props.showLeftPanel} />
								<label className="btn btn-outline-secondary" htmlFor="display_structure" title={`Toggle structure`}>
									<i className="fa fa-fw fa-list"></i>
								</label>
							</>}
							{this.props.onSetShowSource && <>
								<input type="checkbox" className="btn-check" id="display_source" autocomplete="off"
									onClick={(e) => this.props.onSetShowSource(!this.props.showSource)} defaultChecked={this.props.showSource} checked={this.props.showSource} />
								<label className={this.props.showSource ? "btn btn-outline-secondary admin-page__display-options--labelled" : "btn btn-outline-secondary"} htmlFor="display_source" title={`Toggle source view`}>
									<i className="fa fa-fw fa-code"></i>
									{this.props.showSource && <span className="admin-page__display-options-label">
										{`Return to preview`}
									</span>}
								</label>
							</>}
							{!this.props.showSource && this.props.toggleRightPanel && <>
								<input type="checkbox" className="btn-check" id="display_props" autocomplete="off"
									onClick={(e) => this.props.toggleRightPanel(!this.props.showRightPanel)} defaultChecked={this.props.showRightPanel} checked={this.props.showRightPanel} />
								<label className="btn btn-outline-secondary" htmlFor="display_props" title={`Toggle properties`}>
									<i className="fa fa-fw fa-cog"></i>
								</label>
							</>}
						</div>
					</div>
				</header>}
				{this.props.feedback && <>
					<footer className="admin-page__feedback">
						{this.props.feedback}
					</footer>
				</>}
				<div className="panelled-editor__content-wrapper">
					<div className="panelled-editor__content">
						{/* Left panel */}
						<div className={this.props.showLeftPanel ? "panelled-editor__structure" : "panelled-editor__structure panelled-editor__structure--hidden"}>
							<ul className="panelled-editor__structure-tabs">
								{this.props.leftPanelTitle && <li className={this.state.structureTab == StructureEnum.PAGE ?
									"panelled-editor__structure-tab panelled-editor__structure-tab--page panelled-editor__structure-tab--active" : "panelled-editor__structure-tab panelled-editor__structure-tab--page"}>
									<button type="button" className="btn" onClick={() => this.setState({ structureTab: StructureEnum.PAGE })}>
										{this.props.leftPanelTitle}
									</button>
								</li>}
								{this.props.toggleLeftPanel && <li class="panelled-editor__structure-tab panelled-editor__structure-tab--close">
									<button type="button" class="btn" onClick={() => this.props.toggleLeftPanel(false)}>
										<i class="fal fa-times"></i>
									</button>
								</li>}
							</ul>
							{this.state.structureTab == StructureEnum.PAGE && <>
								<div className="panelled-editor__structure-tab-content">
									<ul className="panelled-editor__structure-items">
										{this.props.leftPanel && this.props.leftPanel()}
									</ul>
								</div>
							</>}
						</div>
						{/* Main panel */}
						<div className="panelled-editor__preview">
							{this.props.children}
						</div>
						{/* Selected entity properties */}
						<div className={this.props.showRightPanel ? "panelled-editor__properties" : "panelled-editor__properties panelled-editor__properties--hidden"}>
							<ul className="panelled-editor__property-tabs">
								<li className={this.props.propertyTab == PropertiesEnum.PAGE ?
									"panelled-editor__property-tab panelled-editor__property-tab--page panelled-editor__property-tab--active" : "panelled-editor__property-tab panelled-editor__property-tab--page"}>
									<button type="button" className="btn" onClick={() => this.props.changeRightTab(PropertiesEnum.PAGE)}>
										{this.props.additionalFieldsTitle}
									</button>
								</li>
								{this.props.rightPanelTitle && <li className={this.props.propertyTab == PropertiesEnum.COMPONENT ?
									"panelled-editor__property-tab panelled-editor__property-tab--component panelled-editor__property-tab--active" : "panelled-editor__property-tab panelled-editor__property-tab--component"}>
									<button type="button" className="btn" onClick={() => this.props.changeRightTab(PropertiesEnum.COMPONENT)} disabled={!rightPanel}>
										{this.props.rightPanelTitle}
									</button>
								</li>}
								{this.props.toggleRightPanel && <li class="panelled-editor__property-tab panelled-editor__property-tab--close">
									<button type="button" class="btn" onClick={() => this.props.toggleRightPanel(false)}>
										<i class="fal fa-times"></i>
									</button>
								</li>}
							</ul>
							{/* page properties - importantly, these are always present in the DOM (such that if you're looking at component properties and hit save and publish, they are submitted), 
							 but may simply be display:none */}
							{this.props.additionalFields && <>
								<div className="panelled-editor__property-tab-content" style={this.props.propertyTab != PropertiesEnum.PAGE ? {display: 'none'} : undefined }>
									{this.props.additionalFields()}
								</div>
							</>}

							{/* component properties */}
							{this.props.propertyTab == PropertiesEnum.COMPONENT && <>
								<div className="panelled-editor__property-tab-content">
									{rightPanel}
								</div>
							</>}

						</div>
					</div>
				</div>
				{this.props.controls && <>
					<footer className="admin-page__footer">
						{this.props.controls}
					</footer>
				</>}
				{/*this.props.controls && <Tile className="xpanelled-editor-footer" fixedFooter>
					{this.props.controls}
				</Tile>*/}
			</div>
		);
	}
}