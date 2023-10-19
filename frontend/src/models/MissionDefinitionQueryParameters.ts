import { SourceType } from './MissionDefinition'

export interface MissionDefinitionQueryParameters {
    nameSearch?: string
    robotNameSearch?: string
    area?: string
    sourceType?: SourceType
    pageNumber?: number
    pageSize?: number
    orderBy?: string
}
