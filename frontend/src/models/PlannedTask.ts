import { IsarPosition } from './IsarPosition'
import { PlannedInspection } from './PlannedInspection'

export interface PlannedTask {
    id: string
    tagId: string
    url: string
    tagPosition: IsarPosition
    inspections: PlannedInspection[]
}
