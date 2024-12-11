import { Position } from './Position'

export interface Inspection {
    id: string
    isarTaskId?: string
    status: InspectionStatus
    isCompleted: boolean
    inspectionType: InspectionType
    inspectionTarget: Position
    videoDuration?: number
    analysisType?: string
    inspectionUrl?: string
    startTime?: Date
    endTime?: Date
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
