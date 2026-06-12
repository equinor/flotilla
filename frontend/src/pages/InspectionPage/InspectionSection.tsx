import { InspectionArea } from 'models/InspectionArea'
import { MissionDefinition } from 'models/MissionDefinition'

export interface InspectionAreaInspectionTuple {
    missionDefinitions: MissionDefinition[]
    inspectionArea: InspectionArea
}
