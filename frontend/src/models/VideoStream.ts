export enum MediaType {
    Video,
    Audio,
}

export enum MediaConnectionType {
    LiveKit,
}

export type MediaStreamConfig = {
    url: string
    streamId: string
    authToken: string
    mediaType: MediaType
    robotId: string
    connectionType: MediaConnectionType
}

export type MediaStreamConfigAndTracks = MediaStreamConfig & { streams: MediaStreamTrack[] }
