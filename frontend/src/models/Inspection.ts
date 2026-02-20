import { Position } from './Position'

export interface Inspection {
    id: string
    isarInspectionId: string
    isCompleted: boolean
    inspectionType: InspectionType
    analysisResult?: AnalysisResult
    inspectionTarget: Position
    videoDuration?: number
    inspectionUrl?: string
    startTime?: Date
    endTime?: Date
}

interface AnalysisResult {
    inspectionId: string
    analysisType: string
    value?: string
    unit?: string
    confidence?: number
    warning?: string
    storageAccount?: string
    blobContainer?: string
    blobName?: string
}

export enum InspectionType {
    Image = 'Image',
    ThermalImage = 'ThermalImage',
    Video = 'Video',
    ThermalVideo = 'ThermalVideo',
    Audio = 'Audio',
    CO2Measurement = 'CO2Measurement',
}

export enum DisplayMethod {
    Image = 'Image',
    Number = 'Number',
    None = 'None',
}

export const ValidInspectionReportInspectionTypes: InspectionType[] = [
    InspectionType.Image,
    InspectionType.ThermalImage,
]

export const InspectionTypeToDisplayMethod: { [inspectionType in InspectionType]: DisplayMethod } = {
    [InspectionType.Image]: DisplayMethod.Image,
    [InspectionType.ThermalImage]: DisplayMethod.Image,
    [InspectionType.CO2Measurement]: DisplayMethod.Number,
    [InspectionType.Video]: DisplayMethod.None,
    [InspectionType.ThermalVideo]: DisplayMethod.None,
    [InspectionType.Audio]: DisplayMethod.None,
}

export interface SaraInspectionVisualizationReady {
    inspectionId: string
    storageAccount: string
    blobContainer: string
    blobName: string
}

export interface SaraAnalysisResultReady {
    inspectionId: string
    displayText: string
    analysisType: string
    value?: number
    unit?: string
    class?: string
    confidence?: number
    warning?: string
    storageAccount: string
    blobContainer: string
    blobName: string
}
