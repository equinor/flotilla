import { AnalysisType } from './MissionDefinition'
import { Pose } from './Pose'
import { Position } from './Position'

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

export interface Task {
    id: string
    taskOrder: number
    tagId?: string
    description?: string
    robotPose: Pose
    analysisTypes: AnalysisType[]
    status: TaskStatus
    isCompleted: boolean
    startTime?: Date
    endTime?: Date
    sensorType: SensorType
    targetPosition: Position
    videoDuration?: number
    acousticInspectionMetadata?: AcousticInspectionMetadata
    errorDescription?: string
}

export enum TaskStatus {
    Successful = 'Successful',
    PartiallySuccessful = 'PartiallySuccessful',
    NotStarted = 'NotStarted',
    InProgress = 'InProgress',
    Failed = 'Failed',
    Cancelled = 'Cancelled',
    Paused = 'Paused',
}
