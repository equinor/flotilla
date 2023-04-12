import { Pose } from './Pose'

export interface AssetDeck {
    id: string
    assetCode: string
    deckName: string
    defaultLocalizationPose: Pose
}
