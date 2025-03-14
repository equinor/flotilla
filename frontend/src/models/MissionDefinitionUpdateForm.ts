import { AutoScheduleFrequency } from './AutoScheduleFrequency'

export interface MissionDefinitionUpdateForm {
    comment?: string
    autoScheduleFrequency?: AutoScheduleFrequency
    name?: string
    isDeprecated?: boolean
}
