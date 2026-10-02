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
    url: string
    token: string
    robotId: string
    mediaConnectionType: MediaConnectionType
    streams?: NamedMediaStream[]
}
