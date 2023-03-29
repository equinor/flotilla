import { MissionStatus } from './Mission'

export interface MissionQueryParameters {
    status?: MissionStatus
    assetCode?: string
    robotId?: string
    nameSearch?: string
    robotNameSearch?: string
    tagSearch?: string
    minStartTime?: number
    MaxStartTime?: number
    minEndTime?: number
    maxEndTime?: number
    minDesiredStartTime?: number
    maxDesiredStartTime?: number
    pageNumber?: number
    pageSize?: number
    orderBy?: string
}
