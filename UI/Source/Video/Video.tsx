import { getUrl } from 'UI/FileRef';
import * as React from 'react';

/**
 * Used to display a video from a fileRef. Can be e.g. youtube:VideoID
 * Min required props: fileRef.
 * <Video fileRef='public:2.mp4'/>
 * 
 * @icon fal fa-video
 * @description Displays a single video.
 */
export interface VideoProps {
	fileRef?: string | object,
	width?: number | string,
	height?: number | string,
	autoHeight?: boolean,
	autoplay?: boolean,
	size?: string
}

const Video: React.FC<VideoProps> = (props) => {
	var ref = props.fileRef as string;
	var width = props.width || 560;
	var autoHeight = props.autoHeight;
	var height = autoHeight ? "auto" : props.height || 315;
	var autoplay = props.autoplay;
	
	if (!ref) {
		return (<div style={{width, height, backgroundColor: 'grey', color: 'white', textAlign: 'center', display: 'inline-block'}}>
			<div style={{margin: '10px'}}>
				<i className='fa fa-play' />
			</div>
			{`No source`}
		</div>);
	}
	
	if (typeof ref === 'string') {
		if (ref.indexOf('youtube:') === 0) {
			var videoId = ref.substring(8);
			return (
				<iframe
					width={width}
					height={height}
					src={"https://www.youtube.com/embed/" + videoId}
					frameBorder="0"
					allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture"
					allowFullScreen
				/>
			);
		} else if (ref.indexOf('vimeo:') === 0) {
			return (
				<iframe
					src={"https://player.vimeo.com/video/" + ref.substring(6)}
					width={width}
					height={height}
					frameBorder="0"
					allow="autoplay; fullscreen"
					allowFullScreen
				/>
			);
		} else if (ref.indexOf('wistia:') === 0) {
			return (
				<iframe
					src={"https://fast.wistia.net/embed/iframe/" + ref.substring(7)}
					width={width}
					height={height}
					frameBorder="0"
					allow="autoplay; fullscreen"
					allowFullScreen
				/>
			);
		}
	}

	/* assuming mp4 for now */
	return (
		<video
			width={width}
			height={height}
			autoPlay={autoplay}
			controls
		>
			<source
				src={getUrl(props.fileRef as any, {url: true, size: props.size} as any)}
				type="video/mp4"
			/>
			{`Your browser does not support this video.`}
		</video>
	);
}

export default Video;
