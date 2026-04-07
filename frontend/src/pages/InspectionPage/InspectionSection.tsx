import { InspectionArea } from 'models/InspectionArea'
import { MissionDefinition } from 'models/MissionDefinition'

export interface Inspection {
    missionDefinition: MissionDefinition
    deadline: Date | undefined
}

export interface InspectionAreaInspectionTuple {
    inspections: Inspection[]
    inspectionArea: InspectionArea
}
