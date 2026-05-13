import { InspectionArea } from './InspectionArea'
import { Mission } from './Mission'
import { AutoScheduleFrequency } from './AutoScheduleFrequency'
import { Pose } from './Pose'
import { Position } from './Position'
import { SensorType } from './Inspection'

export enum AnalysisType {
    Fencilla = 'Fencilla',
    CLOE = 'CLOE',
    ThermalReading = 'ThermalReading',
    CO2 = 'CO2',
}

interface ZoomDescription {
    objectWidth: number
    objectHeight: number
}

interface MissionTaskDefinition {
    id: string
    tagId: string
    description?: string
    robotPose: Pose
    targetPosition: Position
    zoomDescription?: ZoomDescription
    analysisTypes: AnalysisType[]
    sensorType: SensorType
    videoDuration?: number
}

export interface MissionDefinition {
    id: string
    name: string
    installationCode: string
    comment?: string
    inspectionFrequency?: string
    autoScheduleFrequency?: AutoScheduleFrequency
    lastSuccessfulRun?: Mission
    inspectionArea: InspectionArea
    tasks: MissionTaskDefinition[]
}
