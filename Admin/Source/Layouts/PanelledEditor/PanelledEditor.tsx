import { useState, useEffect, RefObject } from 'react';
import Input from 'UI/Input';
import Button from 'UI/Button';
import SubHeader, { Breadcrumb } from 'Admin/SubHeader';
import { useSession } from 'UI/Session';

// Left-hand tabs
enum StructureEnum {
  PAGE = 1,
}

// Right-hand tabs
enum PropertiesEnum {
  PAGE = 1,
  COMPONENT = 2,
}

type RefObjectGetValue = RefObject<HTMLInputElement> & {
    onGetValue: Function
}

/**
 * Used to automatically generate forms used by the admin area based on fields from your entity declarations in the API.
 * To use this, use AutoService/ AutoController.
 * Most modules do this, so check any existing one for some examples.
 */
type PanelledEditorProps = {
  title: string;
  leftPanelTitle?: string;
  rightPanelTitle?: string;
  breadcrumbs?: Breadcrumb[];
  primaryUrl?: string;
  showLeftPanel: boolean;
  showRightPanel: boolean;
  showSource: boolean;
  toggleLeftPanel: (state: boolean) => void;
  toggleRightPanel: (state: boolean) => void;
  onSetShowSource?: (state: boolean) => void;
  leftPanel?: () => React.ReactNode;
  rightPanel?: () => React.ReactNode;
  controls?: React.ReactNode;
  feedback?: React.ReactNode;
  children?: React.ReactNode;
  propertyTab: PropertiesEnum;
  changeRightTab: (tab: PropertiesEnum) => void;
  graphState?: boolean;
  name?: string;
  label?: string;
  session?: SessionResponse
};

