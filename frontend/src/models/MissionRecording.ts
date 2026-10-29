export interface CameraRecording {
    cameraId: string
    key: string
    firstFrameAt?: string
    status: string
    error?: string
}

export interface MissionRecording {
    status: string
    engine?: string
    gaps: { at: string; reason: string }[]
    cameras: Record<string, CameraRecording>
}

export interface MissionRecordingAccess {
    playbackUrl: string
    downloadUrl: string
    expiresAt: string
}

export interface RecordingSeekRequest {
    timestamp: number
}
