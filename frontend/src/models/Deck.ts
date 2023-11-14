import { Pose } from './Pose'

export interface Deck {
    id: string
    deckName: string
    plantName: string
    installationCode: string
    defaultLocalizationPose?: Pose
}
