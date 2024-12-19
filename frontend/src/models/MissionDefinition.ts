import { InspectionArea } from './InspectionArea'
import { Mission } from './Mission'

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
}

export interface PlantInfo {
    plantCode: string
    projectDescription: string
}
