import { Installation } from './Installation'
import { PlantInfo } from './MissionDefinition'

export interface InspectionArea {
    id: string
    name: string
    plant: PlantInfo
    installation: Installation
}
