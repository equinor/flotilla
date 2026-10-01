export enum MediaConnectionType {
    LiveKit = 'LiveKit',
    OvenMediaEngine = 'OvenMediaEngine',
}

export interface NamedMediaStream {
    role: string
    url: string
}

export interface MediaStreamConfig {
    url: string
    token: string
    robotId: string
    mediaConnectionType: MediaConnectionType
    streams?: NamedMediaStream[]
}
