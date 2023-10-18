import { Pose } from './Pose'

export interface Area {
    id: string
    areaName: string
    plantCode: string
    plantName: string
    installationCode: string
    deckName: string
    defaultLocalizationPose: Pose
}
