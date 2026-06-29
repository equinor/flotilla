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
    acousticInspectionMetadata?: AcousticInspectionMetadata
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
    AcousticMeasurement = 'AcousticMeasurement',
}

type AcousticDetectionType = 'leak'

interface Roi {
    x: number
    y: number
    width: number
    height: number
}

export interface AcousticInspectionMetadata {
    frequencyFrom: number
    frequencyTo: number
    snrValueThreshold: number
    detectionType: AcousticDetectionType
    roi?: Roi
}

export enum DisplayMethod {
    Image = 'Image',
    Video = 'Video',
    Number = 'Number',
    None = 'None',
}

export const ValidInspectionReportInspectionTypes: SensorType[] = [
    SensorType.Image,
    SensorType.ThermalImage,
    SensorType.Video,
    SensorType.ThermalVideo,
    SensorType.AcousticMeasurement,
]

export const SensorTypeToDisplayMethod: { [sensorType in SensorType]: DisplayMethod } = {
    [SensorType.Image]: DisplayMethod.Image,
    [SensorType.ThermalImage]: DisplayMethod.Image,
    [SensorType.CO2Measurement]: DisplayMethod.Number,
    [SensorType.Video]: DisplayMethod.Video,
    [SensorType.ThermalVideo]: DisplayMethod.Video,
    [SensorType.Audio]: DisplayMethod.None,
    [SensorType.AcousticMeasurement]: DisplayMethod.Video,
}

export interface SaraInspectionVisualizationReady {
    inspectionId: string
}

export interface SaraAnalysisResultReady {
    inspectionId: string
    analysisType: string
    value?: number
    unit?: string
    class?: string
    confidence?: number
    warning?: string
}
