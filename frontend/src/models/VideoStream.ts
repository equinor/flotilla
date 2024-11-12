export enum MediaConnectionType {
    LiveKit = 'LiveKit',
}

export interface MediaStreamConfig {
    url: string
    token: string
    robotId: string
    mediaConnectionType: MediaConnectionType
}
