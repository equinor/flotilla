import { Position } from './Position'

export interface Inspection {
    id: string
    isarInspectionId: string
    isCompleted: boolean
    inspectionType: InspectionType
    inspectionTarget: Position
    videoDuration?: number
    inspectionUrl?: string
    startTime?: Date
    endTime?: Date
}

export enum InspectionType {
    Image = 'Image',
    ThermalImage = 'ThermalImage',
    Video = 'Video',
    ThermalVideo = 'ThermalVideo',
    Audio = 'Audio',
}

export const ValidInspectionReportInspectionTypes: InspectionType[] = [
    InspectionType.Image,
    InspectionType.ThermalImage,
]

export interface SaraInspectionVisualizationReady {
    inspectionId: string
    storageAccount: string
    blobContainer: string
    blobName: string
}
