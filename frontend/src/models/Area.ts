import { Pose } from './Pose'

export interface Area {
    id: string
    areaName: string
    installationCode: string
    assetCode: string
    deckName: string
    defaultLocalizationPose: Pose
}
