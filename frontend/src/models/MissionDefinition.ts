import { InspectionArea } from './InspectionArea'
import { Mission } from './Mission'
import { MapMetadata } from './MapMetadata'

export interface MissionDefinition {
    id: string
    name: string
    installationCode: string
    comment?: string
    inspectionFrequency?: string
    lastSuccessfulRun?: Mission
    inspectionArea?: InspectionArea
    isDeprecated: boolean
    sourceId: string
    map?: MapMetadata
}

export interface PlantInfo {
    plantCode: string
    projectDescription: string
}
