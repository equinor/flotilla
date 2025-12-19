import { TimeAndDay } from './AutoScheduleFrequency'

export interface MissionDefinitionUpdateForm {
    comment?: string
    schedulingTimesCETperWeek?: TimeAndDay[]
    name?: string
    isDeprecated?: boolean
}
