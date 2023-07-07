import { InspectionType } from './Inspection'
import { MissionStatus } from './Mission'

export interface MissionQueryParameters {
    statuses?: MissionStatus[]
    robotId?: string
    nameSearch?: string
    robotNameSearch?: string
    tagSearch?: string
    inspectionTypes?: InspectionType[]
    minStartTime?: number
    maxStartTime?: number
    minEndTime?: number
    maxEndTime?: number
    minDesiredStartTime?: number
    maxDesiredStartTime?: number
    pageNumber?: number
    pageSize?: number
    orderBy?: string
}
