export interface Inspection {
    id: string
    isarStepId?: string
    status: InspectionStatus
    isCompleted: boolean
    inspectionType: InspectionType
    videoDuration?: number
    analysisTypes?: string
    inspectionUrl?: string
    startTime?: Date
    endTime?: Date
    error?: string
}

enum InspectionStatus {
    Successful = 'Successful',
    InProgress = 'InProgress',
    NotStarted = 'NotStarted',
    Failed = 'Failed',
    Cancelled = 'Cancelled',
}

export enum InspectionType {
    Image = 'Image',
    ThermalImage = 'ThermalImage',
    Video = 'Video',
    ThermalVideo = 'ThermalVideo',
    Audio = 'Audio',
}
