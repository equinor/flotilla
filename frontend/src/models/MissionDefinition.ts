import { Deck } from './Deck'
import { Mission } from './Mission'

export interface MissionDefinition {
    id: string
    name: string
    installationCode: string
    comment?: string
    inspectionFrequency?: string
    lastSuccessfulRun?: Mission
    inspectionArea?: Deck
    isDeprecated: boolean
    sourceId: string
}

export interface PlantInfo {
    plantCode: string
    projectDescription: string
}
