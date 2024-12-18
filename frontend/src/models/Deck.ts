import { Pose } from './Pose'

export interface InspectionArea {
    id: string
    inspectionAreaName: string
    plantName: string
    installationCode: string
    defaultLocalizationPose?: Pose
}
