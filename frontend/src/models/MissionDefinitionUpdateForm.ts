import { AutoScheduleFrequency } from './AutoScheduleFrequency'

export interface MissionDefinitionUpdateForm {
    comment?: string
    inspectionFrequency?: string
    autoScheduleFrequency?: AutoScheduleFrequency
    name?: string
    isDeprecated?: boolean
}
