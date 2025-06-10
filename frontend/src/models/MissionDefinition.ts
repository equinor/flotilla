import { InspectionArea } from './InspectionArea'
import { Mission } from './Mission'
import { MapMetadata } from './MapMetadata'
import { AutoScheduleFrequency } from './AutoScheduleFrequency'

export interface MissionDefinition {
    id: string
    name: string
    installationCode: string
    comment?: string
    inspectionFrequency?: string
    autoScheduleFrequency?: AutoScheduleFrequency
    lastSuccessfulRun?: Mission
    inspectionArea: InspectionArea
    isDeprecated: boolean
    sourceId: string
    map?: MapMetadata
}

export interface PlantInfo {
    plantCode: string
    projectDescription: string
}
