import Modal from 'UI/Modal';
import Loop from 'UI/Loop';
import Button from 'UI/Button';
// @ts-ignore
import faIconsRef from './faIcons.json';
import Input from 'UI/Input';
import Col from 'UI/Column';
import Loading from 'UI/Loading';
import Debounce from 'UI/Functions/Debounce';
import { getJson, ApiList } from 'UI/Functions/WebRequest';
import * as fileRef from 'UI/FileRef';
import { useState, useEffect, useRef } from 'react';

let icons: Icon[] = [];
let iconStyles: any[] = [];
let iconSets: any[] = [];

type IconSelectorProps = {
	/** Whether the icon selector modal is visible. */
	visible?: boolean;
	/** Called when an icon is selected, with the icon ref string (e.g. "fas:fa-home"). */
	onSelected?: (icon: string) => void;
	/** Called when the modal is closed. */
	onClose?: () => void;
};

type Icon = {
	/** Icon name, e.g. "arrow-right" or "github". */
	name: string;
	/** Available FontAwesome style variants, e.g. ["solid", "regular", "brands"]. */
	styles: string[];
	/** CSS class prefix override for custom icon sets (e.g. "myicons"). Absent for default FA icons. */
	prefix?: string;
	/** Custom icon set name. Absent for default FA icons. */
	set?: string;
};

type IconFile = Icon[];

export type CustomIconSet = {
	listRef: string,
	customSetName?: string,
	prefix?: string
};

export default function IconSelector(props: IconSelectorProps) {
	const [value, setValue] = useState<string | null>(null);
	const [styleFilter, setStyleFilter] = useState<string | undefined>(undefined);
	const [setFilter, setSetFilter] = useState<string | undefined>(undefined);
	const [searchFilter, setSearchFilter] = useState<string | undefined>(undefined);
	const [iconsLoaded, setIconsLoaded] = useState(icons.length > 0);
	const debounceRef = useRef(new Debounce((query: string) => {
		setSearchFilter(query.toLowerCase());
	}));

	useEffect(() => {
		if (!icons.length) {

			var styles = [{name: `All`, key: 'all'},{name:`Regular`, key: 'regular', prefix: 'far'}, {name:`Solid`, key: 'solid', prefix: 'fas'}, {name: `Brands`, key: 'brands', prefix: 'fab'}];
			var sets = [{name: `All`, key: 'all'}, {name: `Default (FontAwesome)`, key: 'default'}];

			var proms = [getJson<IconFile>(fileRef.getUrl((faIconsRef as any) as string)!)];

			if((window as any).customIcons){
				(window as any).customIcons.forEach((ci : CustomIconSet) => {
					proms.push(getJson<IconFile>(fileRef.getUrl(ci.listRef)!).then(response=>{
						response.forEach(icon => {
							if(ci.prefix){
								icon.prefix = ci.prefix;
							}

							icon.set = ci.customSetName || 'custom';
						});

						sets.push({name: ci.customSetName || 'custom', key: ci.customSetName || 'custom'});

						return response;
					}));

				});

			}

			Promise.all(proms).then(responses => {
				icons = [];
				responses.forEach(r => icons=icons.concat(r));
				iconStyles = styles;
				iconSets = sets;
				setIconsLoaded(true);
			});
		}
	}, []);

	function closeModal() {
		props.onClose && props.onClose();
	}

	function renderHeader(){
		return <div className="row header-container">
			<Col size="4">
				<label htmlFor="icon-style">
					{`Style`}
				</label>
				<Input type="select"
					name="icon-style"
					onChange={(e) => {
						setStyleFilter((e.target as HTMLSelectElement).value);
					}}
				>
					{iconStyles.map(s => <option value={s.key}>{s.name}</option>)}
				</Input>
			</Col>
			<Col size="4">
				<label htmlFor="icon-set">
					{`Set`}
				</label>
				<Input type="select"
					name="icon-set"
					onChange={(e) => {
						setSetFilter((e.target as HTMLSelectElement).value);
					}}
				>
					{iconSets.map(s => <option value={s.key}>{s.name}</option>)}
				</Input>
			</Col>
			<Col size="4">
				<label htmlFor="icon-search">
					{`Search`}
				</label>
				<Input type="text" value={searchFilter} name="icon-search" onKeyUp={(e) => {
					debounceRef.current.handle((e.target as HTMLInputElement).value);
				}}/>
			</Col>
		</div>;
	}

	var prefixForStyle: Record<string, string> = {};

	iconStyles.forEach(s => {
		prefixForStyle[s.key] = s.prefix;
	});

	return props.visible ? <div className="icon-selector">
		<Modal
			visible={true}
			onClose={() => closeModal()}
			isLarge
			className={"icon-select-modal"}
			title={`Select an Icon`}
		>
			{renderHeader()}
			<div className="icon-container">
				<Loop
					source={() => new Promise<ApiList<Icon>>((s, r) => s({results: icons} as ApiList<Icon>))}
					orNone={() => <Loading />}
				>
					{icon => {
						if (!searchFilter || icon.name.toLowerCase().includes(searchFilter) || icon.name.toLowerCase().replace(/-/g, " ").includes(searchFilter)) {
							return icon.styles.map(style => {

								if (styleFilter && styleFilter != "all") {
									if (styleFilter != style) {
										return null;
									}
								}

								if (setFilter && setFilter != "all") {
									if (setFilter == 'default') {
										if (icon.set) {
											return null;
										}
									} else if (setFilter != icon.set) {
										return null;
									}
								}

								var prefix = prefixForStyle[style];
								var readableName = icon.name.replace(/-/g, " ");
								var styleClass = "icon-tile__style icon-tile__style--" + style.toLowerCase();

								return <Button title={readableName} className="icon-tile" onClick={() => {
									var newIcon = prefix + ":" + (icon.prefix || "fa") + "-" + icon.name;

									setValue(newIcon);
									props.onSelected && props.onSelected(newIcon);
									closeModal();
								}}>
									<div className="icon-tile__preview">
										<i className={prefix + " " + (icon.prefix || "fa") + "-" + icon.name} />
										<span className={styleClass}>{style}</span>
									</div>
									<p className="icon-tile__name">{readableName}</p>
								</Button>
							})
						}
					}}
				</Loop>
			</div>
		</Modal>
	</div> : <></>;
}