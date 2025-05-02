import { InspectionType } from './Inspection'
import { MissionStatus } from './Mission'

export interface MissionRunQueryParameters {
    statuses?: MissionStatus[]
    missionId?: string
    robotId?: string
    nameSearch?: string
    robotNameSearch?: string
    tagSearch?: string
    inspectionTypes?: InspectionType[]
    inspectionArea?: string
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
