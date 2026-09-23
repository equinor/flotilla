import { InspectionArea } from './InspectionArea'
import { Mission } from './Mission'
import { AutoScheduleFrequency } from './AutoScheduleFrequency'
import { Pose } from './Pose'
import { Position } from './Position'
import { AcousticInspectionMetadata, SensorType } from './Task'

export enum AnalysisType {
    Fencilla = 'fencilla',
    CLOE = 'cloe',
    ThermalReading = 'thermal-reading',
    CO2 = 'co2',
}

interface ZoomDescription {
    objectWidth: number
    objectHeight: number
}

export interface MissionTaskDefinition {
    tagId: string
    description?: string
    robotPose: Pose
    targetPosition: Position
    zoomDescription?: ZoomDescription
    analysisTypes: AnalysisType[]
    sensorType: SensorType
    videoDuration?: number
    acousticInspectionMetadata?: AcousticInspectionMetadata
}

export interface MissionDefinition {
    id: string
    name: string
    isAdHoc: boolean
    installationCode: string
    comment?: string
    autoScheduleFrequency?: AutoScheduleFrequency
    lastSuccessfulRun?: Mission
    inspectionArea: InspectionArea
    tasks: MissionTaskDefinition[]
}
