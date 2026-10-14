export enum MediaConnectionType {
    LiveKit = 'LiveKit',
    OvenMediaEngine = 'OvenMediaEngine',
}

export interface NamedMediaStream {
    cameraId: string
    url: string
    rotationDegrees?: number
}

export interface MediaStreamConfig {
    url: string | null
    token: string | null
    robotId: string | null
    mediaConnectionType: MediaConnectionType
    streams?: NamedMediaStream[]
}
