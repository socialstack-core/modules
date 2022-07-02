import { useEffect, useState, useRef } from 'react';

export default function Playback(props){
    var { info, onPlay, onPause, bookmarks } = props;

    var duration = !isNaN(info.duration) && isFinite(info.duration) ? Math.round(info.duration) : 0;
    var isLive = info.isLive;
    var elapsed = !isNaN(info.elapsed) && isFinite(info.elapsed) ? Math.round(info.elapsed) : (isLive ? duration : 0);

    const [playbackActive, setPlaybackActive] = useState(isLive);
    const [controlsVisible, setControlsVisible] = useState(true);
    const [progressTimer, setProgressTimer] = useState(null);
    const [playbackElapsed, setPlaybackElapsed] = useState(elapsed);
    const [playbackDuration, setPlaybackDuration] = useState(duration);
    const [masterVolume, setMasterVolume] = useState(1);
    const [lastVolume, setLastVolume] = useState(1);
    const [volumeMuted, setVolumeMuted] = useState(false);

    // example bookmarks
    /*
    bookmarks = [
        {
            duration: (60 * 8), // 8:00
            title: `Section 1`
        },
        {
            duration: (60 * 3) + 30, // 3:30
            title: `Section 2`
        },
        {
            duration: (60 * 2) + 7, // 2:07
            title: `Section 3`
        }
    ];
    */

    // if no bookmarks or this is live, show this as a single section
    if (!bookmarks || !bookmarks.length || isLive) {
        bookmarks = [
            {
                duration: playbackDuration,
                title: null
            }
        ];
    }

    const playbackRef = useRef(null);
    const playButtonRef = useRef(null);
    const playIconRef = useRef(null);
    const pauseIconRef = useRef(null);
    const progressBarRef = useRef(null);
    const seekRef = useRef(null);
    const seekTooltipRef = useRef(null);
    const volumeRangeRef = useRef(null);
    //const fullscreenButtonRef = useRef(null);
    //const fullscreenIconRef = useRef(null);
    //const fullscreenExitIconRef = useRef(null);
    //const pipButtonRef = useRef(null);
    const elapsedRef = useRef(null);
    const durationRef = useRef(null);

    // toggle playback state of video
    function togglePlay() {

        if (playbackActive) {
            setPlaybackActive(false);
            onPause();
            clearInterval(progressTimer);
        } else {
            setPlaybackActive(true);
            onPlay();
            setProgressTimer(setInterval(updateProgress, 1000));
        }

    }

    // update progress bar
    function updateProgress() {
        setPlaybackElapsed(playbackElapsed => playbackElapsed + 1);

        if (isLive) {
            // TODO: check - already being updated via props.playbackinfo.duration?
            setPlaybackDuration(playbackDuration => playbackDuration + 1);
        }

    }

    // format given time into hh:mm:ss
    function formatTime(timeInSeconds) {
        var timeString = new Date((!isNaN(timeInSeconds) && isFinite(timeInSeconds) ? timeInSeconds : 0) * 1000).toISOString();
        return (playbackDuration < 60 * 60) ? timeString.substr(14, 5) : timeString.substr(11, 8);
    }

    // uses the position of the mouse on the progress bar to roughly work out what point in 
    // the video the user will skip to if the progress bar is clicked at that point
    function updateSeekTooltip(event) {
        const skipTo = Math.round(
            (event.offsetX / event.target.clientWidth) *
            parseInt(event.target.getAttribute('max'), 10)
        );
        seekRef.current.setAttribute('data-seek', skipTo);
        seekTooltipRef.current.textContent = formatTime(skipTo);

        const rect = playbackRef.current.getBoundingClientRect();
        seekTooltipRef.current.style.left = `${event.pageX - rect.left}px`;
    }

    // jump to a different point in the video when progress bar clicked
    function skipAhead(event) {
        const skipTo = event.target.dataset.seek
            ? event.target.dataset.seek
            : event.target.value;

        // TODO: update huddle playback position
        //videoRef.current.currentTime = skipTo;

        progressBarRef.current.value = skipTo;
        seekRef.current.value = skipTo;
    }

    // updates video volume and disable muted state if active
    function updateVolume(e) {
        setMasterVolume(volumeRangeRef.current.value)
    }

    // mutes / unmutes video when executed; 
    // when video is unmuted, volume is returned to the value it was set to before
    function toggleMute() {

        if (!volumeMuted) {
            setVolumeMuted(true);
            setLastVolume(masterVolume);
            setMasterVolume(0);
        } else {
            setVolumeMuted(false);
            setMasterVolume(lastVolume);
        }

    }

    // TODO: better implemented outside of this component
    // toggle fullscreen state
    /**
    function toggleFullScreen(videoContainer) {
        if (document.fullscreenElement) {
            document.exitFullscreen();
        } else if (document.webkitFullscreenElement) {
            // Need this to support Safari
            document.webkitExitFullscreen();
        } else if (videoContainer.webkitRequestFullscreen) {
            // Need this to support Safari
            videoContainer.webkitRequestFullscreen();
        } else {
            videoContainer.requestFullscreen();
        }
    }

    // toggles icon and tooltip of fullscreen button
    function updateFullscreenButton() {
        fullscreenIconRef.current.classList.toggle('hidden');
        fullscreenExitIconRef.current.classList.toggle('hidden');

        if (document.fullscreenElement) {
            fullscreenButtonRef.current.setAttribute('data-title', `Exit full screen`);
        } else {
            fullscreenButtonRef.current.setAttribute('data-title', `Full screen`);
        }
    }

    // toggle picture-in-picture mode
    async function togglePip() {

        if (!videoRef || !videoRef.current) {
            return;
        }

        var video = videoRef.current;

        try {
            if (video !== document.pictureInPictureElement) {
                pipButtonRef.current.disabled = true;
                await video.requestPictureInPicture();
            } else {
                await document.exitPictureInPicture();
            }
        } catch (error) {
            console.error(error);
        } finally {
            pipButtonRef.current.disabled = false;
        }
    }
     */

    // hide the video controls when not in use;
    // if video is paused, controls must remain visible
    function hideControls() {

        if (!playbackActive) {
            return;
        }

        setControlsVisible(false);
    }

    // display the video controls
    function showControls() {
        setControlsVisible(true);
    }

    useEffect(() => {

        if (progressTimer && playbackElapsed >= playbackDuration) {
            togglePlay();
        }

    }, [playbackElapsed]);

    useEffect(() => {
        playButtonRef.current.addEventListener('click', togglePlay);
        playbackRef.current.addEventListener('mouseenter', showControls);
        playbackRef.current.addEventListener('mouseleave', hideControls);
        seekRef.current.addEventListener('mousemove', updateSeekTooltip);
        seekRef.current.addEventListener('input', skipAhead);
        volumeRangeRef.current.addEventListener('input', updateVolume);
        //fullscreenButtonRef.current.addEventListener('click', toggleFullScreen);
        //videoContainer.addEventListener('fullscreenchange', updateFullscreenButton);
        //pipButtonRef.current.addEventListener('click', togglePip);

        /*
        if (!('pictureInPictureEnabled' in document)) {
            pipButtonRef.current.classList.add('hidden');
        }
        */

        return () => {
            playButtonRef.current.removeEventListener('click', togglePlay);
            playbackRef.current.removeEventListener('mouseenter', showControls);
            playbackRef.current.removeEventListener('mouseleave', hideControls);
            seekRef.current.removeEventListener('mousemove', updateSeekTooltip);
            seekRef.current.removeEventListener('input', skipAhead);
            volumeRangeRef.current.removeEventListener('input', updateVolume);
            //fullscreenButtonRef.current.removeEventListener('click', toggleFullScreen);
            //videoContainer.removeEventListener('fullscreenchange', updateFullscreenButton);
            //pipButtonRef.current.removeEventListener('click', togglePip);
        };

    });

    var playbackClass = ['huddle-chat__playback'];

    if (!controlsVisible) {
        playbackClass.push('huddle-chat__playback--hide');
    }

    var progressClass = ['huddle-chat__playback-progress'];

    if (isLive) {
        progressClass.push('huddle-chat__playback-progress--live');
    }

    var liveClass = ['huddle-chat__playback-live'];

    // TODO - determine if we're watching a live stream but are running behind
    /*
    if (isBehind) {
        liveClass.push('huddle-chat__playback-live--behind');
    }
    */

    return <div className={playbackClass.join(' ')} ref={playbackRef}>
        {/* progress indicator */}
        <div className="huddle-chat__playback-progress-wrapper">
            <div className="progress">
                {bookmarks.map(section => {
                    var sectionPercentage = Math.round((section.duration / playbackDuration) * 100);
                    var sectionStyle = { 'width': sectionPercentage + '%' };

                    return <div className="progress-bar" role="progressbar" style={sectionStyle} aria-valuenow={sectionPercentage} aria-valuemin={0} aria-valuemax={100}></div>;
                })}
            </div>
            <progress className={progressClass.join(' ')} value={playbackElapsed} min="0" max={playbackDuration} ref={progressBarRef}></progress>
            <input className="huddle-chat__playback-seek" value={playbackElapsed} min="0" max={playbackDuration}
                type="range" step="1" disabled={!isNaN(playbackDuration) && isFinite(playbackDuration) ? undefined : 'disabled'} ref={seekRef} />
            <div className="huddle-chat__playback-seek-tooltip" ref={seekTooltipRef}>00:00</div>
        </div>

        {/* playback controls */}
        <div className="huddle-chat__playback-controls-wrapper">
            <div className="huddle-chat__playback-controls">
                <button className="btn huddle-chat__playback-play" type="button" data-title={playbackActive ? `Pause` : `Play`} ref={playButtonRef}>
                    <svg className="huddle-chat__playback-button-icon">
                        <use className={playbackActive ? 'hidden' : ''} href="#play-icon" ref={playIconRef}></use>
                        <use className={!playbackActive ? 'hidden' : ''} href="#pause" ref={pauseIconRef}></use>
                    </svg>
                </button>

                <div className="huddle-chat__playback-volume">
                    <button className="btn huddle-chat__playback-volume-button" type="button"
                        data-title={volumeMuted || masterVolume == 0 ? `Unmute` : `Mute`} onClick={() => toggleMute()}>
                        <svg className="huddle-chat__playback-button-icon">
                            {(volumeMuted || masterVolume == 0) && <>
                                <use href="#volume-mute"></use>
                            </>}
                            {!volumeMuted && masterVolume > 0 && masterVolume <= 0.5 && <>
                                <use href="#volume-low"></use>
                            </>}
                            {!volumeMuted && masterVolume > 0.5 && <>
                                <use href="#volume-high"></use>
                            </>}
                        </svg>
                    </button>

                    <input className="huddle-chat__playback-volume-range" value={masterVolume} data-mute="0.5" type="range" max="1" min="0" step="0.01" ref={volumeRangeRef} />
                </div>

                {!isLive && <>
                    <div className="huddle-chat__playback-time">
                        <time className="huddle-chat__playback-time-elapsed" ref={elapsedRef} datetime={formatTime(playbackElapsed)}>
                            {formatTime(playbackElapsed)}
                        </time>
                        <span> / </span>
                        <time className="huddle-chat__playback-time-duration" ref={durationRef} datetime={formatTime(playbackDuration)}>
                            {formatTime(playbackDuration)}
                        </time>
                    </div>
                </>}

                {isLive && <>
                    <p className={liveClass.join(' ')}>
                        {`LIVE`}
                    </p>
                </>}
            </div>

            {/*
            <div className="huddle-chat__playback-sizing">
                <button className="btn huddle-chat__playback-pip" type="button" data-title={`Picture-in-picture`} ref={pipButtonRef}>
                    <svg className="huddle-chat__playback-button-icon">
                        <use href="#pip"></use>
                    </svg>
                </button>

                <button className="btn huddle-chat__playback-fullscreen" type="button" data-title={`Full screen`} ref={fullscreenButtonRef}>
                    <svg className="huddle-chat__playback-button-icon">
                        <use href="#fullscreen" ref={fullscreenIconRef}></use>
                        <use href="#fullscreen-exit" className="hidden" ref={fullscreenExitIconRef}></use>
                    </svg>
                </button>
            </div>
             */}
        </div>

        {/* icon resources */}
        <svg className="huddle-chat__playback-icons">
            <defs>
                <symbol id="pause" viewBox="0 0 24 24">
                    <path d="M14.016 5.016h3.984v13.969h-3.984v-13.969zM6 18.984v-13.969h3.984v13.969h-3.984z"></path>
                </symbol>

                <symbol id="play-icon" viewBox="0 0 24 24">
                    <path d="M8.016 5.016l10.969 6.984-10.969 6.984v-13.969z"></path>
                </symbol>

                <symbol id="volume-high" viewBox="0 0 24 24">
                    <path d="M14.016 3.234q3.047 0.656 5.016 3.117t1.969 5.648-1.969 5.648-5.016 3.117v-2.063q2.203-0.656 3.586-2.484t1.383-4.219-1.383-4.219-3.586-2.484v-2.063zM16.5 12q0 2.813-2.484 4.031v-8.063q1.031 0.516 1.758 1.688t0.727 2.344zM3 9h3.984l5.016-5.016v16.031l-5.016-5.016h-3.984v-6z"></path>
                </symbol>

                <symbol id="volume-low" viewBox="0 0 24 24">
                    <path d="M5.016 9h3.984l5.016-5.016v16.031l-5.016-5.016h-3.984v-6zM18.516 12q0 2.766-2.531 4.031v-8.063q1.031 0.516 1.781 1.711t0.75 2.32z"></path>
                </symbol>

                <symbol id="volume-mute" viewBox="0 0 24 24">
                    <path d="M12 3.984v4.219l-2.109-2.109zM4.266 3l16.734 16.734-1.266 1.266-2.063-2.063q-1.547 1.313-3.656 1.828v-2.063q1.172-0.328 2.25-1.172l-4.266-4.266v6.75l-5.016-5.016h-3.984v-6h4.734l-4.734-4.734zM18.984 12q0-2.391-1.383-4.219t-3.586-2.484v-2.063q3.047 0.656 5.016 3.117t1.969 5.648q0 2.203-1.031 4.172l-1.5-1.547q0.516-1.266 0.516-2.625zM16.5 12q0 0.422-0.047 0.609l-2.438-2.438v-2.203q1.031 0.516 1.758 1.688t0.727 2.344z"></path>
                </symbol>

                <symbol id="fullscreen" viewBox="0 0 24 24">
                    <path d="M14.016 5.016h4.969v4.969h-1.969v-3h-3v-1.969zM17.016 17.016v-3h1.969v4.969h-4.969v-1.969h3zM5.016 9.984v-4.969h4.969v1.969h-3v3h-1.969zM6.984 14.016v3h3v1.969h-4.969v-4.969h1.969z"></path>
                </symbol>

                <symbol id="fullscreen-exit" viewBox="0 0 24 24">
                    <path d="M15.984 8.016h3v1.969h-4.969v-4.969h1.969v3zM14.016 18.984v-4.969h4.969v1.969h-3v3h-1.969zM8.016 8.016v-3h1.969v4.969h-4.969v-1.969h3zM5.016 15.984v-1.969h4.969v4.969h-1.969v-3h-3z"></path>
                </symbol>

                <symbol id="pip" viewBox="0 0 24 24">
                    <path d="M21 19.031v-14.063h-18v14.063h18zM23.016 18.984q0 0.797-0.609 1.406t-1.406 0.609h-18q-0.797 0-1.406-0.609t-0.609-1.406v-14.016q0-0.797 0.609-1.383t1.406-0.586h18q0.797 0 1.406 0.586t0.609 1.383v14.016zM18.984 11.016v6h-7.969v-6h7.969z"></path>
                </symbol>
            </defs>
        </svg>
    </div>;
}