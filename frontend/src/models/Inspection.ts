import { AnalysisType } from './MissionDefinition'
import { Position } from './Position'

export interface Inspection {
    id: string
    isarInspectionId: string
    isCompleted: boolean
    inspectionType: SensorType
    analysisResult?: AnalysisResult
    inspectionTarget: Position
    videoDuration?: number
    inspectionUrl?: string
    analysisTypes: AnalysisType[]
    taskDescription?: string
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

export enum SensorType {
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

export const ValidInspectionReportInspectionTypes: SensorType[] = [SensorType.Image, SensorType.ThermalImage]

export const SensorTypeToDisplayMethod: { [sensorType in SensorType]: DisplayMethod } = {
    [SensorType.Image]: DisplayMethod.Image,
    [SensorType.ThermalImage]: DisplayMethod.Image,
    [SensorType.CO2Measurement]: DisplayMethod.Number,
    [SensorType.Video]: DisplayMethod.None,
    [SensorType.ThermalVideo]: DisplayMethod.None,
    [SensorType.Audio]: DisplayMethod.None,
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
