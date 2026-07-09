import { AnalysisType } from './MissionDefinition'
import { Position } from './Position'

export interface Inspection {
    id: string
    isarInspectionId: string
    isCompleted: boolean
    inspectionType: SensorType
    inspectionTarget: Position
    videoDuration?: number
    acousticInspectionMetadata?: AcousticInspectionMetadata
    inspectionUrl?: string
    analysisTypes: AnalysisType[]
    taskDescription?: string
    startTime?: Date
    endTime?: Date
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

export const ValidInspectionReportInspectionTypes: SensorType[] = [
    SensorType.Image,
    SensorType.ThermalImage,
    SensorType.Video,
    SensorType.ThermalVideo,
    SensorType.AcousticMeasurement,
]
