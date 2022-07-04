interface MediaRecorderProps { onData?(data: unknown): void; }

declare class MediaRecorder {
    constructor(stream: MediaStream, props?: MediaRecorderProps);

    writeHeader(): void;
    start(timeslice: number): void | undefined;
    addData(e: { length: number; data: unknown[]; }): void;
    getWav(): Uint8Array;
    stop(): void;
    onstopped(): void;
    mimeType: string;
    isTypeSupported(mimeType: string): boolean;
    notSupported: boolean;
}

export default MediaRecorder;
