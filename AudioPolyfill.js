let AudioContext = global.AudioContext || global.webkitAudioContext

function error (method) {
  let event = new Event('error')
  event.data = new Error('Wrong state for ' + method)
  return event
}

let context, processor

/**
 * Audio Recorder with MediaRecorder API.
 *
 * @example
 * navigator.mediaDevices.getUserMedia({ audio: true }).then(stream => {
 *   let recorder = new MediaRecorder(stream)
 * })
 */
class MediaRecorder {
	
	/**
	* @param {MediaStream} stream The audio stream to record.
	*/
	constructor (stream, props) {
		/**
		 * The `MediaStream` passed into the constructor.
		 * @type {MediaStream}
		 */
		 this.props = props || {};
		this.stream = stream
		this.maxDuration = 60;
		this.recording = false
		this.addData = this.addData.bind(this);
	}
	
	writeHeader(){
		let view = new DataView(this.arrayBuffer);
		var BYTES_PER_SAMPLE = 2;
		var sampleRate = this.sampleRate;
		
		var length = this.index - 44;
		// RIFF identifier 'RIFF'
		view.setUint32(0, 1380533830, false)
		// file length minus RIFF identifier length and file description length
		view.setUint32(4, 36 + length, true)
		// RIFF type 'WAVE'
		view.setUint32(8, 1463899717, false)
		// format chunk identifier 'fmt '
		view.setUint32(12, 1718449184, false)
		// format chunk length
		view.setUint32(16, 16, true)
		// sample format (raw)
		view.setUint16(20, 1, true)
		// channel count
		view.setUint16(22, 1, true)
		// sample rate
		view.setUint32(24, sampleRate, true)
		// byte rate (sample rate * block align)
		view.setUint32(28, sampleRate * BYTES_PER_SAMPLE, true)
		// block align (channel count * bytes per sample)
		view.setUint16(32, BYTES_PER_SAMPLE, true)
		// bits per sample
		view.setUint16(34, 8 * BYTES_PER_SAMPLE, true)
		// data chunk identifier 'data'
		view.setUint32(36, 1684108385, false)
		// data chunk length
		view.setUint32(40, length, true)
	}
	
  /**
   * Begins recording media.
   *
   * @param {number} [timeslice] The milliseconds to record into each `Blob`.
   *                             If this parameter isn’t included, single `Blob`
   *                             will be recorded.
   *
   * @return {undefined}
   *
   * @example
   * recordButton.addEventListener('click', () => {
   *   recorder.start()
   * })
   */
	start (timeslice) {
		if (this.recording) {
		  return;
		}
		
		this.index = 44;
		this.recording = true;
		
		if(!this.stream){
			
			if(global.audioinput){
				// Use cordova iOS native polyfill
				global.addEventListener("audioinput", this.addData, false);
				
				context = {sampleRate: 44100};
				global.audioinput.start(context);
			}
			
		}
		
		if (!context) {
		  context = new AudioContext()
		}
		
		this.sampleRate = context.sampleRate;
		this.bufferLength = this.maxDuration * this.sampleRate * 2;
		
		if(!this.arrayBuffer){
			this.arrayBuffer = new ArrayBuffer(this.bufferLength);
			this.buffer = new Uint8Array(this.arrayBuffer); // 2 bytes per sample for x seconds.
		}
		
		if(!this.stream){
			return;
		}
		
		this.clone = this.stream.clone()
		let input = context.createMediaStreamSource(this.clone);
		
		if (!processor) {
			processor = context.createScriptProcessor(2048, 1, 1);
		}
		
		if(this.props.onData){
			processor.onaudioprocess = (e) => {
				if (!this.recording) {
					return;
				}
				
				var data = e.inputBuffer.getChannelData(0);
				this.props.onData(data);
			}
		}else{
			processor.onaudioprocess = (e) => {
				if (!this.recording) {
					return;
				}
				
				var data = e.inputBuffer.getChannelData(0);
				this.addData(data);
			}
		}

		input.connect(processor)
		processor.connect(context.destination)

		return undefined
	}
	
	addData(e){
		var data = e.length ? e:e.data;
		
		for (let i = 0; i < data.length; i++) {
			let sample = data[i];
			
			if (sample > 1) {
				sample = 1
			} else if (sample < -1) {
				sample = -1
			}
			
			sample = (sample * 32767) | 0;
			
			this.buffer[this.index++] = sample;
			this.buffer[this.index++] = sample >> 8;
			
			if(this.index == this.bufferLength){
				// stop the recording
				this.recording = false;
				this.onstopped && this.onstopped();
				this.stop();
				return;
			}
		}
	}
	
	getWav() {
		this.writeHeader();
		
		if(this.index == this.bufferLength){
			return this.buffer;
		}
		
		var buffer = this.buffer.slice(0, this.index);
		return buffer;
		
	}
	
	stop () {
		if (!this.recording) {
			return;
		}

		this.recording = false;
		
		if(this.stream == null){
			
			global.removeEventListener("audioinput", this.addData, false);
			
			if(global.audioinput){
				global.audioinput.stop();
			}
		}else{
			this.clone.getTracks().forEach(track => {
				track.stop()
			})
		}
	}

}

/**
 * The MIME type that is being used for recording.
 * @type {string}
 */
MediaRecorder.prototype.mimeType = 'audio/wav'

/**
 * Returns `true` if the MIME type specified is one the polyfill can record.
 *
 * This polyfill supports `audio/wav` and `audio/mpeg`.
 *
 * @param {string} mimeType The mimeType to check.
 *
 * @return {boolean} `true` on `audio/wav` and `audio/mpeg` MIME type.
 */
MediaRecorder.isTypeSupported = mimeType => {
  return MediaRecorder.prototype.mimeType === mimeType
}

/**
 * `true` if MediaRecorder can not be polyfilled in the current browser.
 * @type {boolean}
 *
 * @example
 * if (MediaRecorder.notSupported) {
 *   showWarning('Audio recording is not supported in this browser')
 * }
 */
MediaRecorder.notSupported = !navigator.mediaDevices || !AudioContext

module.exports = MediaRecorder