const PanelledEditorInternal: React.FC<PanelledEditorProps> = (props: PanelledEditorProps): React.ReactNode => {
  const [structureTab, setStructureTab] = useState<StructureEnum>(StructureEnum.PAGE);
  const { session, setSession } = useSession();

  useEffect(() => {
    const html = window.SERVER ? undefined : document.querySelector("html");

    if (html) {
      html.classList.add("admin--page-editor");
    }

    return () => {
      if (html) {
        html.classList.remove("admin--page-editor");
      }
    };
  }, []);

  const capitalise = (name: string) => (name && name.length ? name.charAt(0).toUpperCase() + name.slice(1) : "");

  if (props.name) {
    // Render as an input within some other form.
    return (
      <div>
        <Input
          type="hidden"
          label={props.label}
          name={props.name}
          onInputRef={(ir: HTMLElement) => {
              if (ir) {
                // @ts-ignore
              ir.onGetValue = (val: any, ref: HTMLInputElement) => {
                if (ref !== ir) {
                  return;
                }
                return JSON.stringify({}); // Replace with actual value logic
              };
            }
          }}
        />
        {props.children}
      </div>
    );
  }

  const rightPanel = props.rightPanel && props.rightPanel();
  const editorClass = ["admin-page", "panelled-editor"];

  if (props.graphState) {
    editorClass.push("panelled-editor--graph");
  }

  return (
    <div className={editorClass.join(" ")}>
      {props.breadcrumbs && (
		<SubHeader title={props.title} breadcrumbs={props.breadcrumbs} primaryUrl={props.primaryUrl}>
          <div className="admin-page__supplemental">
            <div className="btn-group btn-group-sm admin-page__display-options" role="group" aria-label={`Display options`}>
              {!props.showSource && props.toggleLeftPanel && (
                <>
                  <input
                    type="checkbox"
                    className="btn-check"
                    id="display_structure"
                    autoComplete="off"
                    onClick={(e) => props.toggleLeftPanel(!props.showLeftPanel)}
                    defaultChecked={props.showLeftPanel}
                    checked={props.showLeftPanel}
                  />
                  <label className="btn btn-outline-secondary" htmlFor="display_structure" title={`Toggle structure`}>
                    <i className="fa fa-fw fa-list"></i>
                    <span className="admin-page__display-options-label">Structure</span>
                  </label>
                </>
              )}
              {props.onSetShowSource && (
                <>
                  <input
                    type="checkbox"
                    className="btn-check"
                    id="display_source"
                    autoComplete="off"
                    onClick={(e) => props.onSetShowSource && props.onSetShowSource(!props.showSource)}
                    defaultChecked={props.showSource}
                    checked={props.showSource}
                  />
                  <label
                    className={props.showSource ? "btn btn-outline-secondary admin-page__display-options--labelled" : "btn btn-outline-secondary"}
                    htmlFor="display_source"
                    title={`Toggle source view`}
                  >
                    <i className="fa fa-fw fa-code"></i>
                    <span className="admin-page__display-options-label">
                      {props.showSource ? `Return to preview` : `Source`}
                    </span>
                  </label>
                </>
              )}
              {!props.showSource && props.toggleRightPanel && (
                <>
                  <input
                    type="checkbox"
                    className="btn-check"
                    id="display_props"
                    autoComplete="off"
                    onClick={(e) => props.toggleRightPanel(!props.showRightPanel)}
                    defaultChecked={props.showRightPanel}
                    checked={props.showRightPanel}
                  />
                  <label className="btn btn-outline-secondary" htmlFor="display_props" title={`Toggle properties`}>
                    <i className="fa fa-fw fa-cog"></i>
                    <span className="admin-page__display-options-label">Properties</span>
                  </label>
                </>
              )}
            </div>
          </div>
        </SubHeader>
      )}
      <div className="panelled-editor__content-wrapper">
        <div className="panelled-editor__content">
          {/* Left panel */}
          <div className={props.showLeftPanel ? "panelled-editor__structure" : "panelled-editor__structure panelled-editor__structure--hidden"}>
            <ul className="panelled-editor__structure-tabs">
              {props.leftPanelTitle && (
                <li
                  className={
                    structureTab === StructureEnum.PAGE
                      ? "panelled-editor__structure-tab panelled-editor__structure-tab--page panelled-editor__structure-tab--active"
                      : "panelled-editor__structure-tab panelled-editor__structure-tab--page"
                  }
                >
                  <Button onClick={() => setStructureTab(StructureEnum.PAGE)}>
                    {props.leftPanelTitle}
                  </Button>
                </li>
              )}
              {props.toggleLeftPanel && (
                <li className="panelled-editor__structure-tab panelled-editor__structure-tab--close">
                  <Button onClick={() => props.toggleLeftPanel(false)}>
                    <i className="fal fa-times"></i>
                  </Button>
                </li>
              )}
            </ul>
            {structureTab === StructureEnum.PAGE && (
              <div className="panelled-editor__structure-tab-content">
                <ul className="panelled-editor__structure-items">{props.leftPanel && props.leftPanel()}</ul>
              </div>
            )}
          </div>
          {/* Main panel */}
          <div className="panelled-editor__preview">{props.children}</div>
          {/* Selected entity properties */}
          <div className={props.showRightPanel ? "panelled-editor__properties" : "panelled-editor__properties panelled-editor__properties--hidden"}>
            <ul className="panelled-editor__property-tabs">
              {props.toggleRightPanel && (
                <li className="panelled-editor__property-tab panelled-editor__property-tab--close">
                  <Button onClick={() => props.toggleRightPanel(false)}>
                    <i className="fal fa-times"></i>
                  </Button>
                </li>
              )}
            </ul>
              <div className="panelled-editor__property-tab-content">{rightPanel}</div>
          </div>
        </div>
      </div>
      {props.feedback && (
        <footer className="admin-page__feedback">
          {props.feedback}
        </footer>
      )}
      {props.controls && (
        <footer className="admin-page__footer">
          {props.controls}
        </footer>
      )}
    </div>
  );
};

const PanelledEditor: React.FC<PanelledEditorProps> = (props: PanelledEditorProps) => {
  return <PanelledEditorInternal {...props} />;
};

export default PanelledEditor;
