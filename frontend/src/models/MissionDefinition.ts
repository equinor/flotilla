import { Area } from './Area'
import { Mission } from './Mission'
import { Task } from './Task'

export interface EchoMissionDefinition {
    echoMissionId: number
    name: string
}

export enum SourceType {
    Echo = 'Echo',
    Custom = 'Custom',
}

export interface CondensedMissionDefinition {
    id: string
    name: string
    installationCode: string
    comment?: string
    inspectionFrequency?: string
    lastSuccessfulRun?: Mission
    area?: Area
    isDeprecated: boolean
    sourceType: SourceType
}

export interface MissionDefinition {
    id: string
    tasks: Task[]
    name: string
    installationCode: string
    comment?: string
    inspectionFrequency?: string
    lastSuccessfulRun?: Mission
    area?: Area
    isDeprecated: boolean
    sourceType: string
}